using MQTTnet;
using MQTTnet.Client;
using MyUplinkSmartConnect.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect.MQTT
{
    internal class MQTTSender
    {
        readonly MqttFactory _mqttFactory;
        static readonly object _lock = new object();
        static IMqttClient? _mqttClient;

        public MQTTSender()
        {
            _mqttFactory = new MqttFactory();
        }       

        internal async Task SendUpdate(string deviceName, CurrentPointParameterType parameter, object value, bool retainMessage = false)
        {
            CheckMQttConnectionStatus();

            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                try
                {
                    var applicationMessage = new MqttApplicationMessageBuilder().WithTopic($"heater/{deviceName}/{parameter}").WithPayload(value.ToString()).WithRetainFlag(retainMessage).Build();
                    await _mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
                    Log.Logger.Debug("Sending update {DeviceName} - {Parameter} - {Value}", deviceName, parameter, value);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Failed to send message to MTQQ message");
                    _mqttClient?.Dispose();
                    _mqttClient = null;
                }
            }
        }

        internal async Task SendUpdate(CurrentPointParameterType parameter, object value,bool retainMessage = false)
        {
            CheckMQttConnectionStatus();

            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                try
                {
                    var applicationMessage = new MqttApplicationMessageBuilder().WithTopic($"heater/{parameter}").WithPayload(value.ToString()).WithRetainFlag(retainMessage).Build();
                    await _mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
                    //Log.Logger.Debug("Sending update - {Parameter} - {Value}", parameter, value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to write log to MQTT, with error " + ex.Message);
                    _mqttClient?.Dispose();
                    _mqttClient = null;
                }
            }
        }

        void CheckMQttConnectionStatus()
        {
            lock (_lock)
            {
                if (_mqttClient == null || !_mqttClient.IsConnected)
                {
                    _mqttClient = _mqttFactory.CreateMqttClient();
                    MqttClientOptionsBuilder optionsBuilder;


                    if (Settings.Instance.MQTTServerPort != 0)
                        optionsBuilder = new MqttClientOptionsBuilder().WithTcpServer(Settings.Instance.MQTTServer, Settings.Instance.MQTTServerPort).WithKeepAlivePeriod(TimeSpan.FromMinutes(Settings.Instance.CheckRemoteStatsIntervalInMinutes + 1));
                    else
                        optionsBuilder = new MqttClientOptionsBuilder().WithTcpServer(Settings.Instance.MQTTServer).WithKeepAlivePeriod(TimeSpan.FromMinutes(Settings.Instance.CheckRemoteStatsIntervalInMinutes + 1));

                    if (!string.IsNullOrEmpty(Settings.Instance.MQTTUserName))
                    {
                        optionsBuilder = optionsBuilder.WithCredentials(Settings.Instance.MQTTUserName, Settings.Instance.MQTTPassword);
                    }

                    try
                    {
                        _ = _mqttClient.ConnectAsync(optionsBuilder.Build(), CancellationToken.None).Result;
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, "Failed to connect to MTQQ server ");
                        _mqttClient?.Dispose();
                        _mqttClient = null;
                    }
                }
            }
        }
    }
}
