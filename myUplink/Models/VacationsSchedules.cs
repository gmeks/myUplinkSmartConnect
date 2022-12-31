using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect.Models
{
    public class VacationsSchedules
    {
        public bool isEnabled { get; set; }

        public int modeId { get; set; }

        public DateTime start { get; set; }

        public DateTime end { get; set; }
    }
}