
using Microsoft.Extensions.Logging;
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
            var service = Host.CreateDefaultBuilder(args).ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<PriceWatchService>();
            });

            service.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSerilog();
            });

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
