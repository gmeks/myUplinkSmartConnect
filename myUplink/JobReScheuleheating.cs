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
        public static async Task Work()
        {
            var powerPrice = new EntsoeAPI();
            await powerPrice.FetchPriceInformation("5cd1c4f6-2172-4453-a8bb-c9467fa0fabc");

            powerPrice.CreateSortedList(DateTime.Now, Settings.Instance.WaterHeaterMaxPowerInHours, Settings.Instance.WaterHeaterMediumPowerInHours);
            powerPrice.CreateSortedList(DateTime.Now.AddDays(1), Settings.Instance.WaterHeaterMaxPowerInHours, Settings.Instance.WaterHeaterMediumPowerInHours);
            powerPrice.PrintScheudule();

            var group = await Settings.Instance.myuplinkApi.GetDevices();
            foreach (var device in group)
            {
                foreach (var tmpdevice in device.devices)
                {
                    var costSaving = new ApplyCostSavingRules();
                    costSaving.WaterHeaterSchedule = await Settings.Instance.myuplinkApi.GetWheeklySchedules(tmpdevice);
                    costSaving.WaterHeaterModes = await Settings.Instance.myuplinkApi.GetCurrentModes(tmpdevice);

                    if (!costSaving.VerifyWaterHeaterModes())
                    {
                        var status = await Settings.Instance.myuplinkApi.SetCurrentModes(tmpdevice, costSaving.WaterHeaterModes);

                        if (!status)
                        {
                            Log.Logger.Error("Failed to update heater modes, aborting");
                            return;
                        }
                    }

                    if (!costSaving.VerifyHeaterSchedule(powerPrice.PriceList, DateTime.Now, DateTime.Now.AddDays(1)))
                    {
                        var status = await Settings.Instance.myuplinkApi.SetWheeklySchedules(tmpdevice, costSaving.WaterHeaterSchedule);

                        if (!status)
                        {
                            Log.Logger.Error("Failed to update heater schedule, aborting");
                            return;
                        }
                    }
                }
            }
        }
    }
}
