using MyUplinkSmartConnect.CostSavingsRules;
using MyUplinkSmartConnect.Models;
using MyUplinkSmartConnect.Services;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect.CostSavings
{
    class SimplePriceBasedRules : RulesBase, ICostSavingRules
    {
        public SimplePriceBasedRules()
        {

        }

        public void LogSchedule()
        {
            foreach (var price in _currentState.PriceList)
            {
                Log.Logger.Debug($"{price.Start.Day}) Start: {price.Start.ToShortTimeString()} | {price.End.ToShortTimeString()} - {price.HeatingMode} - {price.Price}");
            }
        }

        public string GetJson()
        {
            return GetGenericJsonSchedule();
        }

        public void LogToCSV()
        {
            var csv = new StringBuilder();
            csv.AppendLine("Day;Start;End;Target heating;Price based recommendation;Price;Expected energilevel");

            foreach (var price in _currentState.PriceList)
            {
                Console.WriteLine($"{price.Start.Day}) Start: {price.Start.ToShortTimeString()} | {price.End.ToShortTimeString()} - {price.HeatingMode} - {price.Price}");
                csv.AppendLine($"{price.Start.Day};{price.Start.ToShortTimeString()};{price.End.ToShortTimeString()};{price.HeatingMode};{price.Price}");
            }

            File.WriteAllText("c:\\temp\\1.csv", csv.ToString());
        }

        public bool GenerateSchedule( string weekFormat, bool runLegionellaHeating, params DateTime[] datesToSchuedule)
        {
            // Turns out there is a maximum number of "events" so we have to wipe out all other days.
            WaterHeaterSchedule.Clear();

            return GenerateRemoteSchedule(weekFormat,runLegionellaHeating, _currentState.PriceList, datesToSchuedule); ;
        }  
    }
}
