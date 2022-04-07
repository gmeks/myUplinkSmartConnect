using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TibberForbrukWeb.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<RealtimePowerUsage> Get()
        {
            var itemsList = LiteWrapper.GetAll().ToArray();
            /*var text = new StringBuilder();
            text.AppendLine("Date,watt,volt");

            foreach(var item in itemsList)
            {
                text.AppendLine($"{item.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss")},{item.Watt},{item.Volt}");
            }

            return text.ToString();
            */
            for(int i=0;i<itemsList.Length;i++)
            {
                itemsList[i].strTimestamp = GetCompatibleDateTimeString(itemsList[i].Timestamp);
            }

            return itemsList;
        }


        public static string GetCompatibleDateTimeString(DateTime SomeTime)
        {
            //YYYY/MM/DD
            //string value = SomeTime.ToString("MM-dd-yyyy HH:mm:ss");
            string value = SomeTime.ToString("yyyy-MM-dd HH:mm:ss");
            value = value.Replace('-', '/');
            value = value.Replace('.', ':'); // This is stupid. No idea why it inserts .
            return value;
        }


        /*public IEnumerable<RealtimePowerUsage> Get()
        {
            string

            return LiteWrapper.GetAll();
        }*/
    }
}
