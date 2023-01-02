using Microsoft.Extensions.DependencyInjection;
using MyUplinkSmartConnect.MQTT;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Text.Json.Serialization;
using xElectricityPriceApiShared;

namespace MyUplinkSmartConnect
{
    static class Settings
    {
        public static SettingsValues Instance { get; set; } = new SettingsValues();

        public static Logger CreateLogger(LogEventLevel consoleLogLevel)
        {
            return new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console(restrictedToMinimumLevel: consoleLogLevel).WriteTo.MQTTSink().CreateLogger();
        }
        
        public static ServiceProvider ServiceLookup { get; set; }
    }

    class SettingsValues
    {
        public string UserName { get; set; } = "";

        public string Password { get; set; } = "";

        public int CheckRemoteStatsIntervalInMinutes { get; set; } = 1;

        public bool ChangeSchedule { get; set; } = true;

        public int WaterHeaterMaxPowerInHours { get; set; }

        public int WaterHeaterMediumPowerInHours { get; set; }

        public int MediumPowerTargetTemperature { get; set; }

        public int HighPowerTargetTemperature{ get; set; }

        public bool EnergiBasedCostSaving { get; set; }

        public string? PowerZone { get; set; }

        public string EnergiBasedPeakTimes { get; set; } = "weekday6,weekday21,weekday23,weekend11,weekend23";

        public string? MQTTServer { get; set; }

        public int MQTTServerPort { get; set; }

        public string? MQTTUserName { get; set; }

        public string? MQTTPassword { get; set; }

        public LogEventLevel ConsoleLogLevel { get; set; } = LogEventLevel.Information;

        public LogEventLevel MQTTLogLevel { get; set; } = LogEventLevel.Warning;

        [JsonIgnore]
        public PowerZoneName InternalPowerZone { get; set; }

        [JsonIgnore]
        public bool ForceScheduleRebuild { get; set; } = false;

        [JsonIgnore]
        public bool RequireUseOfM2ForLegionellaProgram
        {
            get
            {
                if(HighPowerTargetTemperature < 75)
                    return true;

                return false;
            }
        }

        [JsonIgnore]
        public bool MQTTActive
        {
            get
            {
                if(!string.IsNullOrEmpty(MQTTServer) && CheckRemoteStatsIntervalInMinutes >= 1)
                    return true;

                return false;
            }
        }
    }
}
