using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MyUplinkSmartConnect.Models;
using MyUplinkSmartConnect.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace MyUplinkSmartConnect.Services
{
    public class MQTTService
    {
        readonly MqttClientFactory _mqttFactory;
        static readonly object _lock = new object();
        static IMqttClient? _mqttClient;

        static int _connectionFailedCount = 0;
        const int ConnectionFailedCountConsiderError = 3;

        public MQTTService()
        {
            _mqttFactory = new MqttClientFactory();
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
                    if (_connectionFailedCount >= ConnectionFailedCountConsiderError)
                        Log.Logger.Error(ex, "Failed to send message to MTQQ message");
                    else
                        Log.Logger.Debug(ex, "Failed to send message to MTQQ message");

                    _mqttClient?.Dispose();
                    _mqttClient = null;
                }
            }
        }

        internal async Task SendUpdate(CurrentPointParameterType parameter, object value, bool retainMessage = false)
        {
            CheckMQttConnectionStatus();

            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                try
                {
                    MqttApplicationMessage? applicationMessage = new MqttApplicationMessageBuilder().WithTopic($"heater/{parameter}").WithPayload(value.ToString()).WithRetainFlag(retainMessage).Build();
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
                        var timeoutCts = new CancellationTokenSource();
                        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

                        var tmp = _mqttClient.ConnectAsync(optionsBuilder.Build(), timeoutCts.Token).Result;

                        if (tmp.ResultCode == MqttClientConnectResultCode.Success)
                        {
                            _connectionFailedCount = 0;
                        }
                        _mqttClient.SubscribeAsync("heater/boost", MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
                        _mqttClient.SubscribeAsync("heater/reset_schedule", MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
                        _mqttClient.SubscribeAsync("heater/set_vacation", MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
                        _mqttClient.SubscribeAsync("heater/consoleloglevel", MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
                        _mqttClient.ApplicationMessageReceivedAsync += _mqttClient_ApplicationMessageReceivedAsync;
                    }
                    catch (Exception ex)
                    {
                        _connectionFailedCount++;
                        if (_connectionFailedCount >= ConnectionFailedCountConsiderError)
                            Log.Logger.Error(ex, "Failed to connect to MTQQ server ");
                        else
                            Log.Logger.Debug(ex, "Failed to connect to MTQQ server ");

                        _mqttClient?.Dispose();
                        _mqttClient = null;
                    }
                }
            }
        }

        private async Task _mqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            if (string.IsNullOrEmpty(arg.ApplicationMessage.Topic))
                return;

            Log.Logger.Debug("MQTT message recived: " + arg.ApplicationMessage.Topic);

            if (arg.ApplicationMessage.Topic.StartsWith("heater/consoleloglevel"))
            {
                Log.Logger.Information("MQTT message recived, changing logging to debug");
                Settings.Instance.ConsoleLogLevel = Serilog.Events.LogEventLevel.Debug;
            }

            if (arg.ApplicationMessage.Topic.StartsWith("heater/boost"))
            {
                Log.Logger.Information("MQTT message recived, to add a boost now");
                var scheduleAdjust = Settings.ServiceLookup?.GetService<ScheduleAdjustService>() ?? throw new NullReferenceException();
                scheduleAdjust.Add();

                Settings.Instance.ForceScheduleRebuild = true;
            }

            if (arg.ApplicationMessage.Topic.StartsWith("heater/reset_schedule"))
            {
                Log.Logger.Information("MQTT message recived, reset and rebuild schedule");
                var scheduleAdjust = Settings.ServiceLookup?.GetService<ScheduleAdjustService>() ?? throw new NullReferenceException();
                scheduleAdjust.RemoveBoost();

                Settings.Instance.ForceScheduleRebuild = true;
            }

            if (arg.ApplicationMessage.Topic.StartsWith("heater/set_vacation"))
            {
                Log.Logger.Information("MQTT message recived, reset and rebuild schedule");
                var stateService = Settings.ServiceLookup?.GetService<CurrentStateService>() ?? throw new NullReferenceException();
                var myUplink = Settings.ServiceLookup.GetService<MyUplinkService>() ?? throw new NullReferenceException();

                var vacation = JsonSerializer.Deserialize<VacationsSchedules>(arg.ApplicationMessage.ConvertPayloadToString()) ?? throw new NullReferenceException();
                vacation.isEnabled = true;
                vacation.modeId = stateService.ModeLookup.GetHeatingModeId(HeatingMode.HeathingDisabled);


                var group = await myUplink.GetDevices();
                foreach (var device in group)
                {
                    if (device.devices == null)
                    {
                        Log.Logger.Error("Group({DeviceId}) does not have devices", device.id);
                        continue;
                    }

                    foreach (var tmpdevice in device.devices)
                    {
                        await myUplink.SetVacation(tmpdevice, vacation);
                    }
                }
                       
            }
        }
    }
}
