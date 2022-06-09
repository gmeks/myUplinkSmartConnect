using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect.ExternalPrice
{
    internal interface iBasePriceInformation
    {
        void CreateSortedList(DateTime filterDate, int desiredMaxpower, int mediumPower);

        void PrintScheudule();

        List<stPriceInformation> PriceList { get; }
    }
}
