using MyUplinkSmartConnect.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect.ExternalPrice
{
    internal class BasePriceAPI : iBasePriceInformation
    {
        internal HttpClient _client;
        internal List<stPriceInformation> _priceList;
        internal static IList<string> NorwayPowerZones = new string[] { "NO-1", "NO-2", "NO-3", "NO-4", "NO-5" }.ToList();


        public BasePriceAPI()
        {
            _client = new HttpClient();
            _priceList = new List<stPriceInformation>();
        }

        internal int GetPowerRegionIndex()
        {
            int powerRegionIndex = -1;

            if (!string.IsNullOrEmpty(Settings.Instance.PowerZone))
            {
                for (int i = 0; i < NorwayPowerZones.Count; i++)
                {
                    if (Settings.Instance.PowerZone.Equals(NorwayPowerZones[i], StringComparison.OrdinalIgnoreCase) || Settings.Instance.PowerZone.Equals(NorwayPowerZones[i].Replace("-", ""), StringComparison.OrdinalIgnoreCase))
                    {
                        return  i;
                    }
                }
            }

            if (powerRegionIndex == -1)
            {
                powerRegionIndex = NorwayPowerZones.IndexOf("NO-2");
                Log.Logger.Warning("Using default power zone NO-2");
                Log.Logger.Warning("Valid zones are:");

                foreach(var zone in NorwayPowerZones)
                    Log.Logger.Warning(zone);
            }

            return powerRegionIndex;
        }

        public void PrintScheudule()
        {
            foreach (var price in _priceList)
            {
                Console.WriteLine($"{price.Start.Day}) Start: {price.Start.ToShortTimeString()} | {price.End.ToShortTimeString()} - {price.DesiredPower} - {price.Price}");
            }
        }

        public void CreateSortedList(DateTime filterDate, int desiredMaxpower, int mediumPower)
        {
            var sortedList = new List<stPriceInformation>(24);
            foreach (stPriceInformation price in _priceList)
            {
                if (price.Start.Date != filterDate.Date)
                    continue;

                sortedList.Add(price);
            }

            sortedList.Sort(new SortByLowestPrice());
            var maxPowerHours = sortedList.Take(desiredMaxpower);
            var mediumPowerHours = sortedList.Take(mediumPower + desiredMaxpower);

            for (int i = 0; i < sortedList.Count; i++)
            {
                if (maxPowerHours.Contains(sortedList[i]))
                {
                    sortedList[i].DesiredPower = WaterHeaterDesiredPower.Watt2000;
                }
                else if (mediumPowerHours.Contains(sortedList[i]))
                {
                    sortedList[i].DesiredPower = WaterHeaterDesiredPower.Watt700;
                }
            }

            _priceList.Sort(new SortByStartDate());
            foreach (var price in _priceList)
            {
                var updatedPrice = sortedList.FirstOrDefault(x => x.Id == price.Id);
                if (updatedPrice != null)
                {
                    price.DesiredPower = updatedPrice.DesiredPower;
                }
            }
        }
        internal static double Parse(string input)
        {
            if (input == null || input.Length == 0)
                return 0;

            input = input.Replace("\"", "");

            for (int i = input.Length - 1; i > 0; i--)
            {
                if (input[i] == ',')
                {
                    var tmpStr = input.ToArray();
                    tmpStr[i] = '.';

                    return double.Parse(tmpStr, NumberStyles.Any, CultureInfo.InvariantCulture);
                }
            }

            return double.Parse(input, NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public List<stPriceInformation> PriceList { get { return _priceList; } }
    }

    class ApiDateTimeFormat : IFormatProvider
    {
        public object? GetFormat(Type? formatType)
        {
            return "dd.MM.yyyy HH:mm:ss";
        }
    }

    class SortByLowestPrice : IComparer<stPriceInformation>
    {
        public int Compare(stPriceInformation? x, stPriceInformation? y)
        {
            if (x == null && y == null)
                return 0;

            if (x == null)
                return 1;

            if (y == null)
                return -1;

            if (x.Price == y.Price)
                return 0;

            if (x.Price < y.Price)
                return -1;

            return 1;
        }
    }

    class SortByStartDate : IComparer<stPriceInformation>
    {
        public int Compare(stPriceInformation? x, stPriceInformation? y)
        {
            if (x == null)
                return 1;

            return x.Start.CompareTo(y?.Start);
        }
    }
}
