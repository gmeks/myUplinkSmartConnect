using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;
using Serilog.Events;
using xElectricityPriceApiShared;
using static Dapper.SqlMapper;

namespace xElectricityPriceApi.Services
{
    public class SettingsService
    {
        readonly ILogger<object> _logger;

        public SettingsService(ILogger<object> logger)
        {
            _logger = logger;

            EnvVariables env = new EnvVariables(_logger);
            if (env.HasSetting("IsInsideDocker"))
            {
                MQTTServer = env.GetValue(nameof(MQTTServer));
                MQTTServerPort = env.GetValueInt(nameof(MQTTServerPort), 1883);
                MQTTUserName = env.GetValue(nameof(MQTTUserName));
                MQTTPassword = env.GetValue(nameof(MQTTPassword));

                DatabaseServer = env.GetValue(nameof(DatabaseServer));
                Database = env.GetValue(nameof(Database));
                DatabaseUser = env.GetValue(nameof(DatabaseUser));
                DatabasePassword = env.GetValue(nameof(DatabasePassword));
            }
            else
            {
                Database = "electricityprice_db";
                DatabaseServer = "192.168.50.19";
                DatabaseUser = "electricityprice";
                DatabasePassword = "Xbjdks98aaJl9";
            }
        }   

        public string GetConnectionStr()
        {
            return $"Host={DatabaseServer}; Database={Database}; Username={DatabaseUser}; Password={DatabasePassword}";          
        }

        public string Database { get; set; } = "";

        public string DatabaseServer { get; set; } = "";

        public string DatabaseUser { get; set; } = "";

        public string DatabasePassword { get; set; } = "";

        public string? MQTTServer { get; set; }

        public int MQTTServerPort { get; set; }

        public string? MQTTUserName { get; set; }

        public string? MQTTPassword { get; set; }
    }
}
