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
    internal class BasePriceAPI
    {
        internal HttpClient _client;
        internal static IList<string> NorwayPowerZones = new string[] { "NO-1", "NO-2", "NO-3", "NO-4", "NO-5" }.ToList();


        public BasePriceAPI()
        {
            _client = new HttpClient();
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
                const string DefaultRegion = "NO-2";

                powerRegionIndex = NorwayPowerZones.IndexOf(DefaultRegion);
                Settings.Instance.PowerZone = DefaultRegion;

                Log.Logger.Warning("Using default power zone " + DefaultRegion);
                Log.Logger.Warning("Valid zones are:");

                foreach(var zone in NorwayPowerZones)
                    Log.Logger.Warning(zone);
            }

            return powerRegionIndex;
        }


        internal string ConvertRegionName(string powerzoneName,bool logFailedLookups = true)
        {
            /*
             * Kr.sand
            Bergen
            Molde
            Tr.heim
            Tromsø
             */
            powerzoneName = powerzoneName.ToLowerInvariant();
            switch (powerzoneName)
            {

                case "oslo":
                    return "NO-1";

                case "kr.sand":
                case "kristiansand":
                    return "NO-2";

                case "molde":
                case "trondheim":
                case "tr.heim":
                    return "NO-3";

                case "tromsø":
                case "tromso":
                    return "NO-4";

                case "bergen":
                    return "NO-5";
            }

            if(logFailedLookups)
            {
                Log.Logger.Warning("Failed to find tekniskal name of powerzone from {pwrZone}", powerzoneName);
            }
            
            return "";
        }

        public void CreateSortedList(DateTime filterDate, int desiredMaxpower, int mediumPower)
        {
            var sortedList = new List<ElectricityPriceInformation>(24);
            foreach (ElectricityPriceInformation price in CurrentState.PriceList)
            {
                if (price.Start.Date != filterDate.Date)
                    continue;

                sortedList.Add(price);
            }

            sortedList.Sort(new SortByLowestPrice());

            IEnumerable<ElectricityPriceInformation> maxPowerHours = Array.Empty<ElectricityPriceInformation>();
            IEnumerable<ElectricityPriceInformation> mediumPowerHours = Array.Empty<ElectricityPriceInformation>();
            if (desiredMaxpower != 0)
                maxPowerHours = sortedList.Take(desiredMaxpower);

            if(mediumPower != 0)
                mediumPowerHours = sortedList.Take(mediumPower + desiredMaxpower);

            for (int i = 0; i < sortedList.Count; i++)
            {
                if (maxPowerHours.Contains(sortedList[i]))
                {
                    sortedList[i].HeatingMode =  HeatingMode.HighestTemperature;
                }
                else if (mediumPowerHours.Contains(sortedList[i]))
                {
                    sortedList[i].HeatingMode =  HeatingMode.MediumTemperature;
                }
            }

            CurrentState.PriceList.Sort(new SortByStartDate());
            foreach (var price in CurrentState.PriceList)
            {
                var updatedPrice = sortedList.FirstOrDefault(x => x.Id == price.Id);
                if (updatedPrice != null)
                {
                    price.HeatingMode = updatedPrice.HeatingMode;
                }
            }
        }

        internal static double Parse(string input)
        {
            if (string.IsNullOrEmpty(input) || input == "-")
                return double.MinValue;

            input = input.Replace("\"", "");

            for (int i = input.Length - 1; i > 0; i--)
            {
                if (input[i] == ',')
                {
                    var tmpStr = input.ToArray();
                    tmpStr[i] = '.';

                    return ParseDoubleLogFail(new string(tmpStr));
                }
            }

            return ParseDoubleLogFail(input);
        }

        static double ParseDoubleLogFail(ReadOnlySpan<char> input)
        {
            if (double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            Log.Logger.Debug("Failed to parse double from string:{value}", input.ToString());
            return double.MinValue;
        }

        public List<ElectricityPriceInformation> PriceList { get { return CurrentState.PriceList; } }
    }

    class ApiDateTimeFormat : IFormatProvider
    {
        public object? GetFormat(Type? formatType)
        {
            return "dd.MM.yyyy HH:mm:ss";
        }
    }

    class VgApiDateTimeFormat : IFormatProvider
    {
        public object? GetFormat(Type? formatType)
        {
            return "dd-MM-yyyy HH:mm:ss";
        }
    }

    class SortByLowestPrice : IComparer<ElectricityPriceInformation>
    {
        public int Compare(ElectricityPriceInformation? x, ElectricityPriceInformation? y)
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

    class SortByStartDate : IComparer<ElectricityPriceInformation>
    {
        public int Compare(ElectricityPriceInformation? x, ElectricityPriceInformation? y)
        {
            if (x == null)
                return 1;

            return x.Start.CompareTo(y?.Start);
        }
    }
}
