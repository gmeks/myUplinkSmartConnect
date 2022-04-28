
using myUplink.ModelsPublic.Internal;
using System.Net.Http.Headers;
using System.Text.Json;

namespace myUplink
{
    public class Program
    {
        public static  async Task<int> Main(string[] args)
        {
            string settingsFile;
#if DEBUG
            settingsFile = "appsettings.Development.json";
#else
            settingsFile = "appsettings.json";
#endif
            if(!File.Exists(settingsFile))
            {
                Console.WriteLine($"No settings file found {settingsFile}");
                return 200;
            }

            var settings = JsonSerializer.Deserialize< Settings >(File.ReadAllText(settingsFile));

            var powerPrice = new EntsoeAPI();
            await powerPrice.FetchPriceInformation("5cd1c4f6-2172-4453-a8bb-c9467fa0fabc");

            powerPrice.CreateSortedList(DateTime.Now,5, 6);
            powerPrice.CreateSortedList(DateTime.Now.AddDays(1), 5, 6);

            powerPrice.PrintScheudule();

            var interalAPI = new InternalAPI();
            await interalAPI.LoginToApi(settings.UserName, settings.Password);

            var test = await interalAPI.GetDevices();
            foreach(var device in test)
            {
                foreach (var tmpdevice in device.devices)
                {
                    var costSaving = new ApplyCostSavingRules();
                    costSaving.WaterHeaterSchedule = await interalAPI.GetWheeklySchedules(tmpdevice);
                    costSaving.WaterHeaterModes = await interalAPI.GetCurrentModes(tmpdevice);

                    if(!costSaving.VerifyWaterHeaterModes())
                    {
                        await interalAPI.SetCurrentModes(tmpdevice, costSaving.WaterHeaterModes);
                    }

                    if (!costSaving.VerifyHeaterSchedule(powerPrice.PriceList, DateTime.Now, DateTime.Now.AddDays(1)))
                    {
                        await interalAPI.SetWheeklySchedules(tmpdevice, costSaving.WaterHeaterSchedule);
                    }
                }
            }

            //var test = await interalAPI.GetCurrentSchedule();

            return 0;
        }


   }
}
