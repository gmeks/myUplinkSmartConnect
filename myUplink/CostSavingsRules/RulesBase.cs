using MyUplinkSmartConnect.ExternalPrice;
using MyUplinkSmartConnect.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect.CostSavingsRules
{
    internal class RulesBase
    {
        public bool VerifyWaterHeaterModes()
        {
            bool allModesGood = true;

            foreach (var mode in WaterHeaterModes)
            {
                if (string.IsNullOrEmpty(mode.name))
                    throw new NullReferenceException("mode.name cannot be null");

                bool isGood = true;
                if (mode.name.StartsWith("M6"))
                {
                    isGood = VerifyWaterHeaterMode(mode, WaterHeaterDesiredPower.Watt2000, Settings.Instance.HighPowerTargetTemperature);
                }

                if (mode.name.StartsWith("M5"))
                {
                    isGood = VerifyWaterHeaterMode(mode, WaterHeaterDesiredPower.Watt700, Settings.Instance.MediumPowerTargetTemperature);
                }

                if (mode.name.StartsWith("M4"))
                {
                    isGood = VerifyWaterHeaterMode(mode, WaterHeaterDesiredPower.None, Settings.Instance.MediumPowerTargetTemperature);
                }

                if (Settings.Instance.EnergiBasedCostSaving && mode.name.StartsWith("M3"))
                {
                    isGood = VerifyWaterHeaterMode(mode, WaterHeaterDesiredPower.Watt1300, Settings.Instance.MediumPowerTargetTemperature);
                }

                if (Settings.Instance.RequireUseOfM2ForLegionellaProgram && mode.name.StartsWith("M2"))
                {
                    isGood = VerifyWaterHeaterMode(mode, WaterHeaterDesiredPower.Watt2000, 75);
                }

                if (!isGood)
                    allModesGood = false;
            }

            return allModesGood;
        }

        public List<WaterHeaterMode> WaterHeaterModes { get; set; } = new List<WaterHeaterMode>();

        public List<HeaterWeeklyEvent> WaterHeaterSchedule { get; set; } = new List<HeaterWeeklyEvent>();

        internal bool GenerateRemoteSchedule(string weekFormat,bool runLegionellaProgram,IReadOnlyList<ElectricityPriceInformation> schedule, params DateTime[] datesToSchuedule)
        {
            WaterHeaterSchedule.Clear();

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

                    foreach (var price in schedule)
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

#if !DEBUG
            if(runLegionellaProgram)
            {
                CreateLegionellaHeating(datesToSchuedule);
            }
#else
            if (runLegionellaProgram)
            {
                CreateLegionellaHeating(datesToSchuedule);
            }
#endif
            return true;
        }


        internal IEnumerable<DayOfWeek> GetWeekDayOrder(string input)
        {
            var weekOrder = new List<DayOfWeek>();
            var splitDays = input.Split(',');

            foreach (var strDay in splitDays)
            {
                switch (strDay)
                {
                    case "mon":
                        weekOrder.Add(DayOfWeek.Monday);
                        break;

                    case "tue":
                        weekOrder.Add(DayOfWeek.Tuesday);
                        break;

                    case "wed":
                        weekOrder.Add(DayOfWeek.Wednesday);
                        break;

                    case "thu":
                        weekOrder.Add(DayOfWeek.Thursday);
                        break;

                    case "fri":
                        weekOrder.Add(DayOfWeek.Friday);
                        break;

                    case "sat":
                        weekOrder.Add(DayOfWeek.Saturday);
                        break;

                    case "sun":
                        weekOrder.Add(DayOfWeek.Sunday);
                        break;
                }
            }
            return weekOrder;
        }

        internal int GetModeFromWaterHeaterDesiredPower(WaterHeaterDesiredPower power)
        {
            for (int i = WaterHeaterModes.Count - 1; i > 0; i--) // Some might call this a micro optimization, those people would be correct. But also since we place our modes in the end, why not start checking there.
            {
                var item = WaterHeaterModes[i];
                if (item.settings == null)
                    throw new NullReferenceException("item.settings cannot be null");

                foreach (var settings in item.settings)
                {
                    if (settings.settingId == WaterheaterSettingsMode.TargetHeaterWatt)
                    {
                        var tmp = settings.HelperDesiredHeatingPower;
                        if (tmp == power)
                            return item.modeId;
                    }
                }
            }

            throw new Exception("Failed to find ");
        }

        bool CreateLegionellaHeating(params DateTime[] datesToSchuedule)
        {
            Log.Logger.Debug("Will attemt to find best posible moment to heat water above 75c, to prevent legionella");
            var requiredHeatingMode = -1;
            var heatingModeHigh = -1;
            var heatingModeMedium = -1;

            var timeSlotList = new List<TimeSlot>();

            foreach (var mode in WaterHeaterModes)
            {
                if (string.IsNullOrEmpty(mode.name))
                    continue;

                if (Settings.Instance.RequireUseOfM2ForLegionellaProgram && mode.name.StartsWith("M2"))
                {
                    requiredHeatingMode = mode.modeId;
                    continue;
                }
                if (!Settings.Instance.RequireUseOfM2ForLegionellaProgram && mode.name.StartsWith("M6"))
                {
                    requiredHeatingMode = mode.modeId;
                    heatingModeHigh = mode.modeId;
                    continue;
                }
                if (Settings.Instance.EnergiBasedCostSaving && mode.name.StartsWith("M3") || !Settings.Instance.EnergiBasedCostSaving && mode.name.StartsWith("M5"))
                {
                    heatingModeMedium = mode.modeId;
                    continue;
                }
            }

            const int requiredContinuousHours = 3;
            if (!Settings.Instance.RequireUseOfM2ForLegionellaProgram)
            {
                for (int i = 0; i < WaterHeaterSchedule.Count; i++)   // First we check there is already scheduled a heating that will cover legionella heating.
                {
                    if (WaterHeaterSchedule[i].modeId != requiredHeatingMode)
                        continue;

                    if (!ContainsDay(WaterHeaterSchedule[i].Day, datesToSchuedule))
                        continue;

                    var timeSlot = GetScheduleTimes(i);
                    if (timeSlot.Duration.TotalHours >= requiredContinuousHours)
                    {
                        timeSlotList.Add(timeSlot);
                        Log.Logger.Information("There is already a heating schedules that should heat the water to 75c, this should prevent legionella and reset the timer");
                    }
                }
            }
            else
            {
                // We check if its posible to change M6 program to M2

                for (int i = 0; i < WaterHeaterSchedule.Count; i++) 
                {
                    if (WaterHeaterSchedule[i].modeId != heatingModeHigh)
                        continue;

                    if (!ContainsDay(WaterHeaterSchedule[i].Day, datesToSchuedule))
                        continue;

                    var timeSlot = GetScheduleTimes(i);
                    if (timeSlot.Duration.TotalHours >= requiredContinuousHours)
                    {
                        Log.Logger.Information("There is already a heating schedules thats 3 hour long, we change to to heat water to 75c, this should prevent legionella and reset the timer");
                        timeSlotList.Add(timeSlot);
                    }
                }
            }


            if(timeSlotList.Count == 0) // We did not find a good window to heat up, so we find a window based purly on price.
            {
                for (int i = 1; i < (WaterHeaterSchedule.Count - 1); i++)
                {
                    if (WaterHeaterSchedule[i].modeId != heatingModeMedium)
                        continue;

                    if (!ContainsDay(WaterHeaterSchedule[i].Day, datesToSchuedule))
                        continue;

                    var timeSlot = GetScheduleTimes(i);
                    if (timeSlot.Duration.TotalHours >= requiredContinuousHours)
                    {
                        Log.Logger.Information("There is already a heating schedules thats 3 hour long, we change to to heat water to 75c, this should prevent legionella and reset the timer");
                        timeSlotList.Add(timeSlot); 
                    }
                }
            }

            var sortedTimeSlotList = timeSlotList.OrderBy(x => x.Price).ToList();
            if(sortedTimeSlotList.Count != 0)
            {
                WaterHeaterSchedule[sortedTimeSlotList[0].TimeSlotIndex].modeId = requiredHeatingMode;
            }

            Log.Logger.Debug("Failed to find schedule to change for legionella program to run, will allow water heater to do it on its own.");
            return false;
        }

        TimeSlot GetScheduleTimes(int index)
        {
            var timeslot = new TimeSlot();
            string strDate = DateTime.Now.ToLongDateString();

            var startTime = DateTime.Parse($"{strDate} {WaterHeaterSchedule[index].startTime}");
            DateTime endTime;

            if ((index + 1) < WaterHeaterSchedule.Count)
                endTime = DateTime.Parse($"{strDate} {WaterHeaterSchedule[index + 1].startTime}");
            else
                endTime = DateTime.Parse($"{strDate} 23:59:00"); // There is no timeslot, so we assume it goes to end of the day.

            timeslot.Duration = endTime - startTime;
            timeslot.TimeSlotIndex = index;

            if (CurrentState.PriceList != null)
            {
                foreach(var priceItem in CurrentState.PriceList)
                {
                    if(priceItem.Start.InRange(startTime,endTime))
                    {
                        timeslot.Price += priceItem.Price;
                    }
                }
            }

            return timeslot;
        }

        class TimeSlot
        {
            public TimeSpan Duration { get; set; }

            public double Price { get; set; }

            public int TimeSlotIndex = -1;
        }

        static bool ContainsDay(DayOfWeek day, DateTime[] datesToSchuedule)
        {
            foreach(var date in datesToSchuedule)
            {
                if (date.DayOfWeek == day)
                    return true;
            }

            return false;
        }

        internal static bool VerifyWaterHeaterMode(WaterHeaterMode mode, WaterHeaterDesiredPower desiredPower, int targetTemprature)
        {
            if (mode.settings == null)
                throw new NullReferenceException("mode.settings cannot be null");

            bool isGood = true;
            foreach (var setting in mode.settings)
            {
                switch (setting.settingId)
                {
                    case WaterheaterSettingsMode.TargetHeaterWatt:
                        if (setting.HelperDesiredHeatingPower != desiredPower)
                        {
                            isGood = false;
                            setting.value = (int)desiredPower;
                            Log.Logger.Warning("Water heater desired power level is incorrect for {modename} , changing from {settingHelperDesiredHeatingPower} to {desiredPower}", mode.name, setting.HelperDesiredHeatingPower, desiredPower);
                        }
                        break;

                    case WaterheaterSettingsMode.TargetTempratureSetpoint:
                        if (setting.value != targetTemprature)
                        {
                            isGood = false;
                            Log.Logger.Warning("Water heater target temperature is incorrect ({settingValue}) for {modename} , changing to {targetTemprature}", setting.value, mode.name, targetTemprature);

                            setting.value = targetTemprature;
                        }
                        break;
                }
            }

            return isGood;
        }
    }
}
