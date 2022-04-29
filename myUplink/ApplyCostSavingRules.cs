using myUplink.ModelsPublic.Internal;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myUplink
{
    public class ApplyCostSavingRules
    {

        public ApplyCostSavingRules()
        {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Debug).CreateLogger();
        }

        internal List<WaterHeaterMode> WaterHeaterModes { get; set; } = new List<WaterHeaterMode>();

        internal List<HeaterWeeklyEvent> WaterHeaterSchedule { get; set; } = new List<HeaterWeeklyEvent>();

        public bool VerifyHeaterSchedule(List<stPriceInformation> priceList, params DateTime[] datesToSchuedule)
        {
            List<HeaterWeeklyEvent> whScheadule;

            foreach (var targetSchedule in datesToSchuedule)
            {
                whScheadule = new List<HeaterWeeklyEvent>();
                //clean old values.

                var itemsToRemove = WaterHeaterSchedule.Where(x => x.Day == targetSchedule.DayOfWeek).ToArray();
                foreach(var item in itemsToRemove)
                {
                    WaterHeaterSchedule.Remove(item);   
                }

                HeaterWeeklyEvent sch = null;
                var currentPowerLevel = WaterHeaterDesiredPower.Watt2000;

                foreach (var price in priceList)
                {
                    if (price.Start.Date != targetSchedule.Date)
                        continue;

                    if (price.DesiredPower != currentPowerLevel || sch == null)
                    {
                        if(sch != null)
                            whScheadule.Add(sch);

                        sch = new HeaterWeeklyEvent();
                        sch.startDay = targetSchedule.DayOfWeek.ToString();
                        sch.startTime = price.Start.ToShortTimeString();
                        sch.modeId = GetModeFromWaterHeaterDesiredPower(price.DesiredPower);
                        currentPowerLevel = price.DesiredPower;
                    }

                }

                whScheadule.Add(sch);
                WaterHeaterSchedule.AddRange(whScheadule);
            }

            return false;
        }

        public bool VerifyWaterHeaterModes()
        {
            bool allModesGood = true;

            foreach (var mode in WaterHeaterModes)
            {
                bool isGood = true;
                if (mode.name.StartsWith("M6"))
                {
                    isGood = VerifyWaterHeaterMode(mode, WaterHeaterDesiredPower.Watt2000, 70);
                }

                if (mode.name.StartsWith("M5"))
                {
                    isGood = VerifyWaterHeaterMode(mode, WaterHeaterDesiredPower.Watt700, 50);
                }

                if (mode.name.StartsWith("M4"))
                {
                    isGood = VerifyWaterHeaterMode(mode, WaterHeaterDesiredPower.None, 50);
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
