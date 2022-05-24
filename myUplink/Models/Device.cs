namespace MyUplinkSmartConnect.Models
{
    public class Device
    {
        public string? id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public string? type { get; set; }
        public string? role { get; set; }
        public object? tags { get; set; }
        public object? serialNumber { get; set; }
        public string? deviceAccess { get; set; }
    }
}
