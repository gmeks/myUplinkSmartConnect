using Serilog;
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
        readonly BackgroundJobSupervisor _backgroundJobs;

        public PriceWatchService()
        {
            _backgroundJobs = new BackgroundJobSupervisor();
        }

        public bool Start(HostControl hostControl)
        {          
            if (!File.Exists(settingsFile))
            {
                Log.Logger.Error($"No settings file found {settingsFile}");
                return false;
            }

            Settings.Instance = JsonSerializer.Deserialize<SettingsValues>(File.ReadAllText(settingsFile)) ?? new SettingsValues();
            if (Settings.Instance.WaterHeaterMaxPowerInHours == 0 && Settings.Instance.WaterHeaterMaxPowerInHours == 0)
            {
                Log.Logger.Error("WaterHeaterMaxPowerInHours and WaterHeaterMaxPowerInHours are both set to 0, aborting");
                return false;
            }

            if (Settings.Instance.CheckRemoteStatsIntervalInMinutes == 0)
                Settings.Instance.CheckRemoteStatsIntervalInMinutes = 1;

            if (Settings.Instance.MQTTServerPort == 0)
                Settings.Instance.MQTTServerPort = 1883;

            File.WriteAllText(settingsFile, JsonSerializer.Serialize(Settings.Instance,new JsonSerializerOptions() { WriteIndented = true, PropertyNameCaseInsensitive=true }));
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
