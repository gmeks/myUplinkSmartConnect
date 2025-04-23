using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using xElectricityPriceApiShared.Model;

namespace xElectricityPriceApiShared.ElectricityPrice
{
    internal class HvaKosterStrommen : BasePriceAPI, iBasePriceInformation
    {
        public HvaKosterStrommen(PriceFetcher priceFetcher, ILogger<object> logger) : base(priceFetcher, logger)
        {
            _client = new HttpClient();
        }

        public bool IsPriceInNOK { get { return true; } }

        public bool IsPriceWithVAT { get { return false; } }

        public bool IsPriceInKW { get { return false; } }

        public async Task<bool> GetPriceInformation()
        {
            //https://www.hvakosterstrommen.no/api/v1/prices/2025/04-14_NO5.json
            //https://www.hvakosterstrommen.no/api/v1/prices/2025/04-14_NO2.json 

            try
            {
                _priceFetcher.PriceList.Clear();
                var today = await Getprice(GetUrl(DateTime.Now));
                var tomorrow = await Getprice(GetUrl(DateTime.Now.AddDays(1)));

                if (today && tomorrow)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check HvaKosterStrommen");
                return false;
            }
        }

        async Task<bool> Getprice(Uri vgURL)
        {
            try
            {
                var response = await _client.GetAsync(vgURL);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to check HvaKosterStrommen, with status {status}", response.StatusCode);
                    return false;
                }
                var strContent = await response.Content.ReadAsStringAsync();
                var rootToday = JsonSerializer.Deserialize<HvaKosteStrommenJson[]>(strContent, JsonUtils.GetJsonSettings());
                if (rootToday == null || rootToday.Length == 0) throw new NullReferenceException();

                for (int i = 0; i < rootToday.Length; i++)
                {
                    //var range = GetDateTime(rootToday.priceByHour.date, rootToday.priceByHour.hours[i]);

                    var price = new PricePoint();
                    price.Id = ToGuid(rootToday[i].time_start.ToFileTime(), rootToday[i].time_end.ToFileTime());
                    price.Start = rootToday[i].time_start;
                    price.End = rootToday[i].time_end;
                    price.Price = rootToday[i].NOK_per_kWh;
                    price.SouceApi = nameof(HvaKosterStrommen);

                    if (!_priceFetcher.PriceList.Contains(price) && price.Price != double.MinValue)
                    {
                        _priceFetcher.PriceList.Add(price);
                    }
                }

                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check HvaKosterStrommen");
                return false;
            }
        }

        Uri GetUrl(DateTime datetime)
        {
            return new Uri($"https://www.hvakosterstrommen.no/api/v1/prices/{datetime.Year}/{GetNumber(datetime.Month)}-{GetNumber(datetime.Day)}_{_priceFetcher.PowerZone}.json ");
        }

        string GetNumber(int input)
        {
            if(input >= 10)
                return input.ToString();

            return "0" + input.ToString();
        }

        public class HvaKosteStrommenJson
        {
            public double NOK_per_kWh { get; set; }
            public double EUR_per_kWh { get; set; }
            public double EXR { get; set; }
            public DateTime time_start { get; set; }
            public DateTime time_end { get; set; }
        }
    }
}
