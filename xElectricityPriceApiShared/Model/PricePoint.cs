namespace xElectricityPriceApiShared.Model
{
    public class PricePoint : IEquatable<PricePoint>
    {
        public PricePoint()
        {
            Id = Guid.NewGuid();
            Price = 0;
            Start = DateTime.MinValue;
            End = DateTime.MinValue;
            SouceApi = String.Empty;
        }

        public Guid Id { get; set; }

        public double Price { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public bool Equals(PricePoint? other)
        {
            if (other == null)
                return false;

            return Id.Equals(other.Id);
        }

        public string SouceApi { get; set; }
    }
}
