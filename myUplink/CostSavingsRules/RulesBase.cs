using Microsoft.Extensions.DependencyInjection;
using MyUplinkSmartConnect.ExternalPrice;
using MyUplinkSmartConnect.Models;
using MyUplinkSmartConnect.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using xElectricityPriceApiShared;

namespace MyUplinkSmartConnect.CostSavingsRules
{
    abstract internal class RulesBase
    {
        IList<DayOfWeek> _daysInWeek = Array.Empty<DayOfWeek>();
        ScheduleAdjustService _scheduleAdjustService { get; set; }
        internal CurrentStateService _currentState;

        public RulesBase()
        {
            _scheduleAdjustService = Settings.ServiceLookup.GetService<ScheduleAdjustService>() ?? throw new NullReferenceException();
            _currentState = Settings.ServiceLookup.GetService<CurrentStateService>() ?? throw new NullReferenceException(); 
        }

        public List<HeaterWeeklyEvent> WaterHeaterSchedule { get; set; } = new List<HeaterWeeklyEvent>();

        internal bool GenerateRemoteSchedule(string weekFormat,bool runLegionellaProgram,IReadOnlyList<ElectricityPriceInformation> schedule, params DateTime[] datesToSchuedule)
        {
            WaterHeaterSchedule.Clear();
            _daysInWeek = GetWeekDayOrder(weekFormat).ToList();

            var requiredHours = datesToSchuedule.Length * 24;

            if (_currentState.PriceList.Count < requiredHours)
            {
                Log.Logger.Warning("Cannot build waterheater schedule, the price list only contains {priceListCount}, but we are attempting to schedule {RequiredHours}", _currentState.PriceList.Count, requiredHours);
                return false;
            }

            foreach (var day in _daysInWeek)
            {
                bool foundDayInTargetSchedules = false;                

                if (IsDayInsideScheduleWindow(datesToSchuedule, day))
                {
                    int addedPriceEvents = 0;
                    var currentPowerLevel = HeatingMode.Unkown;

                    foreach (var price in schedule)
                    {
                        if (price.Start.DayOfWeek != day)
                            continue;

                        if(IsInsideBoostWindow(price.Start))
                        {
                            currentPowerLevel =  HeatingMode.HeatingLegionenna;
                            WaterHeaterSchedule.Add(new HeaterWeeklyEvent(price.Start, _currentState.ModeLookup.GetHeatingModeId(currentPowerLevel), hasPriceInformation: true));
                            addedPriceEvents++;
                        }
                        else if (price.HeatingMode != currentPowerLevel)
                        {
                            currentPowerLevel = price.HeatingMode;
                            WaterHeaterSchedule.Add(new HeaterWeeklyEvent(price.Start, _currentState.ModeLookup.GetHeatingModeId(price.HeatingMode), hasPriceInformation: true));
                            addedPriceEvents++;
                        }
                    }

                    if (addedPriceEvents > 0)
                        foundDayInTargetSchedules = true;
                }
                
                if (!foundDayInTargetSchedules)
                {                   
                    WaterHeaterSchedule.Add(new HeaterWeeklyEvent(GetDateOfDay(day), _currentState.ModeLookup.GetHeatingModeId(HeatingMode.HighestTemperature), hasPriceInformation:false));
                    WaterHeaterSchedule.Add(new HeaterWeeklyEvent(GetDateOfDay(day).AddHours(6), _currentState.ModeLookup.GetHeatingModeId(HeatingMode.HeathingDisabled), hasPriceInformation: false));
                    WaterHeaterSchedule.Add(new HeaterWeeklyEvent(GetDateOfDay(day).AddHours(12), _currentState.ModeLookup.GetHeatingModeId(HeatingMode.MediumTemperature), hasPriceInformation: false));
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

        internal bool IsDayInsideScheduleWindow(ReadOnlySpan<DateTime> datesToSchuedule, DayOfWeek day)
        {
            foreach (var targetSchedule in datesToSchuedule)
            {
                if (targetSchedule.DayOfWeek == day)
                    return true;
            }

            return false;
        }

        internal bool IsInsideBoostWindow(DateTime time)
        {            
            return _scheduleAdjustService?.IsBoostScheduled(time) ?? false;   
        }

        internal DateTime GetDateOfDay(DayOfWeek day)
        {
            // Gets the DateTime from the day. This is done by calculating it from week order and todays index in that list.

            var now = DateTime.Now.Date;

            int indexOfToday = -1;
            for (int i = 0; i < _daysInWeek.Count(); i++)
            {
                if (_daysInWeek[i] == now.DayOfWeek)
                {
                    indexOfToday = i;
                    break;
                }
            }


            for (int i = 0; i < _daysInWeek.Count();i++)
            {
                if (_daysInWeek[i] == day)
                {
                    int realativeDay = i - indexOfToday;

                    var relativeDate = now.AddDays(realativeDay);
                    return relativeDate;
                }
            }
            return DateTime.MinValue;
        }

        bool CreateLegionellaHeating(params DateTime[] datesToSchuedule)
        {
            Log.Logger.Debug("Will attemt to find best posible moment to heat water above 75c, to prevent legionella");
            var timeSlotList = new List<TimeSlot>();


            const int requiredContinuousHours = 3;
            for (int i = 0; i < WaterHeaterSchedule.Count; i++)
            {
                if (WaterHeaterSchedule[i].modeId != _currentState.ModeLookup.GetHeatingModeId(HeatingMode.HighestTemperature))
                    continue;

                if (!IsDayInsideScheduleWindow(datesToSchuedule, WaterHeaterSchedule[i].Day))
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
                    if (WaterHeaterSchedule[i].modeId != _currentState.ModeLookup.GetHeatingModeId(HeatingMode.MediumTemperature))
                        continue;

                    if (!IsDayInsideScheduleWindow(datesToSchuedule, WaterHeaterSchedule[i].Day))
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
                WaterHeaterSchedule[sortedTimeSlotList[0].TimeSlotIndex].modeId = _currentState.ModeLookup.GetHeatingModeId(HeatingMode.HeatingLegionenna);

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
            if (_currentState.PriceList != null)
            {
                foreach (var priceItem in _currentState.PriceList)
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
    }
}
