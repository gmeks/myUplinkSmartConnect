using xElectricityPriceApiShared.Model;

namespace xElectricityPriceApi.Models
{
    public class PriceInformation : PricePoint
    {
        public new Guid Id { get; set; }
        public int StartHour { get; set; }
    }

    public class ExtendedPriceInformation : PriceInformation
    {
        public double PriceAfterSupport { get; set; }
    }
}
