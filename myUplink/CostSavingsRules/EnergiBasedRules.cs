using MyUplinkSmartConnect.CostSavingsRules;
using MyUplinkSmartConnect.ExternalPrice;
using MyUplinkSmartConnect.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect.CostSavings
{
    internal class EnergiBasedRules : RulesBase, ICostSavingRules
    {
        const double energiLeakPrHour = 1d;
        const int TankVolume = 187; // fixme, tank valume should not be hardcoded.
        double _desiredMinmalTankEnergi;
        double _desiredMaximalTankEnergi;

        readonly List<WaterHeaterState> _tankHeatingSchedule = new List<WaterHeaterState>();
        readonly List<PeakTimeSchedule> _peakTimeSchedule = new List<PeakTimeSchedule>();

        public EnergiBasedRules()
        {
            _desiredMinmalTankEnergi = EnergiInTank(TankVolume, Settings.Instance.MediumPowerTargetTemperature);
            _desiredMaximalTankEnergi = EnergiInTank(TankVolume, Settings.Instance.HighPowerTargetTemperature);
        }

        class WaterHeaterState : ElectricityPriceInformation
        {
            public double ExpectedEnergiLevel { get; set; }

            public HeatingMode HeatingModeBasedOnPrice { get; set; }
        }

        class PeakTimeSchedule
        {
            public int Hour { get; set; }

            public IEnumerable<DayOfWeek> DayOfWeek { get; set; } = Array.Empty<DayOfWeek>();
        }


        public void LogSchedule()
        {
            foreach (var price in _tankHeatingSchedule)
            {
                Log.Logger.Debug($"{price.Start.Day}) Start: {price.Start.ToShortTimeString()} | {price.End.ToShortTimeString()} - {price.HeatingMode}|{price.HeatingModeBasedOnPrice} - {price.Price} - {price.ExpectedEnergiLevel}");
            }
        }

        public void LogToCSV()
        {
            var csv = new StringBuilder();
            csv.AppendLine("Day;Start;End;Target heating;Price based recommendation;Price;Expected energilevel");

            foreach (var price in _tankHeatingSchedule)
            {
                Console.WriteLine($"{price.Start.Day}) Start: {price.Start.ToShortTimeString()} | {price.End.ToShortTimeString()} - {price.HeatingMode}|{price.HeatingModeBasedOnPrice} - {price.Price} - {price.ExpectedEnergiLevel}");
                csv.AppendLine($"{price.Start.Day};{price.Start.ToShortTimeString()};{price.End.ToShortTimeString()};{price.HeatingMode};{price.HeatingModeBasedOnPrice};{price.Price};{price.ExpectedEnergiLevel}");
            }

            try
            {
                File.WriteAllText("c:\\temp\\1.csv", csv.ToString());
            }
            catch
            {

            }            
        }

        public bool GenerateSchedule(string weekFormat, bool runLegionellaHeating, params DateTime[] datesToSchuedule)
        {
            CreateScheduleEmty();
            FindPeakHeatingRequirements(datesToSchuedule);

            var scheduleList = JsonUtils.CloneTo<List<ElectricityPriceInformation>>(_tankHeatingSchedule);
            var status = GenerateRemoteSchedule(weekFormat, runLegionellaHeating, scheduleList, datesToSchuedule);
            return status;
        }

        void FindPeakHeatingRequirements(DateTime[] datesToSchuedule)
        {
            const int MaximumPreHeatInHours = 4;

            ReCalculateTankExpectedEnergiLevels();

            for (int i = (_tankHeatingSchedule.Count - 1); i > 0; i--)
            {
                if (IsDefinedPeakUsageTime(_tankHeatingSchedule[i].Start) && IsDayInsideScheduleWindow(datesToSchuedule, _tankHeatingSchedule[i].Start.DayOfWeek))
                {
                    Log.Logger.Debug("Peak time at {time} on day {day}", _tankHeatingSchedule[i].Start.ToLongTimeString(), _tankHeatingSchedule[i].Start.DayOfWeek);

                    var neededEnergiInTank = _desiredMaximalTankEnergi - _tankHeatingSchedule[i].ExpectedEnergiLevel;
                    int lastChangeIndex = i;
                    //int changesDone = 0;

                    while(lastChangeIndex != 0)
                    {
                        if(_tankHeatingSchedule[lastChangeIndex].HeatingMode !=  HeatingMode.HeathingDisabled)
                        {
                            neededEnergiInTank -= 2.0d;
                            _tankHeatingSchedule[lastChangeIndex].HeatingMode = HeatingMode.HighestTemperature;
                            Log.Logger.Debug("Changing heating at {startheating} to reach target heating at {targetheating}", _tankHeatingSchedule[lastChangeIndex].Start, _tankHeatingSchedule[i].Start);
                            //changesDone++;
                        }

                        if (neededEnergiInTank <= 0)
                            break;

                        var hoursChecked = i - lastChangeIndex;
                        if (hoursChecked > MaximumPreHeatInHours)
                            break;

                        lastChangeIndex--;                        
                    }

                    ReCalculateTankExpectedEnergiLevels();
                }
            }
        }

        void ReCalculateTankExpectedEnergiLevels()
        {
            for (int i = 0; i < _tankHeatingSchedule.Count; i++)
            {
                if (i == 0)
                {
                    _tankHeatingSchedule[i].ExpectedEnergiLevel = _desiredMinmalTankEnergi;
                }
                else
                {
                    _tankHeatingSchedule[i].ExpectedEnergiLevel = CalculateEnergiChangeTank(_tankHeatingSchedule[i - 1]);
                }
            }
        }

        double CalculateEnergiChangeTank(WaterHeaterState last)
        {
            const double EnergiChangePrHour2Kwh = 2;
            
            double newEnergiLevel;
            switch (last.HeatingMode)
            {
                case HeatingMode.HeatingLegionenna:
                case HeatingMode.HighestTemperature:
                    if (last.ExpectedEnergiLevel < _desiredMaximalTankEnergi)
                    {
                        // We added up 1 hour of full powa.
                        newEnergiLevel = last.ExpectedEnergiLevel + EnergiChangePrHour2Kwh;
                    }
                    else
                    {
                        newEnergiLevel = last.ExpectedEnergiLevel; // We likly did not heat the water, but kept the same energilevel
                    }
                    break;

                case HeatingMode.MediumTemprature1300watt:
                case HeatingMode.MediumTemperature:
                    newEnergiLevel = _desiredMinmalTankEnergi; // We assume that we reach the target level.
                    break;

                default:
                case HeatingMode.HeathingDisabled:
                    if (last.ExpectedEnergiLevel > _desiredMinmalTankEnergi)
                        newEnergiLevel = last.ExpectedEnergiLevel - energiLeakPrHour; // Energileak of 1 kw pr hour with high tempratures.
                    else
                        newEnergiLevel = last.ExpectedEnergiLevel;
                    break;
            }

            return newEnergiLevel;
        }

        bool IsDefinedPeakUsageTime(DateTime start)
        {
            if (_peakTimeSchedule.Count == 0)
            {
                ParsePeakTimeSettings();
            }

            foreach (var peak in _peakTimeSchedule)
            {
                if (!peak.DayOfWeek.Contains(start.DayOfWeek))
                    continue;

                if (peak.Hour == start.Hour)
                    return true;
            }

            return false;
        }

        void ParsePeakTimeSettings()
        {
            var csvText = Settings.Instance.EnergiBasedPeakTimes?.Split(',') ?? Array.Empty<string>();

            if (csvText == null || csvText.Length == 0)
            {
                Log.Logger.Warning($"EnergiBasedPeakTimes is not configured, this is required for energibased rules");
                return;
            }

            foreach (var csv in csvText)
            {
                if (string.IsNullOrEmpty(csv))
                    continue;

                //weekday6
                //weekend23
                string? strNumber = null;
                string? strWeekday = null;

                for (int i = (csv.Length - 1); i > 0; i--)
                {
                    if (!char.IsDigit(csv[i]))
                    {
                        strNumber = csv.Substring(i + 1);
                        strWeekday = csv.Substring(0, i + 1).ToLowerInvariant();
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(strNumber) && !string.IsNullOrEmpty(strWeekday))
                {
                    var priority = new PeakTimeSchedule();
                    priority.Hour = Convert.ToInt32(strNumber);

                    if (strWeekday == "weekday")
                    {
                        priority.DayOfWeek = new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
                    }
                    else if (strWeekday == "weekend")
                    {
                        priority.DayOfWeek = new DayOfWeek[] { DayOfWeek.Saturday, DayOfWeek.Sunday };
                    }
                    else
                    {
                        priority.DayOfWeek = ParseWeekday(strWeekday);
                    }

                    _peakTimeSchedule.Add(priority);
                }
            }
        }

        static DayOfWeek[] ParseWeekday(string weekday)
        {
            if(Enum.TryParse<DayOfWeek>(weekday,true,out DayOfWeek result))
            {
                return new DayOfWeek[] { result };
            }

            Log.Logger.Warning($"Attemting to read value from EnergiBasedPeakTimes, {weekday} is not a valid day name or (weekend/weekday)", weekday);
            return Array.Empty<DayOfWeek>();
        }

        void CreateScheduleEmty()
        {
            foreach (var price in CurrentState.PriceList)
            {
                var newPrice = JsonUtils.CloneTo<WaterHeaterState>(price);
                _tankHeatingSchedule.Add(newPrice);
            }

            // First we just use all recommended heating windows, to keep the tank at minimal desired level.
            for (int i = 0; i < _tankHeatingSchedule.Count; i++)
            {
                _tankHeatingSchedule[i].HeatingModeBasedOnPrice = _tankHeatingSchedule[i].HeatingMode;

                if(_tankHeatingSchedule[i].HeatingMode !=  HeatingMode.HeathingDisabled)
                {
                    _tankHeatingSchedule[i].HeatingMode = HeatingMode.MediumTemperature;
                }
            }
        }

        static double RequiredKillotWattForChange(int tankVolume,int startTemprature,int targetTemptrature)
        {
            //https://www.electrical4u.net/energy-calculation/water-heater-calculator-time-required-to-heat-water/
            return 4.2 * tankVolume  * (targetTemptrature - startTemprature) / 3600;
        }

        static double EnergiInTank(int tankVolume, int temprature)
        {
            //https://www.electrical4u.net/energy-calculation/water-heater-calculator-time-required-to-heat-water/
            return 4.2 * tankVolume * temprature / 3600;
        }
    }
}