using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using xElectricityPriceApiShared.Model;
using static xElectricityPriceApiShared.ElectricityPrice.Nordpoolgroup;

namespace xElectricityPriceApiShared.ElectricityPrice
{
    internal class VgApi : BasePriceAPI, iBasePriceInformation
    {
        public VgApi(PriceFetcher priceFetcher, ILogger<object> logger) : base(priceFetcher, logger)
        {
            _client = new HttpClient();
        }

        public bool IsPriceInNOK { get { return true; } }

        public bool IsPriceWithVAT { get { return false; } }

        public bool IsPriceInKW { get { return true; } }

        public async Task<bool> GetPriceInformation()
        {
            try
            {
                _priceFetcher.PriceList.Clear();
                var today = await GetVgPriceInformation("https://redutv-api.vg.no/power-data/v1/nordpool/today");
                var tomorrow = await GetVgPriceInformation("https://redutv-api.vg.no/power-data/v1/nordpool/day-ahead/latest");

                if (today && tomorrow)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check VgApi");
                return false;
            }
        }

        async Task<bool> GetVgPriceInformation(string vgURL)
        {
            try
            {
                var response = await _client.GetAsync(vgURL);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to check VgApi, with status {status}", response.StatusCode);
                    return false;
                }

                var strContent = await response.Content.ReadAsStringAsync();
                var rootToday = JsonSerializer.Deserialize<VgRoot>(strContent,JsonUtils.GetJsonSettings());
                if (rootToday == null || rootToday.priceByHour == null) throw new NullReferenceException();

                var hourPrices = GetPriceListByHour(rootToday);

                for (int i = 0; i < hourPrices.Count; i++)
                {
                    var range = GetDateTime(rootToday.priceByHour.date, rootToday.priceByHour.hours[i]);

                    var price = new PricePoint();
                    price.Id = ToGuid(range.start.ToFileTime(), range.end.ToFileTime());
                    price.Start = range.start;
                    price.End = range.end;                    
                    price.SouceApi = nameof(VgApi);
                    price.Price = hourPrices[i] / 100; // Price are listed in øre and not NOK.

                    if(i == (hourPrices.Count -1))
                    {
                        // We are at the last index. So we have to add 
                        price.End = range.end.AddDays(1);
                        price.Id = ToGuid(price.Start.ToFileTime(), price.End.ToFileTime());
                    }


                    if (!_priceFetcher.PriceList.Contains(price) && price.Price != double.MinValue)
                    {
                        _priceFetcher.PriceList.Add(price);
                    }
                }

                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check VgApi");
                return false;
            }
        }

        IList<double> GetPriceListByHour(VgRoot root)
        {
            if (root.priceByHour == null || root.priceByHour.pricesObj == null)
                throw new NullReferenceException();

            switch (_priceFetcher.PowerZone)
            {
                case PowerZoneName.NO1:
                    return root.priceByHour.pricesObj.oslo;

                case PowerZoneName.NO2:
                    return root.priceByHour.pricesObj.kristiansand;

                case PowerZoneName.NO3:
                    return root.priceByHour.pricesObj.trondheim;

                case PowerZoneName.NO4:
                    return root.priceByHour.pricesObj.tromso;

                case PowerZoneName.NO5:
                    return root.priceByHour.pricesObj.bergen;
            }

            return Array.Empty<double>();
        }

        static (DateTime start, DateTime end) GetDateTime(string date, string hourRange)
        {
            //"2022-08-18 01:00:00"
            string strStartTime = $"{date} {hourRange.Substring(0, 2)}:00:00";
            string strEndTime = $"{date} {hourRange.Substring(3)}:00:00";

            var start = DateTime.Parse(strStartTime, new VgApiDateTimeFormat());
            var end = DateTime.Parse(strEndTime, new VgApiDateTimeFormat());

            return (start, end);
        }

        class VgPrice2
        {
            public string name { get; set; } = "";
            public IEnumerable<double> data { get; set; } = Array.Empty<double>();
        }

        class VgPriceByHour
        {
            public string date { get; set; } = "";
            public string updated { get; set; } = "";
            public IList<string> hours { get; set; } = Array.Empty<string>();
            public bool isToday { get; set; }
            public VgPricesObj? pricesObj { get; set; }
        }

        class VgPricesObj
        {
            public IList<double> oslo { get; set; } = Array.Empty<double>(); 
            public IList<double> kristiansand { get; set; } = Array.Empty<double>();
            public IList<double> molde { get; set; } = Array.Empty<double>();
            public IList<double> bergen { get; set; } = Array.Empty<double>();
            public IList<double> trondheim { get; set; } = Array.Empty<double>();
            public IList<double> tromso { get; set; } = Array.Empty<double>();
        }

        class VgRoot
        {
            public string date { get; set; } = "";
            public string updated { get; set; } = "";
            public VgPriceByHour? priceByHour { get; set; }
        }
    }
}
