using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace myUplink
{
    //https://github.com/dagsunde/AMS_2/blob/master/AMS_2/DayAheadPrice.cs

    public class NordpoolDayAheadPrice
    {

        private const string MIN = "Min";
        private const string MAX = "Max";
        private const string AVERAGE = "Average";
        private const string PEAK = "Peak";
        private const string OFF_PEAK_1 = "Off-peak 1";
        private const string OFF_PEAK_2 = "Off-peak 2";

        static HttpClient client = new HttpClient();

        private Data data = null;
        private static List<NordpoolDayAheadPrice> priceList = new List<NordpoolDayAheadPrice>();
        #region Public Attributes
        public int DayAheadPriceId { get; set; }
        public DateTime ValidDate { get; set; }
        public String GridArea { get; set; }
        public Decimal Hour1 { get; set; }
        public Decimal Hour2 { get; set; }
        public Decimal Hour3 { get; set; }
        public Decimal Hour4 { get; set; }
        public Decimal Hour5 { get; set; }
        public Decimal Hour6 { get; set; }
        public Decimal Hour7 { get; set; }
        public Decimal Hour8 { get; set; }
        public Decimal Hour9 { get; set; }
        public Decimal Hour10 { get; set; }
        public Decimal Hour11 { get; set; }
        public Decimal Hour12 { get; set; }
        public Decimal Hour13 { get; set; }
        public Decimal Hour14 { get; set; }
        public Decimal Hour15 { get; set; }
        public Decimal Hour16 { get; set; }
        public Decimal Hour17 { get; set; }
        public Decimal Hour18 { get; set; }
        public Decimal Hour19 { get; set; }
        public Decimal Hour20 { get; set; }
        public Decimal Hour21 { get; set; }
        public Decimal Hour22 { get; set; }
        public Decimal Hour23 { get; set; }
        public Decimal Hour24 { get; set; }
        public Decimal Min { get; set; }
        public Decimal Max { get; set; }
        public Decimal Average { get; set; }
        public Decimal Peak { get; set; }
        public Decimal OffPeak1 { get; set; }
        public Decimal OffPeak2 { get; set; }
        #endregion



        public void GetDayAhead()
        {

            client.BaseAddress = new Uri("http://www.nordpoolspot.com/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = client.GetAsync("api/marketdata/page/10/NOK").Result;
            if (response.IsSuccessStatusCode)
            {
                var tmpStr = response.Content.ReadAsStringAsync().Result;
                var wrap =JsonSerializer.Deserialize<Wrapper>(tmpStr);
                data = wrap.data;
            }

            Dictionary<string, NordpoolDayAheadPrice> dayAhead = new Dictionary<string, NordpoolDayAheadPrice>();

            foreach (var row in data.Rows)
            {

                foreach (var col in row.Columns)
                {
                    if (!dayAhead.ContainsKey(col.Name))
                    {
                        dayAhead.Add(col.Name, new NordpoolDayAheadPrice());
                        dayAhead[col.Name].GridArea = col.Name;
                        dayAhead[col.Name].ValidDate = data.DataStartdate;
                    }
                    if (!row.IsExtraRow)
                    {
                        switch (row.StartTime.Hour)
                        {
                            case 0:
                                dayAhead[col.Name].Hour1 = Convert.ToDecimal(col.Value);
                                break;
                            case 1:
                                dayAhead[col.Name].Hour2 = Convert.ToDecimal(col.Value);
                                break;
                            case 2:
                                dayAhead[col.Name].Hour3 = Convert.ToDecimal(col.Value);
                                break;
                            case 3:
                                dayAhead[col.Name].Hour4 = Convert.ToDecimal(col.Value);
                                break;
                            case 4:
                                dayAhead[col.Name].Hour5 = Convert.ToDecimal(col.Value);
                                break;
                            case 5:
                                dayAhead[col.Name].Hour6 = Convert.ToDecimal(col.Value);
                                break;
                            case 6:
                                dayAhead[col.Name].Hour7 = Convert.ToDecimal(col.Value);
                                break;
                            case 7:
                                dayAhead[col.Name].Hour8 = Convert.ToDecimal(col.Value);
                                break;
                            case 8:
                                dayAhead[col.Name].Hour9 = Convert.ToDecimal(col.Value);
                                break;
                            case 9:
                                dayAhead[col.Name].Hour10 = Convert.ToDecimal(col.Value);
                                break;
                            case 10:
                                dayAhead[col.Name].Hour11 = Convert.ToDecimal(col.Value);
                                break;
                            case 11:
                                dayAhead[col.Name].Hour12 = Convert.ToDecimal(col.Value);
                                break;
                            case 12:
                                dayAhead[col.Name].Hour13 = Convert.ToDecimal(col.Value);
                                break;
                            case 13:
                                dayAhead[col.Name].Hour14 = Convert.ToDecimal(col.Value);
                                break;
                            case 14:
                                dayAhead[col.Name].Hour15 = Convert.ToDecimal(col.Value);
                                break;
                            case 15:
                                dayAhead[col.Name].Hour16 = Convert.ToDecimal(col.Value);
                                break;
                            case 16:
                                dayAhead[col.Name].Hour17 = Convert.ToDecimal(col.Value);
                                break;
                            case 17:
                                dayAhead[col.Name].Hour18 = Convert.ToDecimal(col.Value);
                                break;
                            case 18:
                                dayAhead[col.Name].Hour19 = Convert.ToDecimal(col.Value);
                                break;
                            case 19:
                                dayAhead[col.Name].Hour20 = Convert.ToDecimal(col.Value);
                                break;
                            case 20:
                                dayAhead[col.Name].Hour21 = Convert.ToDecimal(col.Value);
                                break;
                            case 21:
                                dayAhead[col.Name].Hour22 = Convert.ToDecimal(col.Value);
                                break;
                            case 22:
                                dayAhead[col.Name].Hour23 = Convert.ToDecimal(col.Value);
                                break;
                            case 23:
                                dayAhead[col.Name].Hour24 = Convert.ToDecimal(col.Value);
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        switch (row.Name)
                        {
                            case "Min":
                                dayAhead[col.Name].Min = Convert.ToDecimal(col.Value);
                                break;
                            case "Max":
                                dayAhead[col.Name].Max = Convert.ToDecimal(col.Value);
                                break;
                            case "Average":
                                dayAhead[col.Name].Average = Convert.ToDecimal(col.Value);
                                break;
                            case "Peak":
                                dayAhead[col.Name].Peak = Convert.ToDecimal(col.Value);
                                break;
                            case "Off-peak 1":
                                dayAhead[col.Name].OffPeak1 = Convert.ToDecimal(col.Value);
                                break;
                            case "Off-peak 2":
                                dayAhead[col.Name].OffPeak2 = Convert.ToDecimal(col.Value);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            foreach (var key in dayAhead.Keys)
            {
                priceList.Add(dayAhead[key]);
            }
        }
    }

    public class Column
    {
        public string Name { get; set; }
        public string Value { get; set; }

    }

    public class Row
    {
        public string Name { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsExtraRow { get; set; }
        public List<Column> Columns { get; set; }

    }

    public class Data
    {

        public DateTime DataStartdate { get; set; }
        public DateTime DataEnddate { get; set; }
        public List<Row> Rows { get; set; }

    }

    public class Wrapper
    {
        public Data data { get; set; }
    }
}
