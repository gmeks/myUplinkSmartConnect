using MyUplinkSmartConnect.Models;

namespace MyUplinkSmartConnect.ExternalPrice
{
    public class stPriceInformation : IEquatable<stPriceInformation>
    {
        public stPriceInformation()
        {
            Id = Guid.NewGuid();
            Price = 0;
            Start = DateTime.MinValue;
            End = DateTime.MinValue;
            DesiredPower = WaterHeaterDesiredPower.None;
        }

        public Guid Id { get; set; }

        public double Price { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public bool Equals(stPriceInformation? other)
        {
            return Id.Equals(other?.Id);
        }

        public WaterHeaterDesiredPower DesiredPower { get; set; }
    }
}
