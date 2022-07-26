using MyUplinkSmartConnect.ExternalPrice;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect
{
    public class JobReScheuleheating
    {
        static async Task<iBasePriceInformation?> GetPriceInformation()
        {            
            var powerPrice = new EntsoeAPI();
            _ = await powerPrice.FetchPriceInformation();
            if(powerPrice.PriceList.Count >= 48)
            {
                return powerPrice;
            }
            

            Log.Logger.Debug("Failed to get updated price information, from EU API. Checking nordpool");

            var nordpoolGroup = new Nordpoolgroup();
            await nordpoolGroup.GetPriceInformation();

            if (nordpoolGroup.PriceList.Count >= 48)
            {
                return nordpoolGroup;
            }

            if(nordpoolGroup.PriceList.Count >= 24)
            {
                Log.Logger.Warning("Failed to get price information for tomorrow, will only be able to change todays schedule");
                return nordpoolGroup;
            }

            Log.Logger.Warning("Failed to get price information from both EU API and nordpool");
            return null;
        }

        public static async Task<bool> Work()
        {
            var priceInformation = await GetPriceInformation();
            if (priceInformation == null)
                return false;

            bool hasTomorrowElectricityPrice = (priceInformation.PriceList.Count >= 48);
            var cleanDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

            priceInformation.CreateSortedList(cleanDate, Settings.Instance.WaterHeaterMaxPowerInHours, Settings.Instance.WaterHeaterMediumPowerInHours);

            if(hasTomorrowElectricityPrice)
            {
                priceInformation.CreateSortedList(cleanDate.AddDays(1), Settings.Instance.WaterHeaterMaxPowerInHours, Settings.Instance.WaterHeaterMediumPowerInHours);
            }
            
#if DEBUG
            priceInformation.PrintScheudule();
#endif

            var group = await Settings.Instance.myuplinkApi.GetDevices();
            foreach (var device in group)
            {
                if (device.devices == null)
                {
                    Log.Logger.Error("Group({DeviceId}) does not have devices",device.id);
                    continue;
                }

                foreach (var tmpdevice in device.devices)
                {
                    var costSaving = new ApplyCostSavingRules();
                    costSaving.WaterHeaterSchedule = await Settings.Instance.myuplinkApi.GetWheeklySchedules(tmpdevice);
                    costSaving.WaterHeaterModes = await Settings.Instance.myuplinkApi.GetCurrentModes(tmpdevice);
                    var weekdayOrder = Settings.Instance.myuplinkApi.GetCurrentDayOrder(tmpdevice);

                    if (!costSaving.VerifyWaterHeaterModes())
                    {
                        var status = await Settings.Instance.myuplinkApi.SetCurrentModes(tmpdevice, costSaving.WaterHeaterModes);

                        if (!status)
                        {
                            Log.Logger.Error("Failed to update heater modes, aborting");
                            return false;
                        }
                    }                    

                    if (hasTomorrowElectricityPrice && !costSaving.VerifyHeaterSchedule(priceInformation.PriceList, weekdayOrder, cleanDate, cleanDate.AddDays(1))  || 
                        !hasTomorrowElectricityPrice && !costSaving.VerifyHeaterSchedule(priceInformation.PriceList, weekdayOrder, cleanDate))
                    {
                        var status = await Settings.Instance.myuplinkApi.SetWheeklySchedules(tmpdevice, costSaving.WaterHeaterSchedule);

                        if (!status)
                        {
                            Log.Logger.Error("Failed to update heater schedule, aborting");
                            return false;
                        }
                        else
                        {
                            if(hasTomorrowElectricityPrice)
                                Log.Logger.Information("Changed schedule for {DeviceId} for today and tomorrow",tmpdevice.id);
                            else
                                Log.Logger.Information("Changed schedule for {DeviceId} for today", tmpdevice.id);

                            if (!string.IsNullOrEmpty(Settings.Instance.MQTTServer) && !string.IsNullOrEmpty(device.name))
                            {
                                await Settings.Instance.MQTTSender.SendUpdate(device.name, Models.CurrentPointParameterType.LastScheduleChangeInHours, Convert.ToInt32(0));
                            }
                            return hasTomorrowElectricityPrice; // If we did not get tomorrows prices we return false so we can try the schedule again.
                        }
                    }
                }
            }
            return false;
        }
    }
}