using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using xElectricityPriceApiShared.Model;
using static xElectricityPriceApiShared.ElectricityPrice.Nordpoolgroup;

namespace xElectricityPriceApiShared.ElectricityPrice
{
    internal class EntsoeAPI : BasePriceAPI, iBasePriceInformation
    {
        //https://transparency.entsoe.eu/content/static_content/Static%20content/web%20api/Guide.html#_authentication_and_authorisation      

        public EntsoeAPI(PriceFetcher priceFetcher, ILogger<object> logger) : base(priceFetcher, logger)
        {
            _client = new HttpClient();
        }

        public bool IsPriceInNOK { get { return false; } }

        public bool IsPriceWithVAT { get { return false; } }

        public bool IsPriceInKW { get { return false; } }

        public async Task<bool> GetPriceInformation()
        {
            var result = await GetPriceInformation(DateTime.Now, DateTime.Now.AddDays(1));
            return result;
        }            

        public async Task<bool> GetPriceInformation(DateTime startFind,DateTime endFind)
        {
            // i hate xml, and this function is a pure pain
            string startDateFormat = startFind.ToString("yyyyMMdd") + "0000";
            string endDateFormat = endFind.AddDays(1).ToString("yyyyMMdd") + "0000";

            try
            {
                _priceFetcher.PriceList.Clear();

                string url = $"https://transparency.entsoe.eu/api?documentType=A44&in_Domain=10Y{GetPowerZoneName(_priceFetcher.PowerZone)}--------T&out_Domain=10Y{GetPowerZoneName(_priceFetcher.PowerZone)}--------T&periodStart={startDateFormat}&periodEnd={endDateFormat}&securityToken=5cd1c4f6-2172-4453-a8bb-c9467fa0fabc";
                var response = await _client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed request against entsoe API, with status code: {httpStatus}", response.StatusCode);
                    return false;
                }

                var xmlText = await response.Content.ReadAsStringAsync();

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlText);

                XmlNode? xmlNode = doc?.LastChild;
                if (xmlNode == null)
                {
                    _logger.LogWarning("Failed to parse XML returned from entsoe");
                    return false;
                }

                foreach (XmlNode child in xmlNode)
                {
                    if (child.Name != "TimeSeries")
                        continue;

                    DateTime startTime = DateTime.MinValue;
                    foreach (XmlNode xmlPeriod in child.ChildNodes)
                    {
                        if (xmlPeriod.Name != "Period")
                            continue;


                        foreach (XmlNode actualData in xmlPeriod.ChildNodes)
                        {
                            if (actualData.Name == "timeInterval")
                            {
                                foreach (XmlNode timeIntervalNode in actualData.ChildNodes)
                                {
                                    if (timeIntervalNode.Name == "end")
                                    {
                                        string strDate = timeIntervalNode.InnerText.Substring(0, timeIntervalNode.InnerText.IndexOf('T'));
                                        startTime = ParseDateTime(strDate);
                                    }
                                }
                            }
                            else if (actualData.Name == "Point")
                            {
                                var price = new PricePoint();                                
                                price.SouceApi = nameof(EntsoeAPI);

                                foreach (XmlNode timeIntervalNode in actualData.ChildNodes)
                                {
                                    if (timeIntervalNode.Name == "position")
                                    {
                                        int iPosition = int.Parse(timeIntervalNode.InnerText);

                                        if (iPosition > 1)
                                            price.Start = startTime.AddHours(iPosition - 1);
                                        else
                                            price.Start = startTime;

                                        price.End = startTime.AddHours(iPosition);
                                    }
                                    else if (timeIntervalNode.Name == "price.amount")
                                    {
                                        price.Price = Parse(timeIntervalNode.InnerText);
                                    }
                                }

                                var timeRange = price.End - price.Start;
                                if (price.Price != double.MinValue && timeRange.TotalHours <= 1)
                                {
                                    price.Id = ToGuid(price.Start.ToFileTime(), price.End.ToFileTime());
                                    _priceFetcher.PriceList.Add(price);
                                }
                                else
                                {
                                    _logger.LogWarning("Failed to get valid data from price endpoint");
                                }
                            }
                        }
                    }
                }
      
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check EntsoeAPI");
                return false;
            }
        }

        string GetPowerZoneName(PowerZoneName zone)
        {
            switch(zone)
            {
                case PowerZoneName.NO1:
                    return "NO-1";

                case PowerZoneName.NO2:
                    return "NO-2";

                case PowerZoneName.NO3:
                    return "NO-3";

                case PowerZoneName.NO4:
                    return "NO-4";

                case PowerZoneName.NO5:
                    return "NO-5";

                default:
                    throw new NotImplementedException(zone.ToString());
            }
        }

        public (DateTime start, DateTime end) GetStartAndEnd(string raw)
        {
            raw = raw.Replace("\"", "");
            var split = raw.Split(" - ");

            var start = DateTime.Parse(split[0], new ApiDateTimeFormat());
            var end = DateTime.Parse(split[1], new ApiDateTimeFormat());

            return (start, end);
        }

        static DateTime ParseDateTime(string strDateTime)
        {
            //foreach (TimeZoneInfo z in TimeZoneInfo.GetSystemTimeZones())
            //    Console.WriteLine(z.Id);
            var dateTime = DateTime.Parse(strDateTime, new ApiDateTimeFormat());
            TimeZoneInfo timeInfo = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            return TimeZoneInfo.ConvertTime(dateTime, timeInfo, TimeZoneInfo.Local);
        }
    }
}
