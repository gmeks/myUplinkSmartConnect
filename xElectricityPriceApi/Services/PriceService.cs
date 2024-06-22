using Hangfire;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Extensions;
using System;
using System.Linq;
using xElectricityPriceApi.BackgroundJobs;
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
            var currentMonth = DateTime.Now.Date;
            var avr = _context.AveragePrice.FirstOrDefault(x => x.Point == currentMonth);
            return avr;
        }

        public PriceInformation? GetCurrentPrice()
        {
            //SystemClock.Instance.GetCurrentInstant()
            var currentDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour,0,0);
            var price = _context.PriceInformation.Where(x => x.Start >= currentDate && currentDate < x.End).FirstOrDefault();

            if (price == null)
            {
                _logger.LogWarning("No price information was gotten, we force a check.");
                RecurringJob.TriggerJob(UpdatePrices.HangfireJobDescription);
                return null;
            }

            return price;
        }

        public IEnumerable<PriceInformation> GetAllThisMonth()
        {
            var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month,1);
            var end = DateTime.Now.Date.AddMonths(1).AddSeconds(-1); ;

            var priceList = Between(start,end);
            return priceList;
        }

        public IEnumerable<PriceInformation> Between(DateTime start, DateTime end)
        {
            var priceList = _context.PriceInformation.Where(x => x.Start >= start && x.End < end);
            return priceList;
        }

        public IQueryable<PriceInformation> GetAll(DateOnly date)
        {
            var start = date.ToDateTime(new TimeOnly(0,0,0));
            var end = date.ToDateTime(new TimeOnly(23, 59, 59));

            var priceList = _context.PriceInformation.Where(x => x.Start >= start && x.End < end);
            return priceList;
        }

        public List<ExtendedPriceInformation> GetAllTodayAndTomorrow()
        {
            var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = DateTime.Now.Date.AddDays(2).AddSeconds(-1);

            var tmpPriceList = Between(start,end).ToList();

            var avarage = GetAverageForMonth();
            if (tmpPriceList?.Any() != true)
            {
                _logger.LogWarning("No price information was gotten, we force a check.");
                RecurringJob.TriggerJob(UpdatePrices.HangfireJobDescription);
                return Enumerable.Empty<ExtendedPriceInformation>().ToList();
            }

            if(avarage == null)
            {
                _logger.LogWarning("Avarage price not ready, forcing update");
                RecurringJob.TriggerJob(UpdatePrices.HangfireJobDescription);
                return Enumerable.Empty<ExtendedPriceInformation>().ToList();
            }

            List<PriceInformation>? todayPrices = GetAll(DateOnly.FromDateTime(DateTime.Now)).OrderBy(x => x.Price).ToList();
            var TommorrowPrices = GetAll(DateOnly.FromDateTime(DateTime.Now.AddDays(1))).OrderBy(x => x.Price).ToList();

            var priceList = JsonUtils.CloneTo<List<ExtendedPriceInformation>>(tmpPriceList);

            foreach (var price in priceList)
            {
                price.PriceAfterSupport = GetEstimatedSupportPrKw(price.Price);
                

                if(todayPrices?.FirstOrDefault()?.Start.Date == price.Start.Date)
                    price.PriceDescription = GetPricePointDescriptionFromPriceList(price, todayPrices);

                if (TommorrowPrices?.FirstOrDefault()?.Start.Date == price.Start.Date)
                    price.PriceDescription = GetPricePointDescriptionFromPriceList(price, TommorrowPrices);
            }

            return priceList;
        }

        const int CheapHoursCount = 6;
        const int NormalHoursCount = 18;

        public PriceDescription GetPricePointDescriptionFromPriceList(ExtendedPriceInformation price, List<PriceInformation>? priceList)
        {
            if (price.Price <= 0.30) // Price bellow 30 øre is always considered cheap.
                return PriceDescription.Cheap;

            List<PriceInformation>? sortedPriceList = null;
            if (priceList?.Any() != true)
            {
                sortedPriceList = GetAll(DateOnly.FromDateTime(price.Start)).OrderBy(x => x.Price).ToList();
            }
            else
            {
                sortedPriceList = priceList.OrderBy(x => x.Price).ToList();
            }

            int index = sortedPriceList.IndexOf(price);

            if (index <= CheapHoursCount)
                return PriceDescription.Cheap;

            // All hours that are as cheap or the same as lowest 6 hours should all count as cheap.
            if (sortedPriceList[index].Price <= sortedPriceList[CheapHoursCount].Price)
                return PriceDescription.Cheap;

            if (index <= NormalHoursCount)
                return PriceDescription.Normal;

            return PriceDescription.Expensive;
        }

        public double GetEstimatedSupportPrKw(double price)
        {
            const double ElectricitySupportStart = 0.875d; // This is startpoint including mva
            const double ElectricitySupportPercentage = 0.90d;

            if(price >  ElectricitySupportStart)
            {
                var priceCoveredByState = (price - ElectricitySupportStart) * ElectricitySupportPercentage;
                return price - priceCoveredByState;
            }

            return price;
        }
    }
}
