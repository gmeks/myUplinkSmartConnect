using MyUplinkSmartConnect.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect
{
    public enum HeatingMode
    {
        Unkown,
        HighestTemperature,
        MediumTemperature,
        HeathingDisabled,
        MediumTemprature1300watt,
        HeatingLegionenna,
    }

    internal class WaterHeaterModeLookup
    {
        /*
         *  M6 High temprature
         *  M5 Medium temprature.
         *  M4 Heating disabled
         *  M3 Heating medium ( 1300 watt)
         *  M2 Heating legionella (2000watt)
         */

        IList<WaterHeaterMode> _waterHeaterModes;
        Dictionary<HeatingMode, int> _heatingModeLookup;

        public WaterHeaterModeLookup(IList<WaterHeaterMode> waterHeaterModes)
        {
            ReCheckModes(waterHeaterModes);
        }

        public IList<WaterHeaterMode> WaterHeaterModes { get { return _waterHeaterModes; } }

        public bool ReCheckModes(IList<WaterHeaterMode> waterHeaterModes)
        {
            _waterHeaterModes = waterHeaterModes;
            _heatingModeLookup = new Dictionary<HeatingMode, int>();

            return VerifyWaterHeaterModes();
        }       

        public int GetHeatingModeId(HeatingMode mode)
        {
            return _heatingModeLookup[mode];
        }

        public HeatingMode GetHeatingModeFromId(int modeId)
        {
            foreach(var item in _heatingModeLookup)
            {
                if (item.Value == modeId)
                    return item.Key;
            }

            throw new EntryPointNotFoundException("Failed to find mode id " + modeId);
        }

        public WaterHeaterDesiredPower GetHeatingPower(HeatingMode mode)
        {
           switch(mode)
            {
                case HeatingMode.HeathingDisabled:
                    return WaterHeaterDesiredPower.None;

                case HeatingMode.HighestTemperature:
                case HeatingMode.HeatingLegionenna:
                    return WaterHeaterDesiredPower.Watt2000;

                case HeatingMode.MediumTemperature:
                    return WaterHeaterDesiredPower.Watt700;

                case HeatingMode.MediumTemprature1300watt:
                    return WaterHeaterDesiredPower.Watt1300;

                default:
                    throw new EntryPointNotFoundException(mode.ToString() + " is not found in function GetHeatingPower" );
            }
        }

        public double GetHeatingPowerInKwh(HeatingMode mode)
        {
            var tmp = GetHeatingPower(mode);
            return GetHeatingPowerInKwh(tmp);
        }

        public double GetHeatingPowerInKwh(WaterHeaterDesiredPower mode)
        {
            switch (mode)
            {
                case WaterHeaterDesiredPower.Watt2000:
                    return 2.0d;

                case WaterHeaterDesiredPower.Watt1300:
                    return 1.3d;

                case WaterHeaterDesiredPower.Watt700:
                    return 0.7d;

                case WaterHeaterDesiredPower.None:
                    return 0;

                default:
                    throw new EntryPointNotFoundException(mode.ToString() + " is not found in function GetHeatingPower");
            }
        }

        bool VerifyWaterHeaterModes()
        {
            bool allModesGood = true;

            foreach (var mode in _waterHeaterModes)
            {
                if (string.IsNullOrEmpty(mode.name))
                    throw new NullReferenceException("mode.name cannot be null");

                bool isGood = true;
                if (mode.name.StartsWith("M6"))
                {
                    isGood = VerifyWaterHeaterMode(mode, GetHeatingPower(HeatingMode.HighestTemperature), Settings.Instance.HighPowerTargetTemperature);

                    _heatingModeLookup.Add(HeatingMode.HighestTemperature ,mode.modeId);
                }

                if (mode.name.StartsWith("M5"))
                {
                    isGood = VerifyWaterHeaterMode(mode, GetHeatingPower(HeatingMode.MediumTemperature), Settings.Instance.MediumPowerTargetTemperature);
                    _heatingModeLookup.Add(HeatingMode.MediumTemperature, mode.modeId);
                }

                if (mode.name.StartsWith("M4"))
                {
                    isGood = VerifyWaterHeaterMode(mode, GetHeatingPower(HeatingMode.HeathingDisabled), Settings.Instance.MediumPowerTargetTemperature);
                    _heatingModeLookup.Add(HeatingMode.HeathingDisabled, mode.modeId);
                }

                if (Settings.Instance.EnergiBasedCostSaving && mode.name.StartsWith("M3"))
                {
                    isGood = VerifyWaterHeaterMode(mode, GetHeatingPower(HeatingMode.MediumTemprature1300watt), Settings.Instance.MediumPowerTargetTemperature);
                    _heatingModeLookup.Add(HeatingMode.MediumTemprature1300watt, mode.modeId);
                }

                if (Settings.Instance.RequireUseOfM2ForLegionellaProgram && mode.name.StartsWith("M2"))
                {
                    isGood = VerifyWaterHeaterMode(mode, GetHeatingPower(HeatingMode.HeatingLegionenna), 75);
                    _heatingModeLookup.Add(HeatingMode.HeatingLegionenna, mode.modeId);
                }

                if (!isGood)
                    allModesGood = false;
            }

            if (!Settings.Instance.RequireUseOfM2ForLegionellaProgram)
            {
                // We dont need a spesial program for legionella, we can use M6.
                _heatingModeLookup.Add(HeatingMode.HeatingLegionenna, GetHeatingModeId(HeatingMode.HighestTemperature));
            }

            return allModesGood;
        }       

        static bool VerifyWaterHeaterMode(WaterHeaterMode mode, WaterHeaterDesiredPower desiredPower, int targetTemprature)
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
