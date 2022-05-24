using MyUplinkSmartConnect.Models;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect
{
    public class ApplyCostSavingRules
    {
        const int TempratureTargetLow = 50;
        const int TempratureTargetHigh = 70;


        public ApplyCostSavingRules()
        {
            
        }

        internal List<WaterHeaterMode> WaterHeaterModes { get; set; } = new List<WaterHeaterMode>();

        internal List<HeaterWeeklyEvent> WaterHeaterSchedule { get; set; } = new List<HeaterWeeklyEvent>();

        public bool VerifyHeaterSchedule(List<stPriceInformation> priceList, params DateTime[] datesToSchuedule)
        {
            // Turns out there is a maximum number of "events" so we have to wipe out all other days.
            WaterHeaterSchedule.Clear();

            var daysInWeek = Enum.GetValues<DayOfWeek>();

            foreach(var day in daysInWeek)
            {
                bool foundDayInTargetSchedules = false;
                HeaterWeeklyEvent? sch = null;

                foreach (var targetSchedule in datesToSchuedule)
                {
                    if(targetSchedule.DayOfWeek != day)
                        continue;

                    int addedPriceEvents = 0;
                    var currentPowerLevel = WaterHeaterDesiredPower.Watt2000;

                    foreach (var price in priceList)
                    {
                        if (price.Start.Date != targetSchedule.Date)
                            continue;

                        if (price.DesiredPower != currentPowerLevel || sch == null)
                        {
                            if (sch != null)
                                WaterHeaterSchedule.Add(sch);

                            sch = new HeaterWeeklyEvent();
                            sch.startDay = targetSchedule.DayOfWeek.ToString();
                            sch.startTime = price.Start.ToString("HH:mm:ss");
                            sch.modeId = GetModeFromWaterHeaterDesiredPower(price.DesiredPower);
                            currentPowerLevel = price.DesiredPower;
                            addedPriceEvents++;
                        }

                    }

                    if(sch != null)
                    {
                        WaterHeaterSchedule.Add(sch);
                        addedPriceEvents++;
                    }
                    
                    if(addedPriceEvents > 0)
                        foundDayInTargetSchedules = true;
                }

                if (!foundDayInTargetSchedules)
                {
                    sch = new HeaterWeeklyEvent();
                    sch.startDay = day.ToString();
                    sch.startTime = "00:00:00";
                    sch.modeId = GetModeFromWaterHeaterDesiredPower( WaterHeaterDesiredPower.Watt2000);
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
                    isGood = VerifyWaterHeaterMode(mode, WaterHeaterDesiredPower.Watt2000, TempratureTargetHigh);
                }

                if (mode.name.StartsWith("M5"))
                {
                    isGood = VerifyWaterHeaterMode(mode, WaterHeaterDesiredPower.Watt700, TempratureTargetLow);
                }

                if (mode.name.StartsWith("M4"))
                {
                    isGood = VerifyWaterHeaterMode(mode, WaterHeaterDesiredPower.None, TempratureTargetLow);
                }

                if(!isGood)
                    allModesGood = false;
            }

            return allModesGood;
        }

        int GetModeFromWaterHeaterDesiredPower(WaterHeaterDesiredPower power)
        {
            for(int i= (WaterHeaterModes.Count-1); i > 0;i--) // Some might call this a micro optimization, those people would be correct. But also since we place our modes in the end, why not start checking there.
            {
                var item = WaterHeaterModes[i];
                if (item.settings == null)
                    throw new NullReferenceException("item.settings cannot be null");

                foreach (var settings in item.settings)
                {
                    if(settings.settingId == WaterheaterSettingsMode.TargetHeaterWatt)
                    {
                        var tmp = settings.HelperDesiredHeatingPower;
                        if (tmp == power)
                            return item.modeId;
                    }
                }
            }

            throw new Exception("Failed to find ");
        }

        static bool VerifyWaterHeaterMode(WaterHeaterMode mode, WaterHeaterDesiredPower desiredPower,int targetTemprature)
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
                            Log.Logger.Warning($"Water heater desired power level is incorrect for {mode.name} , changing from {setting.HelperDesiredHeatingPower} to {desiredPower}");
                        }
                        break;

                    case WaterheaterSettingsMode.TargetTempratureSetpoint:
                        if (setting.value != targetTemprature)
                        {
                            isGood = false;
                            Log.Logger.Warning($"Water heater target temprature is incorrect ({setting.value}) for {mode.name} , changing to {targetTemprature}");

                            setting.value = targetTemprature;
                        }
                        break;
                }
            }

            return isGood;
        }
    }
}
