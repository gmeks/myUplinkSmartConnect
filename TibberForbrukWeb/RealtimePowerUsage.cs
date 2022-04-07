using System;

namespace TibberForbrukWeb
{
    public class RealtimePowerUsage
    {
        public int Watt { get; set; }

        public int Volt { get; set; }

        public int Amp { get; set; }

        public DateTime Timestamp { get; set; }

        public string strTimestamp { get; set; }
    }
}
