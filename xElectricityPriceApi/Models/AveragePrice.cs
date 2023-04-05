
namespace xElectricityPriceApi.Models
{
    public class AveragePrice
    {
        public Guid Id { get; set; }

        public double Price { get; set; }

        public DateTime Point { get; set; }
    }
}
