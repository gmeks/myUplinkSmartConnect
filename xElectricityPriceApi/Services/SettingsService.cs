using Microsoft.AspNetCore.Mvc.Rendering;
using Serilog.Events;
using xElectricityPriceApiShared;

namespace xElectricityPriceApi.Services
{
    public class SettingsService
    {
        readonly ILogger<object> _logger;

        public SettingsService(ILogger<object> logger)
        {
            _logger = logger;

            EnvVariables env = new EnvVariables(_logger);
            MQTTServer = env.GetValue(nameof(MQTTServer));
            MQTTServerPort = env.GetValueInt(nameof(MQTTServerPort), 1883);
            MQTTUserName = env.GetValue(nameof(MQTTUserName));
            MQTTPassword = env.GetValue(nameof(MQTTPassword));
        }

        public string GetSqlLightDatabaseConStr()
        {
            //MQTTLogLevel = env.GetValueEnum(LogEventLevel.Warning, nameof(MQTTLogLevel));

            //Data Source=C:\SQLITEDATABASES\SQLITEDB1.sqlite;Version=3;
            if (!Directory.Exists(DatabasePath))
                Directory.CreateDirectory(DatabasePath);

            string databseName = Path.Combine(DatabasePath, "Database.db");
            return $"Data Source={databseName}";
        }

        public string DatabasePath { get; set; } = "d:\\temp\\";

        public string? MQTTServer { get; set; }

        public int MQTTServerPort { get; set; }

        public string? MQTTUserName { get; set; }

        public string? MQTTPassword { get; set; }
    }
}
