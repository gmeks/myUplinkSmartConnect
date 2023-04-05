using MyUplinkSmartConnect.ExternalPrice;
using MyUplinkSmartConnect.Models;
using MyUplinkSmartConnect.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect.CostSavingsRules
{
    interface ICostSavingRules
    {
        List<HeaterWeeklyEvent> WaterHeaterSchedule { get; set; } 

        bool GenerateSchedule(string weekFormat,bool runLegionellaHeating, params DateTime[] datesToSchuedule);

        void LogSchedule();

        void LogToCSV();

        string GetJson();
    }
}
