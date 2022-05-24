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

            _nextStatusUpdate = DateTime.Now;
            _nextScheduleUpdate = DateTime.Now.AddDays(-1);
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

                var nextScheduleChange = DateTime.Now - _nextScheduleUpdate;
                if (nextScheduleChange.TotalHours > 23 && DateTime.Now.ToUniversalTime().Hour > 15)
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
                    //await _heaterStatus.SendUpdate(device.name, Models.CurrentPointParameterType.LastScheduleChangeInHours, 24);
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