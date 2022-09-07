using MyUplinkSmartConnect.ExternalPrice;
using MyUplinkSmartConnect.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect
{
    [Flags]
    public enum States
    {
        Ok = 1 << 0,
        RemotePrices = 1 << 1,
        Schedule = 1 << 2,
        HeaterStats = 1 << 3,
        MQTTFailed= 1 << 4,
    }

    internal static class CurrentState
    {
        static States _currentFailed = States.Ok;
        static DateTime _lastStatePublish = DateTime.MinValue;


        public static void SetSuccess(States state)
        {
            bool foundChanges = false;

            if(_currentFailed.HasFlag(state))
            {
                Log.Logger.Debug("{state} is now working, removing from failed", state);
                Settings.Instance.MQTTSender.SendUpdate(Models.CurrentPointParameterType.LogEntry, "", true).Wait(); // Incase the last run had a log entry
                _currentFailed -= _currentFailed;
                foundChanges = true;
            }

            if(_lastStatePublish == DateTime.MinValue)
            {
                Settings.Instance.MQTTSender.SendUpdate(Models.CurrentPointParameterType.LogEntry, "",true).Wait(); // Incase the last run had a log entry
                _lastStatePublish = DateTime.Now;
                foundChanges = true;
            }

            if(foundChanges)
            {
                PublishChanges();
            }            
        }

        public static void SetFailed(States state)
        {
            bool foundChanges = false;

            if (!_currentFailed.HasFlag(state))
            {
                Log.Logger.Debug("{state} is now failed", state);
                _currentFailed |= _currentFailed;
                foundChanges = true;
            }

            if (foundChanges)
            {
                PublishChanges();
            }
        }


        static void PublishChanges()
        {
            _lastStatePublish = DateTime.Now;
            Settings.Instance.MQTTSender.SendUpdate(Models.CurrentPointParameterType.ServiceStatus, _currentFailed, true).Wait();
        }

        public static List<ElectricityPriceInformation> PriceList { get; set; } = new List<ElectricityPriceInformation>();

        public static double CurrentTankEnergi { get; set; }

        public static WaterHeaterModeLookup ModeLookup { get; set; } = new WaterHeaterModeLookup(Array.Empty<WaterHeaterMode>());
}
}
