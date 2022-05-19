using MyUplinkSmartConnect.Models;
using System.Text.Json.Serialization;

namespace MyUplinkSmartConnect
{
    static class Settings
    {
        public static SettingsValues Instance { get; set; }
    }

    class SettingsValues
    {
        public string? UserName { get; set; }

        public string? Password { get; set; }

        public int CheckRemoteStatsIntervalInMinutes { get; set; } = 1;

        public int WaterHeaterMaxPowerInHours { get; set; }

        public int WaterHeaterMediumPowerInHours { get; set; }

        public string? MQTTServer { get; set; }

        public int MQTTServerPort { get; set; }

        public string? MQTTUserName { get; set; }

        public string? MQTTPassword { get; set; }

        [JsonIgnore]
        public myuplinkApi? myuplinkApi { get; set; }
    }
}
