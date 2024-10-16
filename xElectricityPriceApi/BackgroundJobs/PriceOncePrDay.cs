using Hangfire;
using xElectricityPriceApi.Models;
using xElectricityPriceApi.Services;
using xElectricityPriceApiShared;

namespace xElectricityPriceApi.BackgroundJobs
{
    public class PriceOncePrDay(MQTTSenderService mqttSender, PriceService priceService, ILogger<WorkOncePrHour> logger)
    {
        readonly MQTTSenderService _mqTTSender = mqttSender;
        readonly PriceService _priceService = priceService;
        readonly ILogger<WorkOncePrHour> _logger = logger;

        public const string HangfireJobDescription = "Hangfire daily price information";

        public async Task Work()
        {
            var currentPrice = _priceService.GetAll(DateOnly.FromDateTime(DateTime.Now)).OrderBy(x => x.Price).FirstOrDefault();

            if (currentPrice == null)
            {
                _logger.LogDebug("Failed to get current lowest price, will retry in a bit");

                RecurringJob.TriggerJob(UpdatePrices.HangfireJobDescription);
                RecurringJob.TriggerJob(PriceOncePrDay.HangfireJobDescription);
                return;
            }
            
            await _mqTTSender.SendUpdate(MessageType.CheapestHour, currentPrice.Start.ToShortTimeString(), true);
        }
    }
}
