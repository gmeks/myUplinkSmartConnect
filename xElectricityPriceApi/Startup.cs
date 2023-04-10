﻿using Hangfire;
using Hangfire.LiteDB.Entities;
using Hangfire.LiteDB;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Net.Http.Headers;
using xElectricityPriceApi.BackgroundJobs;
using xElectricityPriceApi.Services;
using Microsoft.EntityFrameworkCore;
using Hangfire.EntityFrameworkCore;

namespace xElectricityPriceApi
{
    public class Startup
    {
        SettingsService _settingsService;

        public Startup(IConfiguration configuration)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddConsole();
                builder.AddEventSourceLogger();
            });
            var logger = loggerFactory.CreateLogger<Startup>();
            _settingsService = new SettingsService(logger);
            Configuration = configuration;           
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        

            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddDbContext<DatabaseContext>(options => options.UseSqlite(_settingsService.GetSqlLightDatabaseConStr()).EnableSensitiveDataLogging(false));
            services.AddHangfire(config =>
            {
                //config.UseLiteDbStorage(liteDb.DatabaseInstance);
                config.UseSimpleAssemblyNameTypeSerializer();
                config.UseRecommendedSerializerSettings();
                config.UseSerilogLogProvider();
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
                config.UseEFCoreStorage(builder => builder.UseSqlite(_settingsService.GetSqlLightDatabaseConStr()), new EFCoreStorageOptions
                {
                    CountersAggregationInterval = new TimeSpan(0, 5, 0),
                    DistributedLockTimeout = new TimeSpan(0, 10, 0),
                    JobExpirationCheckInterval = new TimeSpan(0, 30, 0),
                    QueuePollInterval = new TimeSpan(0, 0, 15),
                    Schema = string.Empty,
                    SlidingInvisibilityTimeout = new TimeSpan(0, 5, 0),
                }).UseDatabaseCreator();
            });

            services.AddSingleton<SettingsService>();
            services.AddScoped<MQTTSenderService>();
            services.AddScoped<PriceService>();

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
            
            app.UseSwagger(c =>
            {
                c.SerializeAsV2 = true;
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


            GlobalConfiguration.Configuration.UseActivator(new HangfireActivator(serviceProvider));
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
            RecurringJob.AddOrUpdate<SendPriceInformation>(SendPriceInformation.HangfireJobDescription, o => o.WorkOncePrHour(), "0 * * * *"); 


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
        }

        private void UpdateDatabase(IApplicationBuilder app)
        {
            Serilog.Log.Logger.Information("Checking for database migration on database stored  in {path}", _settingsService.DatabasePath);
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetService<DatabaseContext>())
                {
                    context.Database.Migrate();

                    using (var connection = context.Database.GetDbConnection())
                    using (var command = connection.CreateCommand())
                    {
                        connection.Open();

                        command.CommandText = "PRAGMA journal_mode=WAL;";
                        command.ExecuteNonQuery();
                    }
                }
            }
        }


        static readonly IEnumerable<string> _compressedMimeTypes = new[] { "application/octet-stream", "text/plain", "text/css", "application/javascript", "text/html", "application/xml", "text/xml", "application/json", "text/json", };
    }
}
