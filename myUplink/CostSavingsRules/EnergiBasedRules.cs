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
        const double DesiredMinmalEnergi = 10.0d;
        const double DesiredMaximalEnergi = 14.0d;

        List<WaterHeaterState> _tankHeatingSchedule = new List<WaterHeaterState>();
        List<PeakTimeSchedule> _peakTimeSchedule = new List<PeakTimeSchedule>();

        class WaterHeaterState : ElectricityPriceInformation
        {
            public double ExpectedEnergiLevel { get; set; }

            public WaterHeaterDesiredPower TargetHeatingPower { get; set; }
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
                Log.Logger.Debug($"{price.Start.Day}) Start: {price.Start.ToShortTimeString()} | {price.End.ToShortTimeString()} - {price.TargetHeatingPower}|{price.RecommendedHeatingPower} - {price.Price} - {price.ExpectedEnergiLevel}");
#if DEBUG
                Console.WriteLine($"{price.Start.Day}) Start: {price.Start.ToShortTimeString()} | {price.End.ToShortTimeString()} - {price.TargetHeatingPower}|{price.RecommendedHeatingPower} - {price.Price} - {price.ExpectedEnergiLevel}");
#endif
            }
        }

        public bool VerifyHeaterSchedule(string weekFormat, params DateTime[] datesToSchuedule)
        {
            WaterHeaterSchedule.Clear();
            CreateSchedule();
            ReCalculateHeatingTimes();

            var daysInWeek = GetWeekDayOrder(weekFormat);
            var requiredHours = datesToSchuedule.Length * 24;

            if (CurrentState.PriceList.Count < requiredHours)
            {
                Log.Logger.Warning("Cannot build waterheater schedule, the price list only contains {priceListCount}, but we are attempting to schedule {RequiredHours}", CurrentState.PriceList.Count, requiredHours);
                return false;
            }

            foreach (var day in daysInWeek)
            {
                bool foundDayInTargetSchedules = false;
                HeaterWeeklyEvent? sch = null;

                foreach (var targetSchedule in datesToSchuedule)
                {
                    if (targetSchedule.DayOfWeek != day)
                        continue;

                    int addedPriceEvents = 0;
                    var currentPowerLevel = WaterHeaterDesiredPower.Watt2000;

                    foreach (var price in _tankHeatingSchedule)
                    {
                        if (price.Start.Date != targetSchedule.Date)
                            continue;

                        if (price.TargetHeatingPower != currentPowerLevel || sch == null)
                        {
                            if (sch != null)
                                WaterHeaterSchedule.Add(sch);

                            sch = new HeaterWeeklyEvent();
                            sch.startDay = targetSchedule.DayOfWeek.ToString();
                            sch.startTime = price.Start.ToString("HH:mm:ss");
                            sch.modeId = GetModeFromWaterHeaterDesiredPower(price.TargetHeatingPower);
                            currentPowerLevel = price.TargetHeatingPower;
                            addedPriceEvents++;
                        }
                    }

                    if (sch != null)
                    {
                        WaterHeaterSchedule.Add(sch);
                        addedPriceEvents++;
                    }

                    if (addedPriceEvents > 0)
                        foundDayInTargetSchedules = true;
                }

                if (!foundDayInTargetSchedules)
                {
                    sch = new HeaterWeeklyEvent();
                    sch.startDay = day.ToString();
                    sch.startTime = "00:00:00";
                    sch.modeId = GetModeFromWaterHeaterDesiredPower(WaterHeaterDesiredPower.Watt2000);
                    WaterHeaterSchedule.Add(sch);


                    sch = new HeaterWeeklyEvent();
                    sch.startDay = day.ToString();
                    sch.startTime = "06:00:00";
                    sch.modeId = GetModeFromWaterHeaterDesiredPower(WaterHeaterDesiredPower.None);
                    WaterHeaterSchedule.Add(sch);


                    sch = new HeaterWeeklyEvent();
                    sch.startDay = day.ToString();
                    sch.startTime = "12:00:00";
                    sch.modeId = GetModeFromWaterHeaterDesiredPower(WaterHeaterDesiredPower.Watt700);
                    WaterHeaterSchedule.Add(sch);
                }
            }

            return false;
        }

        void ReCalculateHeatingTimes()
        {
            const int MaximumPreHeatInHours = 4;

            ReCalculateTankExpectedEnergiLevels();

            for (int i = (_tankHeatingSchedule.Count - 1); i > 0; i--)
            {
                if (IsPeakWaterUsage(_tankHeatingSchedule[i].Start))
                {
                    var neededEnergiInTank = DesiredMaximalEnergi - _tankHeatingSchedule[i].ExpectedEnergiLevel;
                    int lastChangeIndex = i;

                    int changesDone = 0;

                    while(lastChangeIndex != 0)
                    {
                        if(_tankHeatingSchedule[lastChangeIndex].RecommendedHeatingPower != WaterHeaterDesiredPower.None)
                        {
                            neededEnergiInTank -= 2.0d;
                            _tankHeatingSchedule[lastChangeIndex].TargetHeatingPower = WaterHeaterDesiredPower.Watt2000;
                            changesDone++;
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
                    _tankHeatingSchedule[i].ExpectedEnergiLevel = DesiredMinmalEnergi;
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
            const double EnergiChangePrHour07Kwh = 0.7;
            double newEnergiLevel;
            switch (last.TargetHeatingPower)
            {
                case WaterHeaterDesiredPower.Watt2000:
                    if (last.ExpectedEnergiLevel < DesiredMaximalEnergi)
                    {
                        // We added up 1 hour of full powa.
                        newEnergiLevel = last.ExpectedEnergiLevel + EnergiChangePrHour2Kwh;
                    }
                    else
                    {
                        newEnergiLevel = last.ExpectedEnergiLevel; // We likly did not heat the water, but kept the same energilevel
                    }
                    break;

                case WaterHeaterDesiredPower.Watt700:
                    if (last.ExpectedEnergiLevel < DesiredMinmalEnergi)
                    {
                        // We added up 1 hour of full powa.
                        newEnergiLevel = last.ExpectedEnergiLevel + EnergiChangePrHour07Kwh;
                    }
                    else
                    {
                        newEnergiLevel = last.ExpectedEnergiLevel; // We likly did not heat the water, but kept the same energilevel
                    }
                    break;

                default:
                case WaterHeaterDesiredPower.None:
                    if (last.ExpectedEnergiLevel > DesiredMinmalEnergi)
                        newEnergiLevel = last.ExpectedEnergiLevel - 1; // Energileak of 1 kw pr hour with high tempratures.
                    else
                        newEnergiLevel = last.ExpectedEnergiLevel;
                    break;
            }

            return newEnergiLevel;
        }

        bool IsPeakWaterUsage(DateTime start)
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
                        priority.DayOfWeek = new DayOfWeek[] { Enum.Parse<DayOfWeek>(strWeekday) };
                    }

                    _peakTimeSchedule.Add(priority);
                }
            }
        }

        void CreateSchedule()
        {
            foreach (var price in CurrentState.PriceList)
            {
                var newPrice = JsonUtils.CloneTo<WaterHeaterState>(price);
                _tankHeatingSchedule.Add(newPrice);
            }

            // Firt we just use all recommended heating windows, to keep the tank at minimal desired level.
            for (int i = 0; i < _tankHeatingSchedule.Count; i++)
            {
                if (_tankHeatingSchedule[i].RecommendedHeatingPower == WaterHeaterDesiredPower.Watt2000 || _tankHeatingSchedule[i].RecommendedHeatingPower == WaterHeaterDesiredPower.Watt700 || _tankHeatingSchedule[i].RecommendedHeatingPower == WaterHeaterDesiredPower.Watt1300)
                {
                    _tankHeatingSchedule[i].TargetHeatingPower = WaterHeaterDesiredPower.Watt700;
                }
            }
        }
    }
}