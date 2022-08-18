
using MyUplinkSmartConnect.Models;
using MyUplinkSmartConnect.MQTT;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;

namespace MyUplinkSmartConnect
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = Settings.CreateLogger(LogEventLevel.Information);
            MainAsync(args).GetAwaiter().GetResult();
        }


        private static async Task MainAsync(string[] args)
        {
            var service = Host.CreateDefaultBuilder(args)
                         .ConfigureServices((hostContext, services) =>
                         {
                             services.AddHostedService<PriceWatchService>();
                         });

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                service.UseWindowsService();                
                await service.RunConsoleAsync();
            }
            else
            {
                service.UseSystemd();
                await service.RunConsoleAsync();

                /*
                while (true)
                {
                    var result = Console.ReadLine();

                    if (!string.IsNullOrEmpty(result))
                    {
                        if (result.StartsWith("d"))
                        {
                            Console.WriteLine("Enabling debug logging");
                            Log.Logger = Settings.CreateLogger(LogEventLevel.Debug);
                            continue;
                        }

                        break;
                    }

                    Thread.Sleep(100);
                }
                */
            }
        }
    }
}
