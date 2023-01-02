using xElectricityPriceApiShared.Model;

namespace xElectricityPriceApiShared.ElectricityPrice
{
    public class SortByStartDate : IComparer<IPricePoint>
    {
        public int Compare(IPricePoint? x, IPricePoint? y)
        {
            if (x == null)
                return 1;

            return x.Start.CompareTo(y?.Start);
        }
    }
}
