using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
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
            Instance = new SettingsValue();
            EnvVariables env = new EnvVariables(_logger);
            if (env.HasSetting("IsInsideDocker"))
            {
                _logger.LogInformation("Settings are being read from docker/Envrioment variables");
                Instance.MQTTServer = env.GetValue(nameof(Instance.MQTTServer));
                Instance.MQTTServerPort = env.GetValueInt(nameof(Instance.MQTTServerPort), 1883);
                Instance.MQTTUserName = env.GetValue(nameof(Instance.MQTTUserName));
                Instance.MQTTPassword = env.GetValue(nameof(Instance.MQTTPassword));

                Instance.DatabaseServer = env.GetValue(nameof(Instance.DatabaseServer));
                Instance.Database = env.GetValue(nameof(Instance.Database));
                Instance.DatabaseUser = env.GetValue(nameof(Instance.DatabaseUser));
                Instance.DatabasePassword = env.GetValue(nameof(Instance.DatabasePassword));

                Instance.OnlyFetchPriceEUApi = env.GetValueBool(nameof(Instance.OnlyFetchPriceEUApi),true);

                Instance.PowerZoneName = env.GetValueEnum<PowerZoneName>(PowerZoneName.NO2, nameof(Instance.PowerZoneName));
            }
            else
            {
                _logger.LogInformation("Settings are being read from config file");
                if (System.IO.File.Exists("appsettings.Development.json"))
                {
                    var systemText = System.IO.File.ReadAllText("appsettings.Development.json");
                    var tmpSettings = System.Text.Json.JsonSerializer.Deserialize<SettingsValue>(systemText);

                    if (tmpSettings != null)
                    {
                        Instance = tmpSettings;
                    }
                }                
            }


            _logger.LogInformation("Connecting to MQTT Server {server}:{port}", Instance.MQTTServer, Instance.MQTTServerPort);
            _logger.LogInformation("Database server: {databaseserver}", Instance.DatabaseServer);
            _logger.LogInformation("Price zone: {databaseserver}", Instance.PowerZoneName);
            _logger.LogInformation("Price api EU only: {OnlyFetchPriceEUApi}", Instance.OnlyFetchPriceEUApi);
        }   

        public string GetConnectionStr()
        {
            return $"Host={Instance.DatabaseServer}; Database={Instance.Database}; Username={Instance.DatabaseUser}; Password={Instance.DatabasePassword}";          
        }

        public SettingsValue Instance { get; set; }
    }

    public class SettingsValue
    {
        public string Database { get; set; } = "";

        public string DatabaseServer { get; set; } = "";

        public string DatabaseUser { get; set; } = "";

        public string DatabasePassword { get; set; } = "";

        public string? MQTTServer { get; set; }

        public int MQTTServerPort { get; set; }

        public string? MQTTUserName { get; set; }

        public string? MQTTPassword { get; set; }

        public bool OnlyFetchPriceEUApi { get; set; }

        public PowerZoneName PowerZoneName { get; set; }
    }
}
