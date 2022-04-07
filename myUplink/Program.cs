
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
            var login = new Login();

            await login.LoginToApi(settings.clientIdentifier, settings.clientSecret);
            await login.Ping();

            var systems = await login.GetUserSystems();

            foreach(var system in systems)
            {
                foreach(var deviceId in system.devices)
                {
                    //var info11 = await login.GetDeviceInfo(deviceId.id);
                    var info = await login.GetDeviceInfoPoints(deviceId.id);

                    foreach(var tmpInfo in info)
                    {
                        Console.WriteLine(tmpInfo.parameterName + " - " + tmpInfo.strVal); 
                    }
                }
            }
            return 0;
        }
   }


    class Settings
    {
        public string clientIdentifier { get; set; }

        public string clientSecret { get; set; }
    }
}
