using Hangfire;
using xElectricityPriceApi.Models;
using xElectricityPriceApi.Services;
using xElectricityPriceApiShared;

namespace xElectricityPriceApi.BackgroundJobs
{
    public class SendPriceInformation
    {
        MQTTSenderService _mqTTSender;
        PriceService _priceService;
        ILogger<SendPriceInformation> _logger;

        public SendPriceInformation(MQTTSenderService mqttSender, PriceService priceService, ILogger<SendPriceInformation> logger) 
        {
            _mqTTSender = mqttSender;
            _priceService = priceService;
            _logger = logger;
        }

        public const string HangfireJobDescription = "Hangfire Send priceinfo";

        public async Task WorkOncePrHour()
        {
            var currentPrice = _priceService.GetCurrentPrice();
            var avg = _priceService.GetAverageForMonth();

            if (currentPrice == null || avg == null)
            {
                _logger.LogDebug("Failed to get current price, will retry in a bit");

                RecurringJob.TriggerJob(UpdatePrices.HangfireJobDescription);
                RecurringJob.TriggerJob(SendPriceInformation.HangfireJobDescription);
                return;
            }

            var price = JsonUtils.CloneTo<ExtendedPriceInformation>(currentPrice);
            price.PriceAfterSupport = price.Price - _priceService.GetEstimatedSupportPrKw(avg);
            price.PriceDescription = _priceService.GetPricePointDescriptionFromPriceList(price,null);

            await _mqTTSender.SendUpdate(MessageType.CurrentListPrice, price.Price, true);
            await _mqTTSender.SendUpdate(MessageType.CurrentPriceEstimatedEffectivePrice, price.PriceAfterSupport, true);
            await _mqTTSender.SendUpdate(MessageType.CurrentPriceDescription, price.PriceDescription.ToString(), true);
        }
    }
}