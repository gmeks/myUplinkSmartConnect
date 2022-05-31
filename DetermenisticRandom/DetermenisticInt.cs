using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetermenisticRandom
{
    public class DetermenisticInt : CronStringBase
    {
        public DetermenisticInt(ulong hashingSeed = 0) : base(hashingSeed)
        {

        }

        public byte GetByte(byte minimum,byte maximum, string seed, int hashIndex = 1)
        {
            var hashArray = GetDeterministicHashCode(seed);

            var day = GetValueInRange(hashArray.hashArray, hashArray.hash, hashIndex, minimum, maximum);
            return day;
        }
    }
}
