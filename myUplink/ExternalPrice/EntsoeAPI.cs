using MyUplinkSmartConnect.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MyUplinkSmartConnect.ExternalPrice
{
    internal class EntsoeAPI : BasePriceAPI, iBasePriceInformation
    {        
        //https://transparency.entsoe.eu/content/static_content/Static%20content/web%20api/Guide.html#_authentication_and_authorisation      

        public EntsoeAPI()
        {
            _client = new HttpClient();
        }       

        public async Task<bool> GetPriceInformation()
        {
            // i hate xml, and this function is a pure pain
            string startDateFormat = DateTime.Now.ToString("yyyyMMdd") + "0000";
            string endDateFormat = DateTime.Now.AddDays(1).ToString("yyyyMMdd") + "0000";

            try
            {
                _currentState.PriceList.Clear();

                var powerRegionIndex = GetPowerRegionIndex();

                string url = $"https://transparency.entsoe.eu/api?documentType=A44&in_Domain=10Y{NorwayPowerZones[powerRegionIndex]}--------T&out_Domain=10Y{NorwayPowerZones[powerRegionIndex]}--------T&periodStart={startDateFormat}&periodEnd={endDateFormat}&securityToken=5cd1c4f6-2172-4453-a8bb-c9467fa0fabc";
                var response = await _client.GetAsync(url);

                if(!response.IsSuccessStatusCode)
                {
                    Log.Logger.Warning("Failed request against entsoe API, with status code: {httpStatus}", response.StatusCode);
                    return false;
                }

                var xmlText = await response.Content.ReadAsStringAsync();

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlText);

                XmlNode? xmlNode = doc?.LastChild;
                if (xmlNode == null)
                {
                    Log.Logger.Warning("Failed to parse XML returned from entsoe");
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
                                var price = new ElectricityPriceInformation();

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

                                if (price.Price != double.MinValue)
                                {
                                    _currentState.PriceList.Add(price);
                                }
                            }
                        }
                    }
                }
                return true;

            }
            catch(Exception ex)
            {
                Log.Logger.Error(ex,"Failed to check EntsoeAPI");
                return false;
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
