using Hangfire;
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

            services.AddDbContext<DatabaseContext>(options => options.UseSqlite(Settings.GetSqlLightDatabaseConStr()).EnableSensitiveDataLogging(false));
            services.AddHangfire(config =>
            {
                //config.UseLiteDbStorage(liteDb.DatabaseInstance);
                config.UseSimpleAssemblyNameTypeSerializer();
                config.UseRecommendedSerializerSettings();
                config.UseSerilogLogProvider();
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
                config.UseEFCoreStorage(builder => builder.UseSqlite(Settings.GetSqlLightDatabaseConStr()), new EFCoreStorageOptions
                {
                    CountersAggregationInterval = new TimeSpan(0, 5, 0),
                    DistributedLockTimeout = new TimeSpan(0, 10, 0),
                    JobExpirationCheckInterval = new TimeSpan(0, 30, 0),
                    QueuePollInterval = new TimeSpan(0, 0, 15),
                    Schema = string.Empty,
                    SlidingInvisibilityTimeout = new TimeSpan(0, 5, 0),
                }).UseDatabaseCreator();
            });
            services.AddScoped<PriceService>();


            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            
            services.AddLogging();
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
            RecurringJob.AddOrUpdate<UpdatePrices>("Update prices", o => o.Work(), "2 13 * * *");

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

        private static void UpdateDatabase(IApplicationBuilder app)
        {
            Serilog.Log.Logger.Information("Checking for database migration on database stored  in {path}", Settings.DatabasePath);
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
