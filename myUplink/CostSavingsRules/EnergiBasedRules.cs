using MyUplinkSmartConnect.CostSavingsRules;
using MyUplinkSmartConnect.ExternalPrice;
using MyUplinkSmartConnect.Models;
using Serilog;
using System;
using System.Collections.Generic;
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

        class WaterHeaterState : ElectricityPriceInformation
        {
            public double ExpectedEnergiLevel { get; set; }

            public WaterHeaterDesiredPower TargetHeatingPower { get; set; }
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

                        if (price.RecommendedHeatingPower != currentPowerLevel || sch == null)
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
            var maxiumPreHeat = 4; //todo calculate how early we can start heating before a target time. We leak 1kw pr hour. So that likely means we cannot preheat more then 4 hours

            ReCalculateTankExpectedEnergiLevels();

            for (int i = (_tankHeatingSchedule.Count - 1); i > 0; i--)
            {
                if (IsPeakWaterUsage(_tankHeatingSchedule[i].Start))
                {
                    var neededPowerChange = DesiredMaximalEnergi - _tankHeatingSchedule[i].ExpectedEnergiLevel;
                    int lastChangeIndex = i;

                    // This can likely be improved.
                    // Method 1) using any timeslot that has "cheap" electricity to build up energi.

                    while (neededPowerChange >= 1)
                    {
                        if (_tankHeatingSchedule[lastChangeIndex].RecommendedHeatingPower == WaterHeaterDesiredPower.None)
                        {
                            neededPowerChange += 1; // We are leaking heat at this point
                        }
                        else //if (_tankHeatingSchedule[i].RecommendedHeatingPower == WaterHeaterDesiredPower.Watt700 || _tankHeatingSchedule[i].RecommendedHeatingPower == WaterHeaterDesiredPower.Watt1300)
                        {
                            neededPowerChange -= 2;
                            _tankHeatingSchedule[lastChangeIndex].TargetHeatingPower = WaterHeaterDesiredPower.Watt2000;
                        }
                        
                        if (neededPowerChange > maxiumPreHeat) // Its not posible to change the scheudule
                            break;

                        if (neededPowerChange < 1) // We have found the needed changes to schedule.
                            break;

                        if (lastChangeIndex == 0) // We cannot go back in time.
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

        bool IsPeakWaterUsage(DateTime start)
        {
            if (start.Hour == 6 || start.Hour == 21)
                return true;

            return false;
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
