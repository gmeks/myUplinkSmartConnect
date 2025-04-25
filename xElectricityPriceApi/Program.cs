using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

namespace xElectricityPriceApi;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {              
                webBuilder.UseKestrel((context, options) =>
                {
                    options.ListenAnyIP(5097);
                });
                webBuilder.UseStartup<Startup>();
            }
            ).ConfigureLogging((context, logging) => {
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddConsole();
                logging.AddEventSourceLogger();
                logging.AddSentry(options =>
                {
                    options.Dsn = "https://58c7922933f040f65e0f395ca2eeb8c5@sentry.thexsoft.com/4";
                    options.AttachStacktrace = true;
                    options.InitializeSdk = true;
                    options.MinimumBreadcrumbLevel = LogLevel.Debug;
                    options.MinimumEventLevel = LogLevel.Error;
#if DEBUG
                    options.Debug = true;
                    options.DiagnosticLevel = Sentry.SentryLevel.Debug;
#else
                    options.DiagnosticLevel = Sentry.SentryLevel.Error;
#endif
                    options.TracesSampleRate = 1;
                });
                logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
            });
}
