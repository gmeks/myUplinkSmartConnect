using System;
using System.Buffers.Text;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Globalization;

namespace xElectricityPriceApiShared
{
    class DoubleConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if(reader.TryGetDouble(out double value))
                return value;

            var strDouble = Encoding.UTF8.GetString(reader.ValueSpan);
            if (string.IsNullOrEmpty(strDouble))
                return 0;

            for (int i = strDouble.Length - 1; i > 0; i--)
            {
                if (strDouble[i] == ',')
                {
                    var tmpStr = strDouble.ToArray();
                    tmpStr[i] = '.';

                    return double.Parse(tmpStr, NumberStyles.Any, CultureInfo.InvariantCulture);
                }
            }

            return double.Parse(strDouble, NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        static StandardFormat f = StandardFormat.Parse("F");
        static StandardFormat g = StandardFormat.Parse("G");

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            Span<byte> buffer = stackalloc byte[30];
            var format = value % 1 == 0 ? f : g;
            Utf8Formatter.TryFormat(value, buffer, out var written, format);
            writer.WriteRawValue(buffer[..written]);
        }
    }
}
