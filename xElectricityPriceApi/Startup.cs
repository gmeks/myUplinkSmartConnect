using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Net.Http.Headers;
using xElectricityPriceApi.BackgroundJobs;
using xElectricityPriceApi.Services;

namespace xElectricityPriceApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            //Console.WriteLine($"HttpCache busting ID set to: {Settings.HttpCacheID}");
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            services.AddControllersWithViews();
            services.AddRazorPages();

#if DEBUG
            string databaseFile = "xElectricityPrice.db";
#else
            string databaseFile = "/data/xElectricityPrice.db";
#endif

            var idatabase = new LiteDBService(databaseFile);
            services.AddSingleton<LiteDBService>(idatabase);
            services.AddSingleton<PriceService>();


            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            
            services.AddLogging();
            services.AddResponseCompression(opts =>
            {
                opts.EnableForHttps = true;
                opts.MimeTypes = _compressedMimeTypes;
            });


            services.AddHangfire(config =>
            {
                config.UseMemoryStorage();
            });
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //                app.UseExceptionHandler();
            }
            
            app.UseSwagger(c =>
            {
                c.SerializeAsV2 = true;
            });
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "VesselSource API v1"));
            app.UseResponseCompression();

#if DEBUG
            app.UseHttpsRedirection();
#endif

            app.UseRouting();
            //app.UseCors("default");
            app.UseAuthorization();
            app.UseAuthentication();


            GlobalConfiguration.Configuration.UseActivator(new HangfireActivator(serviceProvider));
            //app.UseHangfireDashboard();
            app.UseHangfireServer();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            //SetupLogger();
            ConfigureBackgroundJobs(serviceProvider);
        }

        void ConfigureBackgroundJobs(IServiceProvider serviceProvider)
        {
            RecurringJob.AddOrUpdate<UpdatePrices>("Update prices", o => o.Work(), "0 1 13 1/1 * ?");

            var priceService = serviceProvider.GetRequiredService<PriceService>();
            if (priceService.AveragePriceCount == 0)
            {
                RecurringJob.TriggerJob("Update prices");
            }
#if DEBUG
            else
            {
                RecurringJob.TriggerJob("Update prices");
            }            
#endif
        }

        static readonly IEnumerable<string> _compressedMimeTypes = new[] { "application/octet-stream", "text/plain", "text/css", "application/javascript", "text/html", "application/xml", "text/xml", "application/json", "text/json", };
    }
}
