
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;
using MyUplinkSmartConnect.Services;
using System;

namespace MyUplinkSmartConnect
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Settings.Instance.ConsoleLogLevel = LogEventLevel.Information;
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            
            var service = Host.CreateDefaultBuilder(args).ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<MyUplinkSmartconnect>();
                services.AddLogging();
            });
            
            service.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSerilog();
            });
            
            Settings.ServiceLookup = new ServiceCollection()
         .AddSingleton<ScheduleAdjustService>()
         .AddSingleton<MQTTService>()
         .AddSingleton<MyUplinkService>()
         .AddSingleton<CurrentStateService>()
         .BuildServiceProvider();

            //service.UseServiceProviderFactory(Settings.Instance.ServiceLookup);
            //Settings.Instance.ServiceLookup.GetService<ILoggerFactory>().AddProvider(Serilog.login)

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                service.UseWindowsService();                                
            }
            else
            {
                service.UseSystemd();                
            }

            await service.RunConsoleAsync();
        }
    }
}
