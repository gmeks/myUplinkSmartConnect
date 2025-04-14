using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xElectricityPriceApiShared.Currency;
using xElectricityPriceApiShared.ElectricityPrice;
using xElectricityPriceApiShared.Model;

namespace xElectricityPriceApiShared
{
    public enum PowerZoneName
    {
        NO1,
        NO2,
        NO3, 
        NO4, 
        NO5
    }

    public class PriceFetcher
    {
        ILogger<object> _logger;

        public PriceFetcher(ILogger<object> logger,PowerZoneName powerZone) 
        {
            PowerZone = powerZone;
            _logger = logger;
        }

        public PowerZoneName PowerZone { get; set; }

        public bool OnlyEUPriceApi { get; set; }

        public List<PricePoint> PriceList { get; set; } = new List<PricePoint>();

        public async Task<bool> FetchHistoricPrices()
        {
            var priceFetchApi = new EntsoeAPI(this, _logger);
            var status = await priceFetchApi.GetPriceInformation(DateTime.Now.AddMonths(-2).Date, DateTime.Now);
            if(status)
            {
                await NormalizePrices(priceFetchApi);
            }

            return status;
        }

        public async Task<bool> UpdateRecentPrices()
        {
            iBasePriceInformation[] priceFetchApiList;

            if (OnlyEUPriceApi)
            {
                priceFetchApiList = new iBasePriceInformation[] { new HvaKosterStrommen(this, _logger)};
            }
            else
            {
#if DEBUG
                priceFetchApiList = new iBasePriceInformation[] { new HvaKosterStrommen(this, _logger) };
#else
                priceFetchApiList = new iBasePriceInformation[] {new HvaKosterStrommen(this, _logger) , new EntsoeAPI(this,_logger), new Nordpoolgroup(this, _logger), new VgApi(this, _logger) };
#endif
            }

            foreach (var priceListApi in priceFetchApiList)
            {
                var status = await priceListApi.GetPriceInformation();
                if (status && PriceList.Count >= 48)
                {
                    await NormalizePrices(priceListApi);

                    _logger.LogInformation("Using {priceApi} price list", priceListApi.GetType());
                    return true;
                }
            }

            _logger.LogWarning("Failed to get price information for today and tomorrow, will check for todays prices");
            foreach (var priceListApi in priceFetchApiList)
            {
                var status = await priceListApi.GetPriceInformation();

                if (status && PriceList.Count >= 24)
                {
                    await NormalizePrices(priceListApi);

                    _logger.LogDebug("Using {priceApi} price list for today", priceListApi.GetType());
                }
            }

            if(PriceList.Count != 24)
                _logger.LogWarning("Failed to price list from all known apis, will check schedule later.");

            return false;
        }       

        async Task NormalizePrices(iBasePriceInformation api)
        {
            double convertToNOK = 0d;
            if(!api.IsPriceInNOK)
            {
                convertToNOK = await ConvertPriceToNOK();
            }

            foreach (var price in PriceList)
            {
                if(!api.IsPriceInNOK)
                {
                    price.Price *= convertToNOK;
                }

                if (!api.IsPriceWithVAT)
                {
                    price.Price *= 1.25;
                }

                if (!api.IsPriceInKW)
                {
                    price.Price /= 1000;
                }
            }
        }

        async Task<double> ConvertPriceToNOK()
        {
            var norgeBank = new NorgesBank();
            var conversion = await norgeBank.GetConversion();
            return conversion;
        }
    }
}
