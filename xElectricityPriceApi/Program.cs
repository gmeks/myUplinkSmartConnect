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
                webBuilder.UseSentry(options =>
                {
                    options.Dsn = "https://58c7922933f040f65e0f395ca2eeb8c5@sentry.thexsoft.com/4";                    
                    options.AttachStacktrace = true;
                    options.InitializeSdk = true;
                    options.MaxRequestBodySize = Sentry.Extensibility.RequestSize.Always;
                    options.MinimumBreadcrumbLevel = LogLevel.Debug;
                    options.MinimumEventLevel = LogLevel.Error;
                    options.Debug = true;
#if DEBUG
                    options.DiagnosticLevel = Sentry.SentryLevel.Debug;
#else
                        options.DiagnosticLevel = Sentry.SentryLevel.Error;
#endif
                    options.TracesSampleRate = 1;
                });
                webBuilder.UseKestrel((context, options) =>
                {
                    options.ListenAnyIP(5097);
                });
                webBuilder.UseStartup<Startup>();
            }
            ).ConfigureLogging((context, logging) => {
                //logging.ClearProviders();
                //logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
            });
}
