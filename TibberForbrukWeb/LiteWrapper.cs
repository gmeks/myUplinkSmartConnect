using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace TibberForbrukWeb
{
    class LiteWrapper
    {
        static LiteDatabase _dbInstance;

        public static void Init()
        {
            _dbInstance = new LiteDatabase("MyData.db");

           // GenerateFakeData();
        }

        static void GenerateFakeData()
        {
            DateTime startTime = DateTime.Now.AddDays(-240);

            while(true)
            {
                if (startTime > DateTime.Now)
                    break;

                startTime = startTime.AddMinutes(1);

                var tmp = new RealtimePowerUsage()
                {
                    Watt = 4000,
                    Volt = 230,
                    Timestamp = startTime,
                };

                Add(tmp);
            }
        }

        public static void Add(RealtimePowerUsage newEntry)
        {
            if (_dbInstance == null)
            {
                Init();
            }

            var col = _dbInstance.GetCollection<RealtimePowerUsage>("RealtimePowerUsage");
            col.Insert(newEntry);
        }

        public static IEnumerable<RealtimePowerUsage> GetAll()
        {
            if (_dbInstance == null)
            {
                Init();
            }

            var col = _dbInstance.GetCollection<RealtimePowerUsage>("RealtimePowerUsage");
            return col.FindAll();
        }
    }
}
