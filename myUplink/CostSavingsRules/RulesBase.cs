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

                if (!isGood)
                    allModesGood = false;
            }

            return allModesGood;
        }

        public List<WaterHeaterMode> WaterHeaterModes { get; set; } = new List<WaterHeaterMode>();

        public List<HeaterWeeklyEvent> WaterHeaterSchedule { get; set; } = new List<HeaterWeeklyEvent>();

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
