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

        public MQTTSink(IFormatProvider? formatProvider)
        {
            _formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            if (!Settings.Instance.MQTTActive)
                return;

            var logLevel = (int)logEvent.Level;
            var configuredMinimalLogLevel = (int)Settings.Instance.MQTTLogLevel;

            if(logLevel >= configuredMinimalLogLevel)
            {
                var message = logEvent.RenderMessage(_formatProvider);

                try
                {
                    Settings.Instance.MQTTSender.SendUpdate(Models.CurrentPointParameterType.LogEntry,message,true).Wait();
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
