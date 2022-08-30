using MyUplinkSmartConnect.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect.CostSavingsRules
{
    interface ICostSavingRules
    {
        public bool VerifyWaterHeaterModes();        

        List<WaterHeaterMode> WaterHeaterModes { get; set; } 

        List<HeaterWeeklyEvent> WaterHeaterSchedule { get; set; } 

        bool GenerateSchedule(string weekFormat, params DateTime[] datesToSchuedule);

        void LogSchedule();

        void LogToCSV();
    }
}
