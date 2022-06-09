using MyUplinkSmartConnect.ExternalPrice;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Topshelf;


namespace MyUplinkSmartConnect
{
    internal class PriceWatchService : ServiceControl
    {
#if DEBUG
        const string settingsFile = "appsettings.Development.json";
#else
        const string settingsFile = "appsettings.json";         
#endif
        BackgroundJobSupervisor _backgroundJobs;

        public PriceWatchService()
        {
            
        }

        public bool Start(HostControl hostControl)
        {
            Log.Logger.Information("Starting up service");

            var env = new EnvVariables();
            if(env.GetValue("IsInsideDocker") != null)
            {
                Settings.Instance = new SettingsValues();
                Settings.Instance.UserName = env.GetValue("UserName");
                Settings.Instance.Password = env.GetValue("Password");
                Settings.Instance.CheckRemoteStatsIntervalInMinutes = env.GetValueInt("CheckRemoteStatsIntervalInMinutes");
                Settings.Instance.WaterHeaterMaxPowerInHours = env.GetValueInt("WaterHeaterMaxPowerInHours");
                Settings.Instance.WaterHeaterMediumPowerInHours = env.GetValueInt("WaterHeaterMediumPowerInHours");
                Settings.Instance.PowerZone = env.GetValue("PowerZone");
                Settings.Instance.MQTTServer = env.GetValue("MQTTServer");
                Settings.Instance.MQTTServerPort = env.GetValueInt("MQTTServerPort");
                Settings.Instance.MQTTUserName = env.GetValue("MQTTUserName");
                Settings.Instance.MQTTPassword = env.GetValue("MQTTPassword");

                Settings.Instance.LogLevel = env.GetValueEnum<LogEventLevel>("LogLevel", LogEventLevel.Information);

                Log.Logger.Information("Reading settings from enviromental variables");
            }
            else
            {
                if (!File.Exists(settingsFile))
                {
                    Log.Logger.Error($"No settings file found {settingsFile}");
                    return false;
                }

                Settings.Instance = JsonSerializer.Deserialize<SettingsValues>(File.ReadAllText(settingsFile)) ?? new SettingsValues();
                Log.Logger.Information("Reading settings from configuration file");
            }

            if(Settings.Instance.LogLevel != LogEventLevel.Information)
            {
                Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console(restrictedToMinimumLevel: Settings.Instance.LogLevel).CreateLogger();
            }

            if (string.IsNullOrEmpty(Settings.Instance.UserName))
            {
                Log.Logger.Error("UserName setting cannot be null or ematy");
                return false;
            }

            if (string.IsNullOrEmpty(Settings.Instance.Password))
            {
                Log.Logger.Error("Password setting cannot be null or ematy");
                return false;
            }

            if (Settings.Instance.WaterHeaterMaxPowerInHours == 0 && Settings.Instance.WaterHeaterMaxPowerInHours == 0)
            {
                Log.Logger.Error("WaterHeaterMaxPowerInHours and WaterHeaterMaxPowerInHours are both set to 0, aborting");
                return false;
            }

            if (Settings.Instance.CheckRemoteStatsIntervalInMinutes == 0)
                Settings.Instance.CheckRemoteStatsIntervalInMinutes = 1;

            if (Settings.Instance.MQTTServerPort == 0)
                Settings.Instance.MQTTServerPort = 1883;

            if(env.GetValue("IsInsideDocker") == null)
            {
                try
                {
                    // Its ok to fail to update settings file.
                    File.WriteAllText(settingsFile, JsonSerializer.Serialize(Settings.Instance, new JsonSerializerOptions() { WriteIndented = true, PropertyNameCaseInsensitive = true }));
                }
                catch
                {
                    Log.Logger.Verbose($"Failed to update setting file {settingsFile}");
                }
            }
            
            _backgroundJobs = new BackgroundJobSupervisor();
            Settings.Instance.myuplinkApi = new myuplinkApi();

            _backgroundJobs.Start();
            return true;
        }       

        public bool Stop(HostControl hostControl)
        {
            Log.Logger.Information("MyUplink-smartconnect is stopping");
            _backgroundJobs.Stop();
            return true;
        }
    }
}
