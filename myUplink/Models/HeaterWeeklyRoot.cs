namespace MyUplinkSmartConnect.Models
{
    public class HeaterWeeklyRoot
    {
        public int weeklyScheduleId { get; set; }
        public string weekFormat { get; set; }
        public List<HeaterWeeklyEvent> events { get; set; }
    }
}
