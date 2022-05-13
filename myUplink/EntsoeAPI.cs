using MyUplinkSmartConnect.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MyUplinkSmartConnect
{
    internal class EntsoeAPI
    {
        //https://transparency.entsoe.eu/content/static_content/Static%20content/web%20api/Guide.html#_authentication_and_authorisation
        HttpClient _client;
        List<stPriceInformation> _priceList;      

        public EntsoeAPI()
        {
            _client = new HttpClient();
            _priceList = new List<stPriceInformation>();
        }

        public List<stPriceInformation> PriceList { get { return _priceList; } }

        public async Task<bool> FetchPriceInformation()
        {
            // i hate xml, and this function is a pure pain

            string startDateFormat = DateTime.Now.ToString("yyyyMMdd") + "0000";
            string endDateFormat = DateTime.Now.AddDays(1).ToString("yyyyMMdd") + "0000";

            string url = $"https://transparency.entsoe.eu/api?documentType=A44&in_Domain=10YNO-2--------T&out_Domain=10YNO-2--------T&periodStart={startDateFormat}&periodEnd={endDateFormat}&securityToken=5cd1c4f6-2172-4453-a8bb-c9467fa0fabc";

            var response = await _client.GetAsync(url);
            if(response.IsSuccessStatusCode)
            {
                var xmlText = await response.Content.ReadAsStringAsync();

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlText);

                XmlNode xmlNode = doc.LastChild;

                foreach (XmlNode child in xmlNode)
                {
                    if (child.Name != "TimeSeries")
                        continue;

                    DateTime startTime = DateTime.MinValue;
                    foreach(XmlNode xmlPeriod in child.ChildNodes)
                    {
                        if (xmlPeriod.Name != "Period")
                            continue;
                        

                        foreach(XmlNode actualData in xmlPeriod.ChildNodes)
                        {
                            if (actualData.Name == "timeInterval")
                            {
                                foreach(XmlNode timeIntervalNode in actualData.ChildNodes)
                                {
                                    if(timeIntervalNode.Name == "end")
                                    {                                        
                                        string strDate = timeIntervalNode.InnerText.Substring(0, timeIntervalNode.InnerText.IndexOf('T'));
                                        startTime = ParseDateTime(strDate);
                                    }
                                }
                            }
                            else if(actualData.Name == "Point")
                            {
                                var price = new stPriceInformation();

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

                                _priceList.Add(price);
                            }
                        }                       
                    }                                      
                }
                return true;
            }
            return false;
        }

        public void CreateSortedList(DateTime filterDate,int desiredMaxpower,int mediumPower)
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

            for(int i=0;i< sortedList.Count;i++)
            {
                if(maxPowerHours.Contains(sortedList[i]))
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
                if(updatedPrice != null)
                {
                    price.DesiredPower = updatedPrice.DesiredPower;
                }
            }
        }

        internal void PrintScheudule()
        {
            foreach (var price in _priceList)
            {
                 Console.WriteLine($"{price.Start.Day}) Start: {price.Start.ToShortTimeString()} | {price.End.ToShortTimeString()} - {price.DesiredPower} - {price.Price}");
            }
        }

        static double Parse(string input)
        {
            if (input == null || input.Length == 0)
                return 0;

            input = input.Replace("\"", "");

            for (int i=input.Length-1;i > 0;i--)
            {
                if(input[i] == ',')
                {
                    var tmpStr = input.ToArray();
                    tmpStr[i] = '.';

                    return double.Parse(tmpStr, NumberStyles.Any, CultureInfo.InvariantCulture);
                }
            }

            return double.Parse(input, NumberStyles.Any, CultureInfo.InvariantCulture); 
        }

        public (DateTime start,DateTime end) GetStartAndEnd(string raw)
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
            return TimeZoneInfo.ConvertTime(dateTime, timeInfo,TimeZoneInfo.Local);
        }
    }

    class ApiDateTimeFormat : IFormatProvider
    {
        public object? GetFormat(Type? formatType)
        {
            return "dd.MM.yyyy HH:mm:ss";
        }
    }

    public class stPriceInformation : IEquatable<stPriceInformation>
    {
        public stPriceInformation()
        {
            Id = Guid.NewGuid();
            Price = 0;
            Start = DateTime.MinValue;
            End = DateTime.MinValue;
            DesiredPower = WaterHeaterDesiredPower.None;
        }

        public Guid Id { get; set; }

        public double Price { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public bool Equals(stPriceInformation other)
        {
            return this.Id.Equals(other?.Id);
        }

        public WaterHeaterDesiredPower DesiredPower { get; set; }
    }

    class SortByLowestPrice : IComparer<stPriceInformation>
    {
        public int Compare(stPriceInformation x, stPriceInformation y)
        {
            if (x == null && y == null)
                return 0;

            if(x == null && y != null)
                return 1;

            if (y == null && x != null)
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
        public int Compare(stPriceInformation x, stPriceInformation y)
        {
            return x.Start.CompareTo(y.Start);
        }
    }
}
