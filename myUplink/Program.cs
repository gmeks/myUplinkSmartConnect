
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

            /*
            var login = new PublicAPI();

            await login.LoginToApi(settings.clientIdentifier, settings.clientSecret);
            await login.Ping();

            var systems = await login.GetUserSystems();

            foreach(var system in systems)
            {
                foreach(var deviceId in system.devices)
                {
                    var info = await login.GetDeviceInfoPoints(deviceId.id);
                    foreach(var tmpInfo in info)
                    {
                        Console.WriteLine(tmpInfo.parameterName + " - " + tmpInfo.strVal); 
                    }
                }
            }
            */

            var powerPrice = new EntsoeAPI();
            await powerPrice.GetPrices();

            powerPrice.CreateSortedList(5, 6);

            var interalAPI = new InternalAPI();
            await interalAPI.LoginToApi(settings.UserName, settings.Password);

            var test = await interalAPI.GetDevices();
            foreach(var device in test)
            {
                foreach (var tmpdevice in device.devices)
                {
                    var currentSchedule = await interalAPI.GetWheeklySchedules(tmpdevice);
                    var currentModes = await interalAPI.GetCurrentModes(tmpdevice);
                }
            }

            //var test = await interalAPI.GetCurrentSchedule();

            return 0;
        }
   }
}
