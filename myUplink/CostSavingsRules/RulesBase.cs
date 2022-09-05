using MyUplinkSmartConnect.ExternalPrice;
using MyUplinkSmartConnect.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
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
                            sch.Date = targetSchedule;
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
            
            //CreateLegionellaHeating(datesToSchuedule);
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

            var heatingModeLegionnella = -1;
            var heatingModeHigh = -1;
            var heatingModeMedium = -1;

            var timeSlotList = new List<TimeSlot>();

            foreach (var mode in WaterHeaterModes)
            {
                if (string.IsNullOrEmpty(mode.name))
                    continue;

                if (Settings.Instance.RequireUseOfM2ForLegionellaProgram && mode.name.StartsWith("M2"))
                {
                    heatingModeLegionnella = mode.modeId;
                    continue;
                }
                else if (!Settings.Instance.RequireUseOfM2ForLegionellaProgram && mode.name.StartsWith("M6"))
                {
                    heatingModeLegionnella = mode.modeId;
                    heatingModeHigh = mode.modeId;
                    continue;
                }
                else if (mode.name.StartsWith("M6"))
                {
                    heatingModeHigh= mode.modeId;
                    continue;
                }
                else if (Settings.Instance.EnergiBasedCostSaving && mode.name.StartsWith("M3") || !Settings.Instance.EnergiBasedCostSaving && mode.name.StartsWith("M5"))
                {
                    heatingModeMedium = mode.modeId;
                    continue;
                }
            }

            const int requiredContinuousHours = 3;
            for (int i = 0; i < WaterHeaterSchedule.Count; i++)
            {
                if (WaterHeaterSchedule[i].modeId != heatingModeHigh)
                    continue;

                if (!ContainsDay(WaterHeaterSchedule[i].Day, datesToSchuedule))
                    continue;

                // First we check there is already scheduled a heating that will cover legionella heating
                // If needed we change the heating so it above 75c.

                var timeSlot = GetScheduleTimes(i);
                if (timeSlot.Duration.TotalHours == 0)
                    continue; // Most likely this is the past, so it cannto be used.

                if (timeSlot.Duration.TotalHours >= requiredContinuousHours)
                {
                    timeSlot.Price = 0; // This is perfect, we already need this heating, using it will add 0 ekstra cost.

                    timeSlotList.Add(timeSlot);
                    Log.Logger.Information("There is already a heating schedules that should heat the water to 75c, this should prevent legionella and reset the timer");
                }
                else
                {
                    // Now we check the price if we extend the heating window.
                    var extendedInHours = (requiredContinuousHours - Convert.ToInt32(timeSlot.Duration.TotalHours));
                    timeSlot.ExtendedTimeSlot = extendedInHours * -1;

                    var result = GetDateTimeFromHourString(WaterHeaterSchedule[i].startTime, WaterHeaterSchedule[i + 1].startTime, WaterHeaterSchedule[i].Date);
                    result.starTime = result.starTime.AddHours(timeSlot.ExtendedTimeSlot);
                    timeSlot.Price = CalculatePriceTotal(result.starTime, result.endTime);
                    timeSlot.Price = (timeSlot.Price / requiredContinuousHours) * extendedInHours;

                    timeSlotList.Add(timeSlot);

                    Log.Logger.Information("There is already a heating schedules that should heat the water to 75c, with extending this by {hours} ", extendedInHours);
                }                
            }

            if (true)
            {
                // We did not find a good window to heat up, so we find a window based purly on price. This is the worse case.
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
                WaterHeaterSchedule[sortedTimeSlotList[0].TimeSlotIndex].modeId = heatingModeLegionnella;

                if(sortedTimeSlotList[0].ExtendedTimeSlot != 0)
                {
                    var time = GetDateTimeFromHourString(WaterHeaterSchedule[sortedTimeSlotList[0].TimeSlotIndex].startTime, WaterHeaterSchedule[sortedTimeSlotList[0].TimeSlotIndex + 1].startTime, WaterHeaterSchedule[sortedTimeSlotList[0].TimeSlotIndex].Date);
                    time.starTime = time.starTime.AddHours(sortedTimeSlotList[0].ExtendedTimeSlot);

                    WaterHeaterSchedule[sortedTimeSlotList[0].TimeSlotIndex].startTime = time.starTime.ToString("HH:mm:ss");
                }
            }

            Log.Logger.Debug("Failed to find schedule to change for legionella program to run, will allow water heater to do it on its own.");
            return false;
        }

        TimeSlot GetScheduleTimes(int index)
        {
            var timeslot = new TimeSlot();
            (DateTime starTime, DateTime endTime) result = GetDateTimeFromHourString(WaterHeaterSchedule[index].startTime, WaterHeaterSchedule[index + 1].startTime, WaterHeaterSchedule[index].Date);

            timeslot.Duration = result.endTime - result.starTime;
            timeslot.TimeSlotIndex = index;

            if (result.starTime <= DateTime.Now || timeslot.Duration.TotalHours < 0 || timeslot.Duration.TotalHours > 6)
            {
                // We cannot use a timeslot thats in the past. Or it has some other weird value....
                // the reason we get weird values, is because we dont have dates for days outside the 2 days we are actualy scheduling.
                timeslot.Duration = new TimeSpan();
                return timeslot;
            }                
   
            timeslot.Price = CalculatePriceTotal(result.starTime, result.endTime);
            return timeslot;
        }

        (DateTime starTime,DateTime endTime) GetDateTimeFromHourString(string? strStart,string? strEnd,DateTime date)
        {
            string strDate;
            if (date == DateTime.MinValue)
            {
                if(strEnd == "00:00")
                    strDate = DateTime.Now.AddDays(1).ToLongDateString();
                else
                    strDate = DateTime.Now.ToLongDateString();
            }                
            else
                strDate = date.ToLongDateString();

            var startTime = DateTime.Parse($"{strDate} {strStart}");
            var endTime = DateTime.Parse($"{strDate} {strEnd}");

            return (startTime, endTime);
        }

        double CalculatePriceTotal(DateTime startTime,DateTime endTime)
        {
            double price = 0;
            if (CurrentState.PriceList != null)
            {
                foreach (var priceItem in CurrentState.PriceList)
                {
                    if (priceItem.Start.InRange(startTime, endTime))
                    {
                        price += priceItem.Price;
                    }
                }
            }
            return price;
        }

        class TimeSlot
        {
            public TimeSpan Duration { get; set; }

            public double Price { get; set; }

            public int ExtendedTimeSlot { get; set; } = 0;// negative value for how far the starttime was extended

            public int TimeSlotIndex { get; set; } = -1;
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
