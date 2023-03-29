using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace xElectricityPriceApiShared
{
    public static class JsonUtils
    {
        public static JsonSerializerOptions GetJsonSettings()
        {
            var serializeOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    new DoubleConverter()
                }
            };

            return serializeOptions;
        }

        public static T CloneTo<T>(object otherObj) where T : class
        {
            ReadOnlySpan<byte> objBytes = JsonSerializer.SerializeToUtf8Bytes(otherObj);
            if (objBytes == null)
                throw new NullReferenceException();

            var obj = JsonSerializer.Deserialize<T>(objBytes);
            if (obj == null)
                throw new NullReferenceException();

            return obj;
        }
    }

    public static class DateTimeExtensions
    {
        public static bool InRange(this DateTime dateToCheck, DateTime startDate, DateTime endDate)
        {
            return dateToCheck >= startDate && dateToCheck < endDate;
        }
    }
}
