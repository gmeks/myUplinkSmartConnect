using Hangfire;
using xElectricityPriceApi.Services;

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
            if (currentPrice == null)
            {
                _logger.LogDebug("Failed to get current price, will retry in a bit");

                RecurringJob.TriggerJob(UpdatePrices.HangfireJobDescription);
                RecurringJob.TriggerJob(SendPriceInformation.HangfireJobDescription);
                return;
            }

            await _mqTTSender.SendUpdate(MessageType.CurrentListPrice, currentPrice.Price, true);
        }
    }
}
