using MQTTnet.Client;
using MQTTnet;
using Serilog;

namespace xElectricityPriceApi.Services
{
    public enum MessageType
    {
        Unkown,
        CurrentListPrice,
        CurrentPriceEstimatedEffectivePrice,
        CurrentPriceDescription,
        CheapestHour,
    }
    public class MQTTSenderService(SettingsService settingsService)
    {
        readonly MqttFactory _mqttFactory = new MqttFactory();
        readonly SettingsService _settingsService = settingsService;
        static readonly object _lock = new object();
        static IMqttClient? _mqttClient;

        static int _connectionFailedCount = 0;
        const int ConnectionFailedCountConsiderError = 3;

        internal async Task SendUpdate(MessageType msgType, object value, bool retainMessage = true)
        {
            CheckMQttConnectionStatus();

            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                try
                {
                    var applicationMessage = new MqttApplicationMessageBuilder().WithTopic($"electricityPrice/{msgType}").WithPayload(value.ToString()).WithRetainFlag(retainMessage).Build();
                    await _mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
                    Log.Logger.Debug("Sending update {DeviceName} - {Parameter} - {Value}", msgType, value);
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

        void CheckMQttConnectionStatus()
        {
            lock (_lock)
            {
                if (_mqttClient == null || !_mqttClient.IsConnected)
                {
                    _mqttClient = _mqttFactory.CreateMqttClient();
                    MqttClientOptionsBuilder optionsBuilder;


                    if (_settingsService.Instance.MQTTServerPort != 0)
                        optionsBuilder = new MqttClientOptionsBuilder().WithTcpServer(_settingsService.Instance.MQTTServer, _settingsService.Instance.MQTTServerPort).WithKeepAlivePeriod(TimeSpan.FromMinutes(1));
                    else
                        optionsBuilder = new MqttClientOptionsBuilder().WithTcpServer(_settingsService.Instance.MQTTServer).WithKeepAlivePeriod(TimeSpan.FromMinutes(1));

                    if (!string.IsNullOrEmpty(_settingsService.Instance.MQTTUserName))
                    {
                        optionsBuilder = optionsBuilder.WithCredentials(_settingsService.Instance.MQTTUserName, _settingsService.Instance.MQTTPassword);
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

                       // _mqttClient.SubscribeAsync("heater/consoleloglevel", MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
                      //  _mqttClient.ApplicationMessageReceivedAsync += _mqttClient_ApplicationMessageReceivedAsync;
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
    }
}
