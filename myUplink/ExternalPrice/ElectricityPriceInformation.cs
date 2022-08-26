using MyUplinkSmartConnect.Models;

namespace MyUplinkSmartConnect.ExternalPrice
{
    public class ElectricityPriceInformation : IEquatable<ElectricityPriceInformation>
    {
        public ElectricityPriceInformation()
        {
            Id = Guid.NewGuid();
            Price = 0;
            Start = DateTime.MinValue;
            End = DateTime.MinValue;
            RecommendedHeatingPower = WaterHeaterDesiredPower.None;
        }

        public Guid Id { get; set; }

        public double Price { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public bool Equals(ElectricityPriceInformation? other)
        {
            return Id.Equals(other?.Id);
        }

        public WaterHeaterDesiredPower RecommendedHeatingPower { get; set; }
    }
}
