
using MyUplinkSmartConnect.Models;
using Serilog;
using Serilog.Events;
using System.Net.Http.Headers;
using System.Text.Json;
using Topshelf;

namespace MyUplinkSmartConnect
{
    public class Program
    {
        public static  void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Debug).CreateLogger();

            HostFactory.Run(x =>
            {
                x.Service<PriceWatchService>();
                x.EnableServiceRecovery(r => r.RestartService(TimeSpan.FromSeconds(10)));
                x.SetServiceName("MyUplink-smartconnect");
                x.UseSerilog();
                x.StartAutomatically();
            });            
        }
   }
}
