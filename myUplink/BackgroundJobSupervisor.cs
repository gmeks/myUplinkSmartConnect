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
        Thread _threadWorker;
        Thread _threadHealthChecker;
        bool _killWorker;

        DateTime _lastWorkerAliveCheck;
        DateTime _lastWorkerHealthAlive;

        DateTime _nextStatusUpdate;
        DateTime _nextScheduleUpdate;

        JobCheckHeaterStatus? _heaterStatus;

        public BackgroundJobSupervisor()
        {
            var random = new DetermenisticInt();
            int tmpHour = random.GetByte(13, 22, BuildDetermenisticRandomSeed(), 3);
            int tmpMinute = random.GetByte(13, 22, BuildDetermenisticRandomSeed(), 2);

            _nextScheduleUpdate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, tmpHour,tmpMinute,0);
            _nextStatusUpdate = DateTime.Now;

            _nextScheduleUpdate = _nextScheduleUpdate.AddDays(-1);
            Log.Logger.Information("Target Schedule change time is {NextScheduleUpdate}",_nextScheduleUpdate.ToLocalTime().ToShortTimeString());
        }

        void CreateWorkerThread()
        {
            _lastWorkerAliveCheck = DateTime.Now;
            _threadWorker = new Thread(Worker);
            _threadWorker.IsBackground = true;
            _threadWorker.Name = "BackgroundJobs";
            _threadWorker.Start();
        }

        void CreateWorkerHealthCheckThread()
        {
            _lastWorkerHealthAlive = DateTime.Now;
            _threadHealthChecker = new Thread(WorkerHealthCheck);
            _threadHealthChecker.IsBackground = true;
            _threadHealthChecker.Name = "BackgroundJobs healthcheck";
            _threadHealthChecker.Start();
        }

        string BuildDetermenisticRandomSeed()
        {
            var seed = new StringBuilder();

            if (!string.IsNullOrEmpty(Settings.Instance.UserName))
                seed.Append(Settings.Instance.UserName);

            if (!string.IsNullOrEmpty(Settings.Instance.MQTTServer))
                seed.Append(Settings.Instance.MQTTServer);

            if (!string.IsNullOrEmpty(Settings.Instance.PowerZone))
                seed.Append(Settings.Instance.PowerZone);

            return seed.ToString();
        }

        public void Start()
        {
            _killWorker = false;
            if (_threadWorker == null || !_threadWorker.IsAlive)
            {
                CreateWorkerThread();
            }

            if (_threadHealthChecker == null || !_threadHealthChecker.IsAlive)
            {
                CreateWorkerHealthCheckThread();
            }
        }

        public void Stop()
        {
            _killWorker = true;
        }

        void WorkerHealthCheck()
        {
            while (!_killWorker)
            {
#if DEBUG
                Thread.Sleep(TimeSpan.FromMinutes(1));
#else
                Thread.Sleep(TimeSpan.FromMinutes(10));
#endif

                if (_killWorker)
                    break;

                _lastWorkerHealthAlive = DateTime.Now;
                var lastJobTime = DateTime.Now - _lastWorkerAliveCheck;
                if (lastJobTime.TotalMinutes > 20)
                {
                    Log.Logger.Error("Worker thread had stopped, restarting it.");
                    //CreateWorkerThread();
                }                
            }
        }

        async void Worker()
        {
            while(!_killWorker)
            {
                _lastWorkerAliveCheck = DateTime.Now;
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
                        Log.Logger.Error(ex,"Failed to run heater status job");
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
                        Settings.Instance.myuplinkApi.ClearCached();

                        await JobReScheuleheating.Work();
                        _nextScheduleUpdate = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, "Failed to run heater status job");
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

                var lastJobTime = DateTime.Now - _lastWorkerHealthAlive;
                if (!_killWorker && lastJobTime.TotalMinutes > 20)
                {
                    CreateWorkerHealthCheckThread();
                }
            }
        }
    }
}