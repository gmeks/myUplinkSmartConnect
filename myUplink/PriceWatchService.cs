using Hangfire;
using Hangfire.MemoryStorage;
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
        BackgroundJobServer _server;
#if DEBUG
        const string settingsFile = "appsettings.Development.json";
#else
        const string settingsFile = "appsettings.json";         
#endif
        public PriceWatchService()
        {
            GlobalConfiguration.Configuration.UseMemoryStorage();
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

            Settings.Instance = JsonSerializer.Deserialize<SettingsValues>(File.ReadAllText(settingsFile));
            if (Settings.Instance.WaterHeaterMaxPowerInHours == 0 && Settings.Instance.WaterHeaterMaxPowerInHours == 0)
            {
                Log.Logger.Error("WaterHeaterMaxPowerInHours and WaterHeaterMaxPowerInHours are both set to 0, aborting");
                return false;
            }

            Settings.Instance.myuplinkApi = new myuplinkApi();

            RecurringJob.AddOrUpdate("Reschedule heaters", () => JobReScheuleheating.Work(), "0 0 17 * * ?");

            if(string.IsNullOrEmpty(Settings.Instance.MTQQServer))
            {
                RecurringJob.AddOrUpdate("Heaters status", () => new JobCheckHeaterStatus().Work(), Cron.MinuteInterval(10));
#if DEBUG
                RecurringJob.Trigger("Heaters status");
#endif
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
