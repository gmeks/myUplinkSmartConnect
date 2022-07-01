using MQTTnet;
using MQTTnet.Client;
using MyUplinkSmartConnect.Models;
using Serilog;

namespace MyUplinkSmartConnect
{
    internal class JobCheckHeaterStatus
    {
        readonly MqttFactory _mqttFactory;
        static readonly object _lock = new object();
        static IMqttClient? _mqttClient;
        IEnumerable<DeviceGroup>? _deviceGroup;

        public JobCheckHeaterStatus()
        {
            _mqttFactory = new MqttFactory();
        }

        public async Task Work()
        {
            if (Settings.Instance.myuplinkApi == null)
                return;

            if(_deviceGroup == null)
            {
                _deviceGroup = await Settings.Instance.myuplinkApi.GetDevices();
            }

            foreach (var device in _deviceGroup)
            {
                if (string.IsNullOrEmpty(device.name) || device.devices == null)
                    throw new NullReferenceException("device name or device.devices is null");

                foreach (var tmpdevice in device.devices)
                {
                    var devicePointsList = await Settings.Instance.myuplinkApi.GetDevicePoints(tmpdevice, CurrentPointParameterType.TargetTemprature, CurrentPointParameterType.CurrentTemprature, 
                        CurrentPointParameterType.EstimatedPower, CurrentPointParameterType.EnergyTotal, CurrentPointParameterType.EnergiStored, CurrentPointParameterType.FillLevel);

                    foreach (var devicePoint in devicePointsList)
                    {
                        var parm = (CurrentPointParameterType)int.Parse(devicePoint.parameterId ?? "");

                        switch(parm)
                        {
                            case CurrentPointParameterType.FillLevel:
                            case CurrentPointParameterType.EnergiStored:
                            case CurrentPointParameterType.EnergyTotal:
                            case CurrentPointParameterType.EstimatedPower:
                            case CurrentPointParameterType.TargetTemprature:
                            case CurrentPointParameterType.CurrentTemprature:
                                await SendUpdate(device.name, parm, devicePoint.value);
                                break;
                        }                        
                    }
                }
            }            
        }

        internal async Task SendUpdate(string deviceName, CurrentPointParameterType parameter,object value)
        {
            lock(_lock)
            {
                if (_mqttClient == null || !_mqttClient.IsConnected)
                {
                    _mqttClient = _mqttFactory.CreateMqttClient();
                    MqttClientOptionsBuilder optionsBuilder;


                    if (Settings.Instance.MQTTServerPort != 0)
                        optionsBuilder = new MqttClientOptionsBuilder().WithTcpServer(Settings.Instance.MQTTServer, Settings.Instance.MQTTServerPort);
                    else
                        optionsBuilder = new MqttClientOptionsBuilder().WithTcpServer(Settings.Instance.MQTTServer);

                    if (!string.IsNullOrEmpty(Settings.Instance.MQTTUserName))
                    {
                        optionsBuilder = optionsBuilder.WithCredentials(Settings.Instance.MQTTUserName, Settings.Instance.MQTTPassword);
                    }

                    try
                    {
                        _= _mqttClient.ConnectAsync(optionsBuilder.Build(), CancellationToken.None).Result;
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, "Failed to connect to MTQQ server ");
                        _mqttClient = null;
                    }
                }
            }            

            if(_mqttClient != null && _mqttClient.IsConnected)
            {
                try
                {
                    var applicationMessage = new MqttApplicationMessageBuilder().WithTopic($"heater/{deviceName}/{parameter}").WithPayload(value.ToString()).Build();
                    await _mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
                    Log.Logger.Debug("Sending update {DeviceName} - {Parameter} - {Value}",deviceName,parameter,value);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Failed to send message to MTQQ message");
                    _mqttClient?.Dispose();
                    _mqttClient = null;
                }
            }            
        }
    }
}
