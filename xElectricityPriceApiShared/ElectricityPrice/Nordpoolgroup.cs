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
    internal class Nordpoolgroup : BasePriceAPI, iBasePriceInformation
    {
        public Nordpoolgroup(PriceFetcher priceFetcher, ILogger<object> logger) : base(priceFetcher, logger)
        {

        }

        public bool IsPriceInNOK { get { return true; } }

        public bool IsPriceWithVAT { get { return true; } }

        public bool IsPriceInKW { get { return true; } }

        public async Task<bool> GetPriceInformation()
        {
            _priceFetcher.PriceList.Clear();
            //TomorrowsPrice
            var tResponse = await _client.GetAsync("https://www.nordpoolgroup.com/api/marketdata/page/10?currency=,,,NOK").ConfigureAwait(true);
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var strContent = await tResponse.Content.ReadAsStringAsync();
                var root = JsonSerializer.Deserialize<Root>(strContent);
                if (root == null || root.data == null) throw new NullReferenceException();

                foreach (var item in root.data.Rows)
                {
                    foreach (var column in item.Columns)
                    {
                        var regionName = ConvertRegionName(column.Name, false);

                        if (regionName == _priceFetcher.PowerZone)
                        {
                            var price = new PricePoint();
                            price.Id = ToGuid(item.StartTime.ToFileTime(), item.EndTime.ToFileTime());
                            price.Start = item.StartTime;
                            price.End = item.EndTime;
                            price.Price = Parse(column.Value);
                            price.SouceApi = nameof(Nordpoolgroup);

                            if (!_priceFetcher.PriceList.Contains(price) && price.Price != double.MinValue)
                            {
                                _priceFetcher.PriceList.Add(price);
                            }
                        }
                    }
                }                
            }
            else
            {
                _logger.LogInformation($"Failed to fetch tomorrows price price information from Nordpool {tResponse.StatusCode}");
                _logger.LogDebug($"Nordpool content returned {tResponse.Content}");
                return false;
            }

            //Todays price.
            string strDate = DateTime.Now.ToString("dd-MM-yyyy");
            tResponse = await _client.GetAsync($"https://www.nordpoolgroup.com/api/marketdata/page/10?currency=,EUR,EUR,EUR&endDate={strDate}").ConfigureAwait(true);
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var strContent = await tResponse.Content.ReadAsStringAsync();
                var root = JsonSerializer.Deserialize<Root>(strContent);
                if (root == null || root.data == null) throw new NullReferenceException();

                foreach (var item in root.data.Rows)
                {
                    foreach (var column in item.Columns)
                    {
                        var regionName = ConvertRegionName(column.Name);

                        if (regionName == _priceFetcher.PowerZone)
                        {
                            var price = new PricePoint();
                            price.Id = ToGuid(item.StartTime.ToFileTime(), item.EndTime.ToFileTime());
                            price.Start = item.StartTime;
                            price.End = item.EndTime;
                            price.Price = Parse(column.Value);
                            price.SouceApi = nameof(Nordpoolgroup);

                            if (!_priceFetcher.PriceList.Contains(price) && price.Price != double.MinValue)
                            {
                                _priceFetcher.PriceList.Add(price);
                            }
                        }
                    }
                }
                return true;
            }
            else
            {
                _logger.LogInformation($"Failed to fetch todays price information from Nordpool {tResponse.StatusCode}");
                _logger.LogDebug($"Nordpool content returned {tResponse.Content}");
                return false;
            }            
        }

        public class Attribute
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? Role { get; set; }
            public bool HasRoles { get; set; }
            public IList<string>? Values { get; set; }
            public string? Value { get; set; }
        }

        public class Column
        {
            public int Index { get; set; }
            public int Scale { get; set; }
            //public object SecondaryValue { get; set; }
            public bool IsDominatingDirection { get; set; }
            public bool IsValid { get; set; }
            public bool IsAdditionalData { get; set; }
            public int Behavior { get; set; }
            public string Name { get; set; } = "";
            public string Value { get; set; } = "";
            public string GroupHeader { get; set; } = "";
            public bool DisplayNegativeValueInBlue { get; set; }
            public string CombinedName { get; set; } = "";
            public DateTime DateTimeForData { get; set; }
            public string DisplayName { get; set; } = "";
            public string DisplayNameOrDominatingDirection { get; set; } = "";
            public bool IsOfficial { get; set; }
            public bool UseDashDisplayStyle { get; set; }
        }

        public class Conf
        {
            //public string Id { get; set; } = "";
            //public object Name { get; set; }
            public DateTime Published { get; set; }
            public bool ShowGraph { get; set; }
            //public ResolutionPeriod ResolutionPeriod { get; set; }
            //public ResolutionPeriodY ResolutionPeriodY { get; set; }
            //public IList<Entity> Entities { get; set; }
            public int TableType { get; set; }
            //public IList<ExtraRow> ExtraRows { get; set; }
            //public IList<Filter> Filters { get; set; }
            public bool IsDrillDownEnabled { get; set; }
            public int DrillDownMode { get; set; }
            public bool IsMinValueEnabled { get; set; }
            public bool IsMaxValueEnabled { get; set; }
            public int ValidYearsBack { get; set; }
            //public string TimeScaleUnit { get; set; }
            public bool IsNtcEnabled { get; set; }
            //public NtcProductType NtcProductType { get; set; }
            //public string NtcHeader { get; set; }
            public int ShowTimelineGraph { get; set; }
            public int ExchangeMode { get; set; }
            public int IsPivotTable { get; set; }
            public int IsCombinedHeadersEnabled { get; set; }
            public int NtcFormat { get; set; }
            public bool DisplayHourAlsoInUKTime { get; set; }
        }

        public class Data
        {
            public IList<Row> Rows { get; set; } = Array.Empty<Row>();  
            public bool IsDivided { get; set; }
            ///public IList<string> SectionNames { get; set; }
            //public IList<string> EntityIDs { get; set; }
            public DateTime DataStartdate { get; set; }
            public DateTime DataEnddate { get; set; }
            public DateTime MinDateForTimeScale { get; set; }
            //public IList<object> AreaChanges { get; set; }
            //public IList<string> Units { get; set; }
            public DateTime LatestResultDate { get; set; }
            public bool ContainsPreliminaryValues { get; set; }
            public bool ContainsExchangeRates { get; set; }
            //public string ExchangeRateOfficial { get; set; }
            //public string ExchangeRatePreliminary { get; set; }
            //public object ExchangeUnit { get; set; }
            public DateTime DateUpdated { get; set; }
            public bool CombinedHeadersEnabled { get; set; }
            public int DataType { get; set; }
            public int TimeZoneInformation { get; set; }
        }

        public class DateRange
        {
            //public string Id { get; set; }
            public DateTime DateFrom { get; set; }
            public DateTime DateTo { get; set; }
            public bool IsNew { get; set; }
        }

        public class Entity
        {
            //public ProductType ProductType { get; set; }
            //public SecondaryProductType SecondaryProductType { get; set; }
            //public int SecondaryProductBehavior { get; set; }
            //public string Id { get; set; }
            //public string Name { get; set; }
            //public string GroupHeader { get; set; }
            //public DateTime DataUpdated { get; set; }
            //public IList<Attribute> Attributes { get; set; }
            //public bool Drillable { get; set; }
            //public IList<DateRange> DateRanges { get; set; }
            public int Index { get; set; }
            public int IndexForColumn { get; set; }
            public bool MinMaxDisabled { get; set; }
            public int DisableNumberGroupSeparator { get; set; }
            //public object TimeserieID { get; set; }
            //public string SecondaryTimeserieID { get; set; }
            public bool HasPreliminary { get; set; }
            //public object TimeseriePreliminaryID { get; set; }
            public int Scale { get; set; }
            public int SecondaryScale { get; set; }
            public int DataType { get; set; }
            public int SecondaryDataType { get; set; }
            public DateTime LastUpdate { get; set; }
            //public string Unit { get; set; }
            public bool IsDominatingDirection { get; set; }
            public bool DisplayAsSeparatedColumn { get; set; }
            public bool EnableInChart { get; set; }
            public bool BlueNegativeValues { get; set; }
        }

        public class ExtraRow
        {
            public string Id { get; set; } = "";
            public string Header { get; set; } = "";
            //public IList<string> ColumnProducts { get; set; }
        }

        public class Filter
        {
            public string Id { get; set; } = "";
            public string AttributeName { get; set; } = "";
            //public IList<string> Values { get; set; }
            public string DefaultValue { get; set; } = "";
        }

        public class Header
        {
            public string title { get; set; } = ""; 
            public string description { get; set; } = "";
            public string questionMarkInfo { get; set; } = "";
            public string hideDownloadButton { get; set; } = "";
        }

        public class NtcProductType
        {
            public string Id { get; set; } = "";
            public object Attributes { get; set; } = "";
            public string Name { get; set; } = "";
            public string DisplayName { get; set; } = "";
        }

        public class ProductType
        {
            public string Id { get; set; } = "";
            //public IList<Attribute> Attributes { get; set; } 
            public string Name { get; set; } = "";
            public string DisplayName { get; set; } = "";
        }

        public class ResolutionPeriod
        {
            public string Id { get; set; } = "";
            public int Resolution { get; set; }
            public int Unit { get; set; }
            public int PeriodNumber { get; set; }
        }

        public class ResolutionPeriodY
        {
            public string Id { get; set; } = "";
            public int Resolution { get; set; }
            public int Unit { get; set; }
            public int PeriodNumber { get; set; }
        }

        public class Root
        {
            public Data? data { get; set; }
            //public string cacheKey { get; set; }
            //public Conf conf { get; set; }
            //public Header header { get; set; }
            //public object endDate { get; set; }
            public string currency { get; set; } = "";
            public int pageId { get; set; }
        }

        public class Row
        {
            public IList<Column> Columns { get; set; } = Array.Empty<Column>(); 
            public string Name { get; set; } = "";
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public DateTime DateTimeForData { get; set; }
            public int DayNumber { get; set; }
            public DateTime StartTimeDate { get; set; }
            public bool IsExtraRow { get; set; }
            public bool IsNtcRow { get; set; }
            //public string EmptyValue { get; set; } = ""; 
            //public object Parent { get; set; }
        }

        public class SecondaryProductType
        {
            public string Id { get; set; } = "";
            //public object Attributes { get; set; }
            //public string Name { get; set; }
            //public string DisplayName { get; set; }
        }
    }
}
