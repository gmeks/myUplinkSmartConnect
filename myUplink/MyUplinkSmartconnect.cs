using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyUplinkSmartConnect.CostSavings;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using xElectricityPriceApiShared;

namespace MyUplinkSmartConnect
{
    internal class MyUplinkSmartconnect : BackgroundService
    {
#if DEBUG
        const string settingsFile = "appsettings.Development.json";
#else
        const string settingsFile = "appsettings.json";         
#endif
        readonly BackgroundJobSupervisor _backgroundJobs;
        readonly ILogger<object> _logger;

        public MyUplinkSmartconnect(ILogger<object> logger)
        {
            _logger = logger;
            _backgroundJobs = new BackgroundJobSupervisor(logger);
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Logger.Information("Starting up service, detected version is {version}", GetVersion());

            EnvVariables env = new EnvVariables(_logger);
            if (!string.IsNullOrEmpty(env.GetValue("IsInsideDocker")))
            {
                Log.Logger.Information("Reading settings from environmental variables");
            }
            else
            {
                if (!File.Exists(settingsFile))
                {
                    Log.Logger.Error($"No settings file found {settingsFile}");
                    return;
                }
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(settingsFile));
                Settings.Instance = new SettingsValues();

                if (dict == null)
                {
                    Log.Logger.Error($"Invalid json");
                    return;
                }

                env = new EnvVariables(_logger,dict);
                Log.Logger.Information("Reading settings from configuration file");
            }

            Settings.Instance.MQTTLogLevel = env.GetValueEnum(LogEventLevel.Warning, nameof(SettingsValues.MQTTLogLevel));
            Settings.Instance.ConsoleLogLevel = env.GetValueEnum(LogEventLevel.Information, nameof(SettingsValues.ConsoleLogLevel), "LogLevel");

            if (Settings.Instance.ConsoleLogLevel != LogEventLevel.Information || Settings.Instance.ConsoleLogLevel > Settings.Instance.MQTTLogLevel)
            {
                if (Settings.Instance.ConsoleLogLevel > Settings.Instance.MQTTLogLevel)
                {
                    Log.Logger.Information("MQTTLogLevel cannot be more verbose then console, moving Console log to {loglevel}", Settings.Instance.MQTTLogLevel);
                    Settings.Instance.ConsoleLogLevel = Settings.Instance.MQTTLogLevel;
                }

                Log.Logger = Settings.CreateLogger(Settings.Instance.ConsoleLogLevel);
                Console.WriteLine($"Setting console log level to {Settings.Instance.ConsoleLogLevel}");
            }

            Settings.Instance = new SettingsValues
            {
                UserName = env.GetValue(nameof(SettingsValues.UserName)),
                Password = env.GetValue(nameof(SettingsValues.Password)),

                ChangeSchedule = env.GetValueBool(nameof(SettingsValues.ChangeSchedule), true),
                EnergiBasedCostSaving = env.GetValueBool(nameof(SettingsValues.EnergiBasedCostSaving), false),
                CheckRemoteStatsIntervalInMinutes = env.GetValueInt(nameof(SettingsValues.CheckRemoteStatsIntervalInMinutes), 1),
                WaterHeaterMaxPowerInHours = env.GetValueInt(nameof(SettingsValues.WaterHeaterMaxPowerInHours), 6),
                WaterHeaterMediumPowerInHours = env.GetValueInt(nameof(SettingsValues.WaterHeaterMediumPowerInHours), 4),
                MediumPowerTargetTemperature = env.GetValueInt(50, nameof(SettingsValues.MediumPowerTargetTemperature), "MediumPowerTargetTemprature"),
                HighPowerTargetTemperature = env.GetValueInt(70, nameof(SettingsValues.HighPowerTargetTemperature), "HighPowerTargetTemprature"),
                PowerZone = env.GetValue(nameof(SettingsValues.PowerZone)),
                EnergiBasedPeakTimes = env.GetValue(nameof(SettingsValues.EnergiBasedPeakTimes), "weekday6,weekday21,weekday22,weekend11,weekend23"),

                MQTTServer = env.GetValue(nameof(SettingsValues.MQTTServer)),
                MQTTServerPort = env.GetValueInt(nameof(SettingsValues.MQTTServerPort), 1883),
                MQTTUserName = env.GetValue(nameof(SettingsValues.MQTTUserName)),
                MQTTPassword = env.GetValue(nameof(SettingsValues.MQTTPassword)),
                MQTTLogLevel = env.GetValueEnum(LogEventLevel.Warning, nameof(SettingsValues.MQTTLogLevel)),
                ConsoleLogLevel = env.GetValueEnum(LogEventLevel.Information, nameof(SettingsValues.ConsoleLogLevel), "LogLevel")
            };

            if (string.IsNullOrEmpty(Settings.Instance.UserName))
            {
                Log.Logger.Error("UserName setting cannot be null or empty");
                return;
            }

            if (string.IsNullOrEmpty(Settings.Instance.Password))
            {
                Log.Logger.Error("Password setting cannot be null or empty");
                return;
            }

            if (Settings.Instance.WaterHeaterMaxPowerInHours is 0 and 0)
            {
                Log.Logger.Error("WaterHeaterMaxPowerInHours and WaterHeaterMaxPowerInHours are both set to 0, aborting");
                return;
            }

            if (Settings.Instance.CheckRemoteStatsIntervalInMinutes <= 0)
            {
                Log.Logger.Error("CheckRemoteStatsIntervalInMinutes settings value is invalid, cannot be 0 or lower. Was {CheckRemoteStatsIntervalInMinutes}", Settings.Instance.CheckRemoteStatsIntervalInMinutes);
                Settings.Instance.CheckRemoteStatsIntervalInMinutes = 1;
            }

            if (!IsValidTempratureSelection(Settings.Instance.MediumPowerTargetTemperature))
            {
                Log.Logger.Error("MediumPowerTargetTemprature is not valid, expect value between 50 and 90, was {HighTemp}", Settings.Instance.MediumPowerTargetTemperature);
                Settings.Instance.MediumPowerTargetTemperature = 50;
            }

            if (!IsValidTempratureSelection(Settings.Instance.HighPowerTargetTemperature) || Settings.Instance.HighPowerTargetTemperature <= Settings.Instance.MediumPowerTargetTemperature)
            {
                Log.Logger.Error("HighPowerTargetTemprature is not valid, expect value between {MediumTemp} and 90, was {HighTemp}", Settings.Instance.MediumPowerTargetTemperature, Settings.Instance.HighPowerTargetTemperature);
                Settings.Instance.HighPowerTargetTemperature = 70;
            }

            Log.Logger.Information("Reporting to MQTT is: {status}", Settings.Instance.MQTTActive);

            if (!Settings.Instance.ChangeSchedule)
            {
                Log.Logger.Information("Automatic adjusting of schedule is disabled", Settings.Instance.MQTTActive);
            }
            else
            {
                if (Settings.Instance.EnergiBasedCostSaving)
                    Log.Logger.Information("Automatic adjusting of schedule  will follow energi based rules", Settings.Instance.MQTTActive);
                else
                    Log.Logger.Information("Automatic adjusting of schedule  will follow price rules", Settings.Instance.MQTTActive);
            }

            if(Enum.TryParse<PowerZoneName>(Settings.Instance.PowerZone,out PowerZoneName result))
            {
                Settings.Instance.InternalPowerZone= result;
            }
            else
            {
                Log.Logger.Error("Failed to get powerzone name, from " + Settings.Instance.PowerZone, Settings.Instance.MQTTActive);
                var posiblePowerZoens = Enum.GetValues<PowerZoneName>();
                foreach(var zone in posiblePowerZoens) 
                {
                    Log.Logger.Error("Posible valid name: " + zone, Settings.Instance.MQTTActive);
                }
            }

            if (string.IsNullOrEmpty(env.GetValue("IsInsideDocker")))
            {
                try
                {
                    // Its ok to fail to update settings file.
                    File.WriteAllText(settingsFile, JsonSerializer.Serialize(Settings.Instance, new JsonSerializerOptions() { WriteIndented = true, PropertyNameCaseInsensitive = true }));
                }
                catch
                {
                    Log.Logger.Debug($"Failed to update setting file {settingsFile}");
                }
            }

            _backgroundJobs.Start();
        }

        static bool IsValidTempratureSelection(int value)
        {
            if (value < 50)
                return false;

            if (value > 90)
                return false;

            return true;
        }

        static string GetVersion()
        {
            try
            {
                string version = Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString() ?? "0.0.0.0";
                return version;
            }
            catch
            {
                return "";
            }
        }
    }
}