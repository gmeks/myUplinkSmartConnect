using System.Text.Json.Serialization;

namespace myUplink.Models
{
    public enum WaterheaterSettingsMode
    {
        TargetTempratureSetpoint = 1,
        TargetHeaterWatt = 2,
    }

    public enum WaterHeaterDesiredPower
    {
        None = 0,
        Watt700 = 1,
        Watt1300 = 2,
        Watt2000 = 3,
    }

    public class WaterHeaterModeSetting
    {
        public WaterheaterSettingsMode settingId { get; set; }
        public int value { get; set; }

        [JsonIgnore]
        public WaterHeaterDesiredPower HelperDesiredHeatingPower
        {
            get
            {
                return (WaterHeaterDesiredPower)value;
            }
        }
    }
}
