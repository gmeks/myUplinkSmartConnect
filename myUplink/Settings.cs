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

        public int WaterHeaterMaxPowerInHours { get; set; }

        public int WaterHeaterMediumPowerInHours { get; set; }

        public string? MTQQServer { get; set; }

        public int MTQQServerPort { get; set; }

        public string? MTQQUserName { get; set; }

        public string? MTQQPassword { get; set; }

        [JsonIgnore]
        public myuplinkApi? myuplinkApi { get; set; }
    }
}
