using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myUplink.Models
{
    public class DeviceInfoEnumValue
    {
        public string value { get; set; }
        public string text { get; set; }
        public string icon { get; set; }
    }

    public class DeviceInfo
    {
        public string category { get; set; }
        public string parameterId { get; set; }
        public string parameterName { get; set; }
        public string parameterUnit { get; set; }
        public bool writable { get; set; }
        public DateTime timestamp { get; set; }
        public double value { get; set; }
        public string strVal { get; set; }
        public List<object> smartHomeCategories { get; set; }
        public int? minValue { get; set; }
        public int? maxValue { get; set; }
        public List<DeviceInfoEnumValue> enumValues { get; set; }
        public string scaleValue { get; set; }
    }
}
