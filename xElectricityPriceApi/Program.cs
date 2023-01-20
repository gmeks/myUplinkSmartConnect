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
#if !DEBUG
                webBuilder.UseKestrel((context, options) =>
                {
                    options.ListenAnyIP(5097);
                });
#endif
                webBuilder.UseStartup<Startup>();
            })
        .ConfigureLogging((context, logging) => {
            logging.ClearProviders();
            logging.AddSerilog(SetupLogger());
        });


    static Serilog.Core.Logger SetupLogger()
    {
        var log = new LoggerConfiguration().MinimumLevel.Information()
            // .Enrich.WithMachineName()
            //.Enrich.WithProcessName()                
            //.Enrich.WithThreadId()
            //.Enrich.WithMemoryUsage()
            .Enrich.FromLogContext()
            //.Enrich.WithExceptionDetails()
            //.Enrich.WithProperty("Application", "Serilog Test Application")
            .WriteTo.Console(LogEventLevel.Warning).CreateLogger();

        return log;
    }
}
