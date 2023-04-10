using NodaTime;
using System.Diagnostics;
using System.Text.Json.Serialization;
using xElectricityPriceApiShared;

namespace MyUplinkSmartConnect.Models
{
    public class HeaterWeeklyEvent : IEquatable<HeaterWeeklyEvent>
    {
        public HeaterWeeklyEvent()
        {
            enabled = true;
            phantom_id = Guid.NewGuid();
        }

        public HeaterWeeklyEvent(DateTime start,int heatingModeId,bool hasPriceInformation)
        {
            enabled = true;
            phantom_id = Guid.NewGuid();

            startDay = start.DayOfWeek.ToString();
            startTime = start.ToString("HH:mm:ss");
            modeId = heatingModeId;
            Date = start;
            HasPriceInformation = hasPriceInformation;
        }

        public bool enabled { get; set; }
        public int modeId { get; set; }
        public string? startDay { get; set; }
        public string? startTime { get; set; }
        public string? stopDay { get; set; }
        public string? stopTime { get; set; }
        public Guid phantom_id { get; set; }

        [JsonIgnore]
        public bool HasPriceInformation { get; set; }

        [JsonIgnore]
        public DateTime Date
        {
            get;set;
        }

        [JsonIgnore]
        public DayOfWeek Day
        {
            get
            {
                if(string.IsNullOrEmpty(startDay))
                    throw new NullReferenceException("startDay");

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
