using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tibber.Sdk;

namespace TibberForbrukWeb
{
    public class RealTimeMeasurementObserver : IObserver<RealTimeMeasurement>
    {
        static int _lastVolt;
        static int _lastAmp;
        static DateTime _lastMessage;

        public void OnCompleted() => Console.WriteLine("Real time measurement stream has been terminated. a");
        public void OnError(Exception error) => Console.WriteLine($"An error occured: {error}");
        public void OnNext(RealTimeMeasurement value)
        {

            if (value.VoltagePhase1 != null)
            {
                _lastVolt = Convert.ToInt32(value.VoltagePhase1);
            }

            if(value.CurrentPhase1 != null)
            {
                _lastAmp = Convert.ToInt32(value.CurrentPhase1);
            }

            var usage = new RealtimePowerUsage()
            {
                Timestamp = value.Timestamp.LocalDateTime,
                Volt = _lastVolt,
                Watt = Convert.ToInt32(value.Power),
                Amp = _lastAmp,
            };

            var lastMessageStored = DateTime.Now - _lastMessage;
            if (lastMessageStored.TotalSeconds > 60 && usage.Amp != 0 && usage.Volt!= 0)
            {
                _lastMessage = DateTime.Now;
                LiteWrapper.Add(usage);

                Console.WriteLine($"{value.Timestamp} - power: {usage.Watt}W | {usage.Amp} A | {usage.Volt} V");
            }            
        }
    }
}
