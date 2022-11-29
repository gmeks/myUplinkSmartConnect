using Microsoft.Extensions.DependencyInjection;
using MyUplinkSmartConnect.Services;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect.MQTT
{
    public class MQTTSink : ILogEventSink
    {
        private readonly IFormatProvider? _formatProvider;
        private MQTTService _mqttService;

        public MQTTSink(IFormatProvider? formatProvider)
        {
            _formatProvider = formatProvider;            
        }

        public void Emit(LogEvent logEvent)
        {
            if (!Settings.Instance.MQTTActive)
                return;

            if(_mqttService == null)
            {
                _mqttService = Settings.ServiceLookup.GetService<MQTTService>();
            }

            var logLevel = (int)logEvent.Level;
            var configuredMinimalLogLevel = (int)Settings.Instance.MQTTLogLevel;

            if(logLevel >= configuredMinimalLogLevel)
            {
                var message = logEvent.RenderMessage(_formatProvider);

                try
                {
                    _mqttService.SendUpdate(Models.CurrentPointParameterType.LogEntry,message,true).Wait();
                }
                catch
                {

                }
            }            
        }
    }

    public static class MQTTSinkExtensions
    {
        public static LoggerConfiguration MQTTSink(this LoggerSinkConfiguration loggerConfiguration, IFormatProvider? formatProvider = null)
        {
            return loggerConfiguration.Sink(new MQTTSink(formatProvider));
        }
    }
}
