using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect.Models
{
    enum CurrentPointParameterType
    {
        LastScheduleChangeInHours=1,
        TargetTemprature = 527,
        CurrentTemprature = 528,
        FillLevel = 404, // How much water in tank
        EnergiStored = 302, // Estimated energi stored in tank
        EstimatedPower = 400, // Estimated electricity usage to heat water now.
        EnergyTotal = 303, // Electricity used in total.
    }

    internal class CurrentValues
    {
        public string? parameterId { get; set; }
        public int rawValue { get; set; }
        public string? kind { get; set; }
        public double value { get; set; }
        public string? unit { get; set; }
        public DateTime timestamp { get; set; }
        public double? minVal { get; set; }
        public double? maxVal { get; set; }
    }
}
