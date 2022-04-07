
namespace myUplink
{
    public class Program
    {
        public static  async Task<int> Main(string[] args)
        {
            var login = new Login();

            await login.LoginToApi("42c78ce2-51b9-4af9-8e14-d26a5f3af2e5", "67439170B6E90314F8C21FDB6403F06D");
            await login.Ping();

            var systems = await login.GetUserSystems();

            foreach(var system in systems)
            {
                foreach(var deviceId in system.devices)
                {
                    await login.GetUserSystems(deviceId.id);
                }
            }
            return 0;
        }
   }
}
