using Hangfire;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using xElectricityPriceApi.Controllers;
using xElectricityPriceApi.Models;
using xElectricityPriceApiShared;
using xElectricityPriceApiShared.Model;

namespace xElectricityPriceApi.Services
{
    public class PriceService
    {
        readonly DatabaseContext _context;
        readonly ILogger<PriceService> _logger;

        public PriceService(DatabaseContext context, ILogger<PriceService> logger) 
        {
            _context = context;
            _logger = logger;
        }

        public void Add(PricePoint pricePoint)
        {
            var item = new PriceInformation()
            {
                Price = pricePoint.Price,
                End = pricePoint.End,
                Id = pricePoint.Id,
                SouceApi = pricePoint.SouceApi,
                Start = pricePoint.Start,
                StartHour = pricePoint.Start.Hour
            };

            var existingItem = _context.PriceInformation.FirstOrDefault(x => x.Id == item.Id);
            if (existingItem != null)
            {
                existingItem.Price = item.Price;
                existingItem.SouceApi = item.SouceApi;
                _context.SaveChanges();
            }
            else
            {
                Add(item);
            }            
        }

        public void Add(AveragePrice averagePrice)
        {
            var existingItem = _context.AveragePrice.FirstOrDefault(x => x.Id == averagePrice.Id);
            if (existingItem != null)
            {
                existingItem.Price = averagePrice.Price;
            }
            else
            {
                _context.AveragePrice.Add(averagePrice);
            }
                
            _context.SaveChanges();
        }

        public int AveragePriceCount
        {
            get
            {
                return _context.AveragePrice.Count();
            }
        }

        public void Add(PriceInformation pricePoint)
        {
            _context.PriceInformation.Add(pricePoint);

            _context.SaveChanges();
        }

        public AveragePrice GetAverageForMonth()
        {            
            var avr = _context.AveragePrice.FirstOrDefault(x => x.Point == DateTime.Now.Date);
            return avr;
        }

        public IEnumerable<PriceInformation> GetAllThisMonth()
        {
            var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month,1);
            var end = DateTime.Now.Date.AddDays(2).AddSeconds(-1);

            var priceList = Between(start,end);
            return priceList;
        }

        public IEnumerable<PriceInformation> Between(DateTime start,DateTime end)
        {
            var priceList = _context.PriceInformation.Where(x => x.Start >= start && x.End < end);
            return priceList;
        }

        public List<ExtendedPriceInformation> GetAllTodayAndTomorrow()
        {

            const double ElectricitySupportStart = 0.875d;
            const double ElectricitySupportPercentage = 0.90d;

            var start = DateTime.Now.Date;
            var end = start.AddDays(2).AddSeconds(-1);

            var tmpPriceList = Between(start, end).ToList();
            var avarage = GetAverageForMonth();
            if (tmpPriceList?.Any() != true)
            {
                _logger.LogWarning("No price information was gotten, we force a check.");
                RecurringJob.TriggerJob("Update prices");
                return Enumerable.Empty<ExtendedPriceInformation>().ToList();
            }

            if(avarage == null)
            {
                _logger.LogWarning("Avarage price not ready, forcing update");
                RecurringJob.TriggerJob("Update prices");
                return Enumerable.Empty<ExtendedPriceInformation>().ToList();
            }

            var priceList = JsonUtils.CloneTo<List<ExtendedPriceInformation>>(tmpPriceList);
            var electricitySupportPayBackPrKw = (avarage.Price - ElectricitySupportStart) * ElectricitySupportPercentage;

            foreach (var price in priceList)
            {
                price.PriceAfterSupport = price.Price - electricitySupportPayBackPrKw;
            }

            return priceList;
        }  
    }
}
