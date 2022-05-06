using Hangfire;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Topshelf;
using Hangfire.LiteDB;

namespace MyUplinkSmartConnect
{
    internal class PriceWatchService : ServiceControl
    {
        BackgroundJobServer _server;
#if DEBUG
        const string settingsFile = "appsettings.Development.json";
#else
        const string settingsFile = "appsettings.json";         
#endif
        public PriceWatchService()
        {
            GlobalConfiguration.Configuration.LiteDbStorage("myuplink-hangfire.db");
            var options = new BackgroundJobServerOptions
            {
                // This is the default value
                WorkerCount = 2
            };
            _server = new BackgroundJobServer(options);            
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
            File.WriteAllText(settingsFile, JsonSerializer.Serialize(Settings.Instance,new JsonSerializerOptions() { WriteIndented = true, PropertyNameCaseInsensitive=true }));
            Settings.Instance.myuplinkApi = new myuplinkApi();

            RecurringJob.AddOrUpdate("Reschedule heaters", () => JobReScheuleheating.Work(), Cron.Daily(14,45));

            if(!string.IsNullOrEmpty(Settings.Instance.MQTTServer))
            {
                RecurringJob.AddOrUpdate("Heaters status", () => new JobCheckHeaterStatus().Work(), Cron.MinuteInterval(10));
                RecurringJob.Trigger("Heaters status");
            }

#if DEBUG
            //RecurringJob.Trigger("Reschedule heaters");
#endif
            return true;
        }       

        public bool Stop(HostControl hostControl)
        {
            Log.Logger.Information("MyUplink-smartconnect is stopping");
            _server.Dispose();
            return true;
        }
    }
}
