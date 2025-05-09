﻿using Hangfire;
using Hangfire.PostgreSql;
using xElectricityPriceApi.BackgroundJobs;
using xElectricityPriceApi.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Hangfire.EntityFrameworkCore;
using System;

namespace xElectricityPriceApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {    
            Configuration = configuration;           
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            //var dataSource = dataSourceBuilder.;
            var settingsService = new SettingsService();

            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddSingleton<SettingsService>(settingsService);

            services.AddDbContext<DatabaseContext>(opt =>opt.UseNpgsql(settingsService.GetConnectionStr()));            
            services.AddScoped<MQTTSenderService>();
            services.AddScoped<PriceService>();

            //services.AddDbContext<DatabaseContext>(_settingsService.GetConnection().CreationOptions);
            services.AddHangfire(config =>
            {
                //config.UseLiteDbStorage(liteDb.DatabaseInstance);
                config.UseSimpleAssemblyNameTypeSerializer();
                config.UseRecommendedSerializerSettings();
                config.UseSerilogLogProvider();
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
                config.UseEFCoreStorage(builder => builder.UseNpgsql(settingsService.GetConnectionStr()), new EFCoreStorageOptions
                {
                    CountersAggregationInterval = new TimeSpan(0, 5, 0),
                    DistributedLockTimeout = new TimeSpan(0, 10, 0),
                    JobExpirationCheckInterval = new TimeSpan(0, 30, 0),
                    QueuePollInterval = new TimeSpan(0, 0, 15),
//                    PrepareSchemaIfNecessary = true,
 //                   SchemaName = ["Schema"]
                    SlidingInvisibilityTimeout = new TimeSpan(0, 5, 0),
                }).UseDatabaseCreator();
            });



            services.AddHangfire(config => config.UsePostgreSqlStorage(settingsService.GetConnectionStr()));
            services.AddHangfireServer();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            services.AddResponseCompression(opts =>
            {
                opts.EnableForHttps = true;
                opts.MimeTypes = _compressedMimeTypes;
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

            app.UseSentryTracing();
            app.UseSwagger(c =>
            {
            });
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "VesselSource API v1"));
            app.UseResponseCompression();

#if !DEBUG
            app.UseHttpsRedirection();
#endif

            app.UseRouting();
            //app.UseCors("default");
            app.UseAuthorization();
            app.UseAuthentication();
            app.UseHangfireDashboard();

            //GlobalConfiguration.Configuration.UseActivator(new HangfireActivator(serviceProvider));
            //app.UseHangfireDashboard();
            app.UseHangfireDashboard("/HangFireDashboard", new DashboardOptions
            {
                //Authorization = new[] { new HangFireAuthorizeFilter() },
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            //SetupLogger();
            UpdateDatabase(app);
            ConfigureBackgroundJobs(serviceProvider);
        }

        void ConfigureBackgroundJobs(IServiceProvider serviceProvider)
        {
            RecurringJob.AddOrUpdate<UpdatePrices>(UpdatePrices.HangfireJobDescription, o => o.Work(), "2 13 * * *");
            RecurringJob.AddOrUpdate<WorkOncePrHour>(WorkOncePrHour.HangfireJobDescription, o => o.Work(), "0 * * * *");
            RecurringJob.AddOrUpdate<PriceOncePrDay>(PriceOncePrDay.HangfireJobDescription, o => o.Work(),Cron.Daily()); 

            var priceService = serviceProvider.GetRequiredService<PriceService>();
            if (priceService.AveragePriceCount == 0)
            {
                RecurringJob.TriggerJob(UpdatePrices.HangfireJobDescription);
            }
#if DEBUG
            else
            {
                RecurringJob.TriggerJob(UpdatePrices.HangfireJobDescription);
            }
#endif
            RecurringJob.TriggerJob(WorkOncePrHour.HangfireJobDescription); // We always send price information in startup
        }

        private void UpdateDatabase(IApplicationBuilder app)
        {            
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var settingsService = serviceScope.ServiceProvider.GetService<SettingsService>();
                Serilog.Log.Logger.Information("Checking for database migration on database stored  in {path}", settingsService.Instance.Database);

                using (var context = serviceScope.ServiceProvider.GetService<DatabaseContext>())
                {
                    context?.Database.Migrate();            
                }
            }
        }


        static readonly IEnumerable<string> _compressedMimeTypes = new[] { "application/octet-stream", "text/plain", "text/css", "application/javascript", "text/html", "application/xml", "text/xml", "application/json", "text/json", };
    }
}
