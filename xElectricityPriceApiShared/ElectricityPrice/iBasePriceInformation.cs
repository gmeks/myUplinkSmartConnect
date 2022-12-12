using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xElectricityPriceApiShared.ElectricityPrice
{
    internal interface iBasePriceInformation
    {
        bool IsPriceInNOK { get; }

        bool IsPriceWithVAT { get; }

        bool IsPriceInKW { get; }

        Task<bool> GetPriceInformation();
    }
}
