using myUplink.ModelsPublic.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myUplink
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

        public void CreateSortedList(int desiredMaxpower,int mediumPower)
        {
            _priceList.Sort(new SortByLowestPrice());

            var maxPowerHours = _priceList.Take(desiredMaxpower);
            var mediumPowerHours = _priceList.Take(mediumPower + desiredMaxpower);

            for(int i=0;i<_priceList.Count;i++)
            {
                if(maxPowerHours.Contains(_priceList[i]))
                {
                    
                    _priceList[i].DesiredPower = WaterHeaterDesiredPower.Watt2000;
                }
                else if (mediumPowerHours.Contains(_priceList[i]))
                {
                    _priceList[i].DesiredPower = WaterHeaterDesiredPower.Watt700;
                }
            }

            _priceList.Sort(new SortByStartDate());

            foreach(var price in _priceList)
            {
                Console.WriteLine($"Start: {price.Start.ToShortTimeString()} | {price.End.ToShortTimeString()} - {price.DesiredPower} - {price.Price}");
            }
        }

        public async Task GetPrices()
        {
            var rawLines = System.IO.File.ReadAllLines("Day-ahead Prices_202203260000-202203270000.csv");
            for(int i = 1; i < rawLines.Length; i++)
            {
                var splitLine = rawLines[i].Split(',');
                var dates = GetStartAndEnd(splitLine[0]);


                var price = new stPriceInformation()
                {
                    Start = dates.start,
                    End = dates.end,
                    Price = Parse(splitLine[1])
                };
                _priceList.Add(price);
            }            
        }

        double Parse(string input)
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
            return Id.Equals(other.Id);
        }

        public WaterHeaterDesiredPower DesiredPower { get; set; }
    }

    class SortByLowestPrice : IComparer<stPriceInformation>
    {
        public int Compare(stPriceInformation x, stPriceInformation y)
        {
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
