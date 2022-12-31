using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using static MyUplinkSmartConnect.ExternalPrice.Nordpoolgroup;

namespace MyUplinkSmartConnect.ExternalPrice
{
    internal class VgApi : BasePriceAPI, iBasePriceInformation
    {
        public VgApi()
        {
            _client = new HttpClient();
        }

        public async Task<bool> GetPriceInformation()
        {
            try
            {
                _currentState.PriceList.Clear();
                var today = await GetVgPriceInformation("https://redutv-api.vg.no/power-data/v1/nordpool/today");
                var tomorrow = await GetVgPriceInformation("https://redutv-api.vg.no/power-data/v1/nordpool/day-ahead/latest");

                if(today && tomorrow)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to check VgApi");
                return false;
            }
        }

        async Task<bool> GetVgPriceInformation(string vgURL)
        {
            try
            {
                var powerRegionIndex = GetPowerRegionIndex();
                var response = await _client.GetAsync(vgURL);

                if(!response.IsSuccessStatusCode)
                {
                    Log.Logger.Warning("Failed to check VgApi, with status {status}", response.StatusCode);
                    return false;
                }

                var strContent = await response.Content.ReadAsStringAsync();
                var rootToday = JsonSerializer.Deserialize<VgRoot>(strContent) ?? new VgRoot();

                var hourPrices = GetPriceListByHour(powerRegionIndex, rootToday);

                for (int i = 0; i < hourPrices.Count; i++)
                {
                    var range = GetDateTime(rootToday.priceByHour.date, rootToday.priceByHour.hours[i]);

                    var price = new ElectricityPriceInformation();                    
                    price.Id = ToGuid(range.start.ToFileTime(), range.end.ToFileTime());
                    price.Start = range.start;
                    price.End = range.end;
                    price.Price = hourPrices[i];

                    if (!_currentState.PriceList.Contains(price) && price.Price != double.MinValue)
                    {
                        _currentState.PriceList.Add(price);
                    }
                }

                return true;

            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to check VgApi");
                return false;
            }
        }


        IList<double> GetPriceListByHour(int powerRegionIndex, VgRoot root)
        {
            switch (NorwayPowerZones[powerRegionIndex])
            {
                case "NO-1":
                    return root.priceByHour.pricesObj.oslo;

                case "NO-2":
                    return root.priceByHour.pricesObj.kristiansand;

                case "NO-3":
                    return root.priceByHour.pricesObj.trondheim;

                case "NO-4":
                    return root.priceByHour.pricesObj.tromso;

                case "NO-5":
                    return root.priceByHour.pricesObj.bergen;
            }

            return Array.Empty<double>();
        }

        static (DateTime start,DateTime end) GetDateTime(string date,string hourRange)
        {
            //"2022-08-18 01:00:00"
            string strStartTime = $"{date} {hourRange.Substring(0,2)}:00:00";
            string strEndTime = $"{date} {hourRange.Substring(3)}:00:00";


            var start = DateTime.Parse(strStartTime, new VgApiDateTimeFormat());
            var end = DateTime.Parse(strEndTime, new VgApiDateTimeFormat());
            end = end.AddMinutes(-1);

            return (start,end);
        }

        class VgPrice2
        {
            public string name { get; set; }
            public List<double> data { get; set; }
        }

        class VgPriceByHour
        {
            public string date { get; set; }
            public string updated { get; set; }
            public List<string> hours { get; set; }
            public bool isToday { get; set; }
            public VgPricesObj pricesObj { get; set; }
        }

        class VgPricesObj
        {
            public List<double> oslo { get; set; }
            public List<double> kristiansand { get; set; }
            public List<double> molde { get; set; }
            public List<double> bergen { get; set; }
            public List<double> trondheim { get; set; }
            public List<double> tromso { get; set; }
        }

        class VgRoot
        {
            public string date { get; set; }
            public string updated { get; set; }
            public VgPriceByHour priceByHour { get; set; }
        }
    }
}
