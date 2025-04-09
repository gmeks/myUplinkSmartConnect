using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace xElectricityPriceApiShared.Currency
{
    internal class NorgesBank
    {
        HttpClient _client;
        double _lastPrice;

        public NorgesBank() 
        {
            //

            _client = new HttpClient();
        }

        public async Task<double> GetConversion()
        {
            var strCSV = await _client.GetStringAsync("https://data.norges-bank.no/api/data/EXR/B.EUR.NOK.SP?format=sdmx-json&lastNObservations=1&locale=no");
            var apiData = JsonSerializer.Deserialize<Root>(strCSV); 
            if(apiData == null)
            {
                return _lastPrice;
            }

            var strConversion = apiData.data.dataSets.FirstOrDefault()?.series._0000.observations._0?.FirstOrDefault();
            if (double.TryParse(strConversion, NumberStyles.Any, CultureInfo.InvariantCulture, out double result) && result != 0)
            {
                _lastPrice = result;
            }

            return _lastPrice;
        }
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public class _0000
    {
        [JsonPropertyName("attributes")]
        public List<int> attributes { get; set; }

        [JsonPropertyName("observations")]
        public Observations observations { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("dataSets")]

        public List<DataSet> dataSets { get; set; }
    }

    public class DataSet
    {

        [JsonPropertyName("reportingBegin")]
        public DateTime reportingBegin { get; set; }

        [JsonPropertyName("reportingEnd")]
        public DateTime reportingEnd { get; set; }

        [JsonPropertyName("action")]
        public string action { get; set; }

        [JsonPropertyName("series")]
        public Series series { get; set; }
    }  
    public class Dimensions
    {
        [JsonPropertyName("dataset")]
        public List<object> dataset { get; set; }

        [JsonPropertyName("series")]
        public List<Series> series { get; set; }

        [JsonPropertyName("observation")]
        public List<Observation> observation { get; set; }
    }  

    public class Observation
    {
        [JsonPropertyName("id")]
        public string id { get; set; }

        [JsonPropertyName("name")]
        public string name { get; set; }

        [JsonPropertyName("description")]
        public string description { get; set; }

        [JsonPropertyName("keyPosition")]
        public int keyPosition { get; set; }

        [JsonPropertyName("role")]
        public string role { get; set; }


    }

    public class Observations
    {
        [JsonPropertyName("0")]
        public List<string> _0 { get; set; }
    }   
    public class Root
    {
        [JsonPropertyName("data")]
        public Data data { get; set; }
    }

    public class Sender
    {
        [JsonPropertyName("id")]
        public string id { get; set; }
    }

    public class Series
    {
        [JsonPropertyName("0:0:0:0")]
        public _0000 _0000 { get; set; }

        [JsonPropertyName("id")]
        public string id { get; set; }

        [JsonPropertyName("name")]
        public string name { get; set; }

        [JsonPropertyName("description")]
        public string description { get; set; }

        [JsonPropertyName("keyPosition")]
        public int keyPosition { get; set; }

        [JsonPropertyName("role")]
        public object role { get; set; }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
}
