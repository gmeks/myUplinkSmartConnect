using Microsoft.EntityFrameworkCore;
using xElectricityPriceApi.Models;
using NodaTime;
using Npgsql;
using xElectricityPriceApi.Services;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace xElectricityPriceApi
{
    public class DatabaseContext : DbContext
    {
        readonly SettingsService _settingsService;

        public DatabaseContext(DbContextOptions<DatabaseContext> options, SettingsService settingsService) : base(options)
        {
            _settingsService = settingsService;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            /*
            var localDateConverter =
                new ValueConverter<LocalDate, DateTime>(v => v.ToDateTimeUnspecified(),
                v => LocalDate.FromDateTime(v));
            */
            modelBuilder.ApplyUtcDateTimeConverter();//Put before seed data and after model creation
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.EnableSensitiveDataLogging(false);
            optionsBuilder.EnableDetailedErrors();

            //optionsBuilder.UseNpgsql(_settingsService.GetConnectionStr(), o => o.UseNodaTime());
            //optionsBuilder.ConfigureWarnings(b => b.Log((RelationalEventId.ConnectionOpened, LogLevel.Information),(RelationalEventId.ConnectionClosed, LogLevel.Information)));
        }
        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
            //builder.Properties<FeatureTags[]>().HaveConversion<EnumConverter>().HaveColumnType("string");
            base.ConfigureConventions(builder);
            //builder.DefaultTypeMapping<GroupByException>();
        }

        public DbSet<AveragePrice> AveragePrice { get; set; }

        public DbSet<PriceInformation> PriceInformation { get; set; }
        
    }
}