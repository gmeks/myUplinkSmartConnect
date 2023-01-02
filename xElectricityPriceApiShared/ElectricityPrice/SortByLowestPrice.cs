using xElectricityPriceApiShared.Model;

namespace xElectricityPriceApiShared.ElectricityPrice
{
    public class SortByLowestPrice : IComparer<IPricePoint>
    {
        public int Compare(IPricePoint? x, IPricePoint? y)
        {
            if (x == null && y == null)
                return 0;

            if (x == null)
                return 1;

            if (y == null)
                return -1;

            if (x.Price == y.Price)
                return 0;

            if (x.Price < y.Price)
                return -1;

            return 1;
        }
    }
}
