namespace MyUplinkSmartConnect.Models
{
    public class WaterHeaterMode
    {
        public int modeId { get; set; }
        public string? name { get; set; }
        public List<WaterHeaterModeSetting>? settings { get; set; }
    }
}
