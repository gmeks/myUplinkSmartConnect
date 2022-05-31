using HashDepot;
using System.Text;

namespace DetermenisticRandom
{
    public abstract class CronStringBase
    {
        internal const byte MaxDayInMonth = 29;
        internal const byte MaxHoursInDay = 24;
        internal const byte MaxMinutesInHour = 60;
        internal const byte MaxSecondsInMinute = 60;
        internal ulong _HashingSeed;

        public CronStringBase(ulong hashingSeed)
        {
            if (hashingSeed != 0)
                _HashingSeed = hashingSeed;
            else
                _HashingSeed = 1123;
        }

        internal byte GetValue(in List<byte> hashList, ulong hash, int hashListIndex, byte value, int percentRange)
        {
            double dValue = value;

            double dPercentage = (double)((double)percentRange / (double)100);
            double dMaxValue = (double)(dValue * (dPercentage + 1));
            double dMinValue = (double)(dValue / (dPercentage + 1));

            return GetValueInRange(hashList, hash, hashListIndex, Convert.ToByte(dMinValue), Convert.ToByte(dMaxValue));
        }

        internal byte GetValueInRange(in List<byte> hashList, ulong hash, int hashListIndex, byte minValue, byte maxValue)
        {
            while (true)
            {
                for (int i = hashListIndex; i < hashList.Count; i++)
                {
                    byte value = GetHashedByte(hashList[i], hash, (ulong)hashListIndex, maxValue);

                    if (value >= minValue)
                        return value;
                }

                //We failed to get valid value, so we adust the hash.
                hash = AdjustHash(hash);
            }
            throw new Exception("Failed to get valid value.");
        }

        internal byte GetValue(in List<byte> hashList, ulong hash, int hashListIndex, byte maxValue)
        {
            byte hour = GetHashedByte(hashList[hashListIndex], hash, (ulong)hashListIndex, maxValue);
            return hour;
        }

        internal byte GetHashedByte(byte inputByte, ulong parrentHash, ulong index, ulong maxValue)
        {
            string strText = $"{inputByte}{parrentHash}{index}{maxValue}";

            var inputBytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(strText));
            var hash = XXHash.Hash64(inputBytes, _HashingSeed);

            return GetByteFromPercentage(hash, maxValue);
        }

        public (List<byte> hashArray, ulong hash) GetDeterministicHashCode(string str)
        {
            var hashArray = new List<byte>();
            var inputBytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(str));
            var hash = XXHash.Hash64(inputBytes, _HashingSeed);

            string strHash = hash.ToString();
            for (int i = 0; i < strHash.Length; i++)
            {
                var tmpByte = Byte.Parse(strHash[i].ToString());
                hashArray.Add(tmpByte);
            }

            return (hashArray, hash);
        }

        internal static byte GetByteFromPercentage(ulong hash, ulong maxValue)
        {
            decimal currentValue = hash;
            decimal maxPosibleValue = ulong.MaxValue;

            decimal percent = (currentValue / maxPosibleValue);// * 100;

            var tes = maxValue * percent;
            return (byte)tes;
        }

        internal static ulong AdjustHash(ulong hash)
        {
            ulong newHash;
            unchecked
            {
                newHash = hash * 3;
            }
            return newHash;
        }
    }
}