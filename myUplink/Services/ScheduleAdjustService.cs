using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect.Services
{
    public class ScheduleAdjustService
    {
        readonly List<ScheduleBoost> _scheduledBoosts;

        public ScheduleAdjustService()
        {
            _scheduledBoosts = new List<ScheduleBoost>();
        }

        public void Add(int maximumHours = 2)
        {
            var now = DateTime.Now;
            
            var adjustStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);


            _scheduledBoosts.Add(new ScheduleBoost()
            {
                 Start = adjustStart,
                 End = adjustStart.AddHours(maximumHours),
            });
        }

        public bool IsBoostScheduled(DateTime time) 
        {
            if (!_scheduledBoosts.Any())
                return false;

            bool foundMatchingSlot = false;
            int itemToRemove =  -1;

            for(int i=0;i< _scheduledBoosts.Count;i++)
            {
                if(time.InRange(_scheduledBoosts[i].Start,_scheduledBoosts[i].End))
                {
                    foundMatchingSlot = true;
                    break;
                }

                if (_scheduledBoosts[i].End < DateTime.Now)
                {
                    itemToRemove = i;
                    continue;
                }
            }

            if(itemToRemove != -1)
            {
                _scheduledBoosts.RemoveAt(itemToRemove);
            }

            return foundMatchingSlot;
        }
    }

    public class ScheduleBoost
    {
        public DateTime Start { get; set; }

        public DateTime End { get; set; }
    }
}
