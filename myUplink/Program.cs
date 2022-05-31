
using MyUplinkSmartConnect.Models;
using Serilog;
using Serilog.Events;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;
using Topshelf;

namespace MyUplinkSmartConnect
{
    public class Program
    {
        public static  void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information).CreateLogger();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                )
            {
                var service = new PriceWatchService();
                service.Start(null);

                while(true)
                {
                    var result = Console.ReadLine();
                    if (!string.IsNullOrEmpty(result))
                        break;

                    Thread.Sleep(100);
                }
            }
            else
            {
                HostFactory.Run(x =>
                {
                    x.Service<PriceWatchService>();
                    x.EnableServiceRecovery(r => r.RestartService(TimeSpan.FromSeconds(10)));
                    x.SetServiceName("MyUplink-smartconnect");
                    x.UseSerilog();
                    x.StartAutomaticallyDelayed();
                });
            }                      
        }
   }
}
