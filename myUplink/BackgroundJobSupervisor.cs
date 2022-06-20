using DetermenisticRandom;
using MyUplinkSmartConnect.ExternalPrice;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect
{
    internal class BackgroundJobSupervisor
    {
        Thread _thread;
        bool _killWorker;

        DateTime _nextStatusUpdate;
        DateTime _nextScheduleUpdate;

        JobCheckHeaterStatus? _heaterStatus;

        public BackgroundJobSupervisor()
        {
            _thread = new Thread(Worker);
            _thread.IsBackground = true;
            _thread.Name = "BackgroundJobs";

            var random = new DetermenisticInt();
            int tmpHour = random.GetByte(13, 22, Settings.Instance.UserName, 3);
            int tmpMinute = random.GetByte(13, 22, Settings.Instance.UserName, 2);

            _nextScheduleUpdate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, tmpHour,tmpMinute,0);
            _nextStatusUpdate = DateTime.Now;

            _nextScheduleUpdate = _nextScheduleUpdate.AddDays(-1);
            Log.Logger.Information("Target Schedule change time is {NextScheduleUpdate}",_nextScheduleUpdate.ToLocalTime().ToShortTimeString());
        }

        public void Start()
        {
            _killWorker = false;
            if (!_thread.IsAlive)
            {
                _thread.Start();
            }
        }

        public void Stop()
        {
            _killWorker = true;
        }

        async void Worker()
        {
            while(!_killWorker)
            {
                var nextStatusUpdate = DateTime.Now - _nextStatusUpdate;
                if (_heaterStatus == null)
                    _heaterStatus = new JobCheckHeaterStatus();

                if (nextStatusUpdate.TotalMinutes > Settings.Instance.CheckRemoteStatsIntervalInMinutes)
                {
                    try
                    {
                        await _heaterStatus.Work();
                        _nextStatusUpdate = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex,"Failed to run heater status job.");
                        _heaterStatus = null;
                    }
                }

                var nowUTC = DateTime.Now;
                var nextScheduleChange = nowUTC - _nextScheduleUpdate;
#if DEBUG
                if (true)
#else
                if (nextScheduleChange.TotalHours > 23 && nowUTC.Hour > 15)
#endif
                {
                    try
                    {
                        await JobReScheuleheating.Work();
                        _nextScheduleUpdate = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, "Failed to run heater status job.");
                    }
                }
                else
                {
                    if(_heaterStatus != null && Settings.Instance.myuplinkApi != null)
                    {
                        var group = await Settings.Instance.myuplinkApi.GetDevices();
                        if (group != null)
                        {
                            foreach (var device in group)
                            {
                                if (device.devices == null || string.IsNullOrEmpty(device.name))
                                    throw new NullReferenceException("device.devices or device.name cannot be null");

                                foreach (var tmpdevice in device.devices)
                                {
                                    await _heaterStatus.SendUpdate(device.name, Models.CurrentPointParameterType.LastScheduleChangeInHours, Convert.ToInt32(nextScheduleChange.TotalHours));
                                }
                            }
                        }
                    }                                   
                }

                Thread.Sleep(TimeSpan.FromMinutes(1));
            }
        }
    }
}