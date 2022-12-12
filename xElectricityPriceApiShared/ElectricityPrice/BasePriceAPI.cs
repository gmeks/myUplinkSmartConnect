using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xElectricityPriceApiShared.Model;

namespace xElectricityPriceApiShared.ElectricityPrice
{
    internal class BasePriceAPI
    {
        internal HttpClient _client;
        internal ILogger<object> _logger;
        internal PriceFetcher _priceFetcher;

        public BasePriceAPI(PriceFetcher priceFetcher, ILogger<object> logger)
        {
            _client = new HttpClient();
            _logger = logger;
            _priceFetcher = priceFetcher;
        }

        internal PowerZoneName ConvertRegionName(string powerzoneName, bool logFailedLookups = true)
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

                case "no1":
                case "no-1":
                case "oslo":
                    return PowerZoneName.NO1;

                case "no2":
                case "no-2":
                case "kr.sand":
                case "kristiansand":
                    return PowerZoneName.NO2;

                case "no3":
                case "no-3":
                case "molde":
                case "trondheim":
                case "tr.heim":
                    return PowerZoneName.NO3;

                case "no4":
                case "no-4":
                case "tromsø":
                case "tromso":
                    return PowerZoneName.NO4;

                case "no5":
                case "no-5":
                case "bergen":
                    return PowerZoneName.NO5;
            }

            if (logFailedLookups)
            {
                _logger.LogWarning("Failed to find tekniskal name of powerzone from {pwrZone}", powerzoneName);
            }

            return  PowerZoneName.NO2;
        }

        internal double Parse(string input)
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

        double ParseDoubleLogFail(ReadOnlySpan<char> input)
        {
            if (double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            _logger.LogDebug("Failed to parse double from string:{value}", input.ToString());
            return double.MinValue;
        }

        internal static Guid ToGuid(long startTime, long endtime)
        {
            byte[] guidData = new byte[16];
            Array.Copy(BitConverter.GetBytes(startTime), guidData, 8);
            Array.Copy(BitConverter.GetBytes(endtime), 0, guidData, 8, 8);

            return new Guid(guidData);
        }
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

    class SortByLowestPrice : IComparer<PricePoint>
    {
        public int Compare(PricePoint? x, PricePoint? y)
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

    class SortByStartDate : IComparer<PricePoint>
    {
        public int Compare(PricePoint? x, PricePoint? y)
        {
            if (x == null)
                return 1;

            return x.Start.CompareTo(y?.Start);
        }
    }
}
