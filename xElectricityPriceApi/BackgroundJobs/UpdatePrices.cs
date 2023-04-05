using xElectricityPriceApi.Controllers;
using xElectricityPriceApi.Services;
using xElectricityPriceApiShared;

namespace xElectricityPriceApi.BackgroundJobs
{
    public class UpdatePrices
    {
        private readonly PriceFetcher _priceFetcher;
        private readonly PriceService _priceService;
        private readonly ILogger<UpdatePrices> _logger;

        public UpdatePrices(PriceService priceService ,ILogger<UpdatePrices> logger)
        {
            _priceFetcher = new PriceFetcher(logger, PowerZoneName.NO2);
            _priceService = priceService;
            _logger = logger;
        }

        public async Task Work()
        {
            _logger.LogInformation("Updating price information");
            await _priceFetcher.UpdateRecentPrices();
            foreach(var price in _priceFetcher.PriceList)
            {
                _priceService.Add(price);
            }
            await UpdateAvaragePrices();
        }

        async Task UpdateAvaragePrices()
        {
            var thisMonthPrices = _priceService.GetAllThisMonth();
            int excpectedPricePoints = (DateTime.Now.Day * 24);

            if (thisMonthPrices.Count() < excpectedPricePoints)
            {
                _logger.LogInformation("Fetching historical price information");
                await _priceFetcher.FetchHistoricPrices();

                foreach (var price in _priceFetcher.PriceList)
                {
                    _priceService.Add(price);
                }
                thisMonthPrices = _priceService.GetAllThisMonth();
            }

            var avarage = _priceService.GetAverageForMonth();
            if(avarage == null)
            {
                avarage = new Models.AveragePrice();
                avarage.Id = ToGuid(DateTime.Now.Date);
                avarage.Point = DateTime.Now.Date;
            }

            var avaragePrice = thisMonthPrices.Average(x => x.Price);
            avarage.Price = avaragePrice;

            _priceService.Add(avarage);
        }


        internal static Guid ToGuid(DateTime seed)
        {
            var seedTime = seed.ToFileTime();

            byte[] guidData = new byte[16];
            Array.Copy(BitConverter.GetBytes(seedTime), guidData, 8);
            Array.Copy(BitConverter.GetBytes(seedTime), 0, guidData, 8, 8);

            return new Guid(guidData);
        }
    }
}
