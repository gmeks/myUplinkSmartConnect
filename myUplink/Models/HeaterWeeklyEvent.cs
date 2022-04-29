using System.Text.Json.Serialization;

namespace myUplink.Models
{
    public class HeaterWeeklyEvent : IEquatable<HeaterWeeklyEvent>
    {
        public bool enabled { get; set; }
        public int modeId { get; set; }
        public string startDay { get; set; }
        public string startTime { get; set; }
        public object stopDay { get; set; }
        public object stopTime { get; set; }

        [JsonIgnore]
        public DayOfWeek Day
        {
            get
            {
                return Enum.Parse<DayOfWeek>(startDay);
            }
        }

        public bool Equals(HeaterWeeklyEvent? other)
        {
            if (other == null)
                return false;

            if (this.modeId == other.modeId && this.startDay == other.startDay && this.startTime == other.startTime)
                return true;

            return false;
        }
    }
}
