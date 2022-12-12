using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xElectricityPriceApiShared.Currency
{
    internal class NorgesBank
    {
        HttpClient _client;

        public NorgesBank() 
        {
            //

            _client = new HttpClient();
        }

        public async Task<double> GetConversion()
        {
            var strCSV = await _client.GetStringAsync("https://data.norges-bank.no/api/data/EXR/B.EUR.NOK.SP?format=csv&lastNObservations=1&locale=en");
            var csvLines = strCSV.Split(';');

            var contentSplit = csvLines[csvLines.Length-1].Split(";");

            var strConversion = contentSplit[contentSplit.Length - 1];

            if (double.TryParse(strConversion, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            throw new NotSupportedException();
        }
    }
}
