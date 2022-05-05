using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect.Models
{
    internal class CurrentValues
    {
        public string parameterId { get; set; }
        public int rawValue { get; set; }
        public string kind { get; set; }
        public double value { get; set; }
        public string unit { get; set; }
        public DateTime timestamp { get; set; }
        public double? minVal { get; set; }
        public double? maxVal { get; set; }
    }
}
