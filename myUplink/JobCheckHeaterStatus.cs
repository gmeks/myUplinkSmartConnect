using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MyUplinkSmartConnect.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect
{
    internal class JobCheckHeaterStatus
    {
        readonly MqttFactory _mqttFactory;
        static IMqttClient? _mqttClient;
        IEnumerable<Group>? _deviceGroup;

        public JobCheckHeaterStatus()
        {
            _mqttFactory = new MqttFactory();
        }

        enum parameterValue
        {
            TargetTemprature= 527,
            CurrentTemprature= 528,
        }

        public async Task Work()
        {
            if(_deviceGroup == null)
            {
                _deviceGroup = await Settings.Instance.myuplinkApi.GetDevices();
            }

            foreach (var device in _deviceGroup)
            {
                foreach (var tmpdevice in device.devices)
                {
                    var devicePointsList = await Settings.Instance.myuplinkApi.GetDevicePoints(tmpdevice);
                    foreach (var devicePoint in devicePointsList)
                    {
                        var parm = (parameterValue)int.Parse(devicePoint.parameterId);

                        switch(parm)
                        {
                            case parameterValue.TargetTemprature:
                            case parameterValue.CurrentTemprature:
                                await SendUpdate(device.name, parm, devicePoint.value);
                                break;
                        }                        
                    }
                }
            }            
        }

        async Task SendUpdate(string deviceName, parameterValue parameter,double value)
        {
            if(_mqttClient == null || !_mqttClient.IsConnected)
            {
                _mqttClient = _mqttFactory.CreateMqttClient();
                IMqttClientOptions options;

                if (Settings.Instance.MTQQServerPort != 0)
                    options = new MqttClientOptionsBuilder().WithTcpServer(Settings.Instance.MTQQServer, Settings.Instance.MTQQServerPort).Build();
                else
                    options = new MqttClientOptionsBuilder().WithTcpServer(Settings.Instance.MTQQServer).Build();

                try
                {
                    await _mqttClient.ConnectAsync(options, CancellationToken.None);
                }
                catch(Exception ex)
                {
                    Log.Logger.Error(ex,"Failed to connect to MTQQ server ");
                }                
            }


            if(_mqttClient != null && _mqttClient.IsConnected)
            {
                var applicationMessage = new MqttApplicationMessageBuilder().WithTopic($"heater/{deviceName}/{parameter}").WithPayload(value.ToString()).Build();

                await _mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
                Log.Logger.Information($"Sending update {deviceName} - {parameter} - {value}");
            }            
        }
    }
}
