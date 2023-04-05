using Microsoft.EntityFrameworkCore;
using xElectricityPriceApi.Models;

namespace xElectricityPriceApi
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging(false);
            optionsBuilder.EnableDetailedErrors();

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