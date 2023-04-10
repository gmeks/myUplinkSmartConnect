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
                /*
                webBuilder.UseSentry(options =>
                {
                    options.Dsn = "https://bed1c64fb3a844518228a6a16ea10e84@sentry.thexsoft.com/3";
                    options.EnableTracing = true;
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
                */
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
