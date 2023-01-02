using MyUplinkSmartConnect.Models;
using MyUplinkSmartConnect.Services;
using xElectricityPriceApiShared.Model;

namespace MyUplinkSmartConnect.ExternalPrice
{
    public class PricePointWithPower : PricePoint,IEquatable<PricePointWithPower>
    {
        public PricePointWithPower()
        {
            Id = Guid.NewGuid();
            Price = 0;
            Start = DateTime.MinValue;
            End = DateTime.MinValue;
            HeatingMode =  HeatingMode.HeathingDisabled;
        }

        public bool Equals(PricePointWithPower? other)
        {
            return Id.Equals(other?.Id);
        }

        public HeatingMode HeatingMode { get; set; }

        public double GetMaximumCost(CurrentStateService state)
        {
            var currentKwh = state.ModeLookup.GetHeatingPowerInKwh(HeatingMode);

            return currentKwh > 0 ? (Price * currentKwh) : 0;
        }
    }
}
