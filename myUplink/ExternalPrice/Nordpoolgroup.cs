using RestSharp;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect.ExternalPrice
{
    internal class Nordpoolgroup : BasePriceAPI, iBasePriceInformation
    {
        public Nordpoolgroup()
        {
            
        }
        public async Task<bool> GetPriceInformation()
        {
            _currentState.PriceList.Clear();
            var powerRegionIndex = GetPowerRegionIndex();

            //TomorrowsPrice
            var tResponse = await _client.GetAsync("https://www.nordpoolgroup.com/api/marketdata/page/10?currency=,,,EUR").ConfigureAwait(true);
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK )
            {
                var strContent = await tResponse.Content.ReadAsStringAsync();
                var root = JsonSerializer.Deserialize<Root>(strContent) ?? throw new NullReferenceException();

                foreach(var item in root.data.Rows)
                {
                    foreach(var column in item.Columns)
                    {
                        var regionName = ConvertRegionName(column.Name,false);

                        if (regionName.Equals(NorwayPowerZones[powerRegionIndex], StringComparison.OrdinalIgnoreCase))
                        {
                            var price = new ElectricityPriceInformation();
                            price.Id = ToGuid(item.StartTime.ToFileTime(), item.EndTime.ToFileTime());
                            price.Start = item.StartTime;
                            price.End = item.EndTime;
                            price.Price = Parse(column.Value);

                            if (!_currentState.PriceList.Contains(price) && price.Price != double.MinValue)
                            {
                                _currentState.PriceList.Add(price);
                            }
                        }
                    }
                }
            }
            else
            {
                Log.Logger.Information($"Login to myUplink API failed with status {tResponse.StatusCode} and message {tResponse.Content}");
                return false;
            }

            //Todays price.
            string strDate = DateTime.Now.ToString("dd-MM-yyyy"); 
            tResponse = await _client.GetAsync($"https://www.nordpoolgroup.com/api/marketdata/page/10?currency=,EUR,EUR,EUR&endDate={strDate}").ConfigureAwait(true);
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var strContent = await tResponse.Content.ReadAsStringAsync();
                var root = JsonSerializer.Deserialize<Root>(strContent) ?? throw new NullReferenceException();

                foreach (var item in root.data.Rows)
                {
                    foreach (var column in item.Columns)
                    {
                        var regionName = ConvertRegionName(column.Name);

                        if (regionName.Equals(NorwayPowerZones[powerRegionIndex], StringComparison.OrdinalIgnoreCase))
                        {
                            var price = new ElectricityPriceInformation();
                            price.Id = ToGuid(item.StartTime.ToFileTime(), item.EndTime.ToFileTime());
                            price.Start = item.StartTime;
                            price.End = item.EndTime;
                            price.Price = Parse(column.Value);

                            if(!_currentState.PriceList.Contains(price) && price.Price != double.MinValue)
                            {
                                _currentState.PriceList.Add(price);
                            }
                        }
                    }
                }

                return true;
            }
            else
            {
                Log.Logger.Information($"Login to myUplink API failed with status {tResponse.StatusCode} and message {tResponse.Content}");
                return false;
            }
        }

        public static Guid ToGuid(long startTime,long endtime)
        {
            byte[] guidData = new byte[16];
            Array.Copy(BitConverter.GetBytes(startTime), guidData, 8);
            Array.Copy(BitConverter.GetBytes(endtime),0, guidData,8,8);

            return new Guid(guidData);
        }

        public class Attribute
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? Role { get; set; }
            public bool HasRoles { get; set; }
            public List<string>? Values { get; set; }
            public string? Value { get; set; }
        }

        public class Column
        {
            public int Index { get; set; }
            public int Scale { get; set; }
            public object SecondaryValue { get; set; }
            public bool IsDominatingDirection { get; set; }
            public bool IsValid { get; set; }
            public bool IsAdditionalData { get; set; }
            public int Behavior { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
            public string GroupHeader { get; set; }
            public bool DisplayNegativeValueInBlue { get; set; }
            public string CombinedName { get; set; }
            public DateTime DateTimeForData { get; set; }
            public string DisplayName { get; set; }
            public string DisplayNameOrDominatingDirection { get; set; }
            public bool IsOfficial { get; set; }
            public bool UseDashDisplayStyle { get; set; }
        }

        public class Conf
        {
            public string Id { get; set; }
            public object Name { get; set; }
            public DateTime Published { get; set; }
            public bool ShowGraph { get; set; }
            public ResolutionPeriod ResolutionPeriod { get; set; }
            public ResolutionPeriodY ResolutionPeriodY { get; set; }
            public List<Entity> Entities { get; set; }
            public int TableType { get; set; }
            public List<ExtraRow> ExtraRows { get; set; }
            public List<Filter> Filters { get; set; }
            public bool IsDrillDownEnabled { get; set; }
            public int DrillDownMode { get; set; }
            public bool IsMinValueEnabled { get; set; }
            public bool IsMaxValueEnabled { get; set; }
            public int ValidYearsBack { get; set; }
            public string TimeScaleUnit { get; set; }
            public bool IsNtcEnabled { get; set; }
            public NtcProductType NtcProductType { get; set; }
            public string NtcHeader { get; set; }
            public int ShowTimelineGraph { get; set; }
            public int ExchangeMode { get; set; }
            public int IsPivotTable { get; set; }
            public int IsCombinedHeadersEnabled { get; set; }
            public int NtcFormat { get; set; }
            public bool DisplayHourAlsoInUKTime { get; set; }
        }

        public class Data
        {
            public List<Row> Rows { get; set; }
            public bool IsDivided { get; set; }
            public List<string> SectionNames { get; set; }
            public List<string> EntityIDs { get; set; }
            public DateTime DataStartdate { get; set; }
            public DateTime DataEnddate { get; set; }
            public DateTime MinDateForTimeScale { get; set; }
            public List<object> AreaChanges { get; set; }
            public List<string> Units { get; set; }
            public DateTime LatestResultDate { get; set; }
            public bool ContainsPreliminaryValues { get; set; }
            public bool ContainsExchangeRates { get; set; }
            public string ExchangeRateOfficial { get; set; }
            public string ExchangeRatePreliminary { get; set; }
            public object ExchangeUnit { get; set; }
            public DateTime DateUpdated { get; set; }
            public bool CombinedHeadersEnabled { get; set; }
            public int DataType { get; set; }
            public int TimeZoneInformation { get; set; }
        }

        public class DateRange
        {
            public string Id { get; set; }
            public DateTime DateFrom { get; set; }
            public DateTime DateTo { get; set; }
            public bool IsNew { get; set; }
        }

        public class Entity
        {
            public ProductType ProductType { get; set; }
            public SecondaryProductType SecondaryProductType { get; set; }
            public int SecondaryProductBehavior { get; set; }
            public string Id { get; set; }
            public string Name { get; set; }
            public string GroupHeader { get; set; }
            public DateTime DataUpdated { get; set; }
            public List<Attribute> Attributes { get; set; }
            public bool Drillable { get; set; }
            public List<DateRange> DateRanges { get; set; }
            public int Index { get; set; }
            public int IndexForColumn { get; set; }
            public bool MinMaxDisabled { get; set; }
            public int DisableNumberGroupSeparator { get; set; }
            public object TimeserieID { get; set; }
            public string SecondaryTimeserieID { get; set; }
            public bool HasPreliminary { get; set; }
            public object TimeseriePreliminaryID { get; set; }
            public int Scale { get; set; }
            public int SecondaryScale { get; set; }
            public int DataType { get; set; }
            public int SecondaryDataType { get; set; }
            public DateTime LastUpdate { get; set; }
            public string Unit { get; set; }
            public bool IsDominatingDirection { get; set; }
            public bool DisplayAsSeparatedColumn { get; set; }
            public bool EnableInChart { get; set; }
            public bool BlueNegativeValues { get; set; }
        }

        public class ExtraRow
        {
            public string Id { get; set; }
            public string Header { get; set; }
            public List<string> ColumnProducts { get; set; }
        }

        public class Filter
        {
            public string Id { get; set; }
            public string AttributeName { get; set; }
            public List<string> Values { get; set; }
            public string DefaultValue { get; set; }
        }

        public class Header
        {
            public string title { get; set; }
            public string description { get; set; }
            public string questionMarkInfo { get; set; }
            public string hideDownloadButton { get; set; }
        }

        public class NtcProductType
        {
            public string Id { get; set; }
            public object Attributes { get; set; }
            public string Name { get; set; }
            public string DisplayName { get; set; }
        }

        public class ProductType
        {
            public string Id { get; set; }
            public List<Attribute> Attributes { get; set; }
            public string Name { get; set; }
            public string DisplayName { get; set; }
        }

        public class ResolutionPeriod
        {
            public string Id { get; set; }
            public int Resolution { get; set; }
            public int Unit { get; set; }
            public int PeriodNumber { get; set; }
        }

        public class ResolutionPeriodY
        {
            public string Id { get; set; }
            public int Resolution { get; set; }
            public int Unit { get; set; }
            public int PeriodNumber { get; set; }
        }

        public class Root
        {
            public Data data { get; set; }
            public string cacheKey { get; set; }
            public Conf conf { get; set; }
            public Header header { get; set; }
            public object endDate { get; set; }
            public string currency { get; set; }
            public int pageId { get; set; }
        }

        public class Row
        {
            public List<Column> Columns { get; set; }
            public string Name { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public DateTime DateTimeForData { get; set; }
            public int DayNumber { get; set; }
            public DateTime StartTimeDate { get; set; }
            public bool IsExtraRow { get; set; }
            public bool IsNtcRow { get; set; }
            public string EmptyValue { get; set; }
            public object Parent { get; set; }
        }

        public class SecondaryProductType
        {
            public string Id { get; set; }
            public object Attributes { get; set; }
            public string Name { get; set; }
            public string DisplayName { get; set; }
        }
    }
}
