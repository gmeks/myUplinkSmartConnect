using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect
{
    public static class JsonUtils
    {
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
}
