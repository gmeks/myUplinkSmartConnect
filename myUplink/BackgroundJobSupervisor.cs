using DetermenisticRandom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyUplinkSmartConnect.Models;
using MyUplinkSmartConnect.Services;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect
{
    internal class BackgroundJobSupervisor
    {
        Thread? _threadWorker;
        Thread? _threadHealthChecker;
        bool _killWorker;

        DateTime _lastWorkerAliveCheck;
        DateTime _lastWorkerHealthAlive;

        DateTime _nextStatusUpdate;
        DateTime _nextScheduleUpdate;

        JobCheckHeaterStatus? _heaterStatus;
        readonly MyUplinkService _myUplinkAPI;
        readonly MQTTService _mqttService;
        readonly CurrentStateService _currentState;
        readonly ILogger<object> _logger;

        const int _minimumHourForScheduleStart = 14;

        public BackgroundJobSupervisor(ILogger<object> logger)
        {
            _mqttService = Settings.ServiceLookup?.GetService<MQTTService>() ?? throw new NullReferenceException();
            _myUplinkAPI = Settings.ServiceLookup?.GetService<MyUplinkService>() ?? throw new NullReferenceException();
            _currentState = Settings.ServiceLookup?.GetService<CurrentStateService>() ?? throw new NullReferenceException();

            var random = new DetermenisticInt();
            int tmpHour = random.GetByte(_minimumHourForScheduleStart, 22, BuildDetermenisticRandomSeed(), 3);
            int tmpMinute = random.GetByte(_minimumHourForScheduleStart, 22, BuildDetermenisticRandomSeed(), 2);

            _nextScheduleUpdate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, tmpHour, tmpMinute, 0);
            _nextStatusUpdate = DateTime.UtcNow;
            _nextScheduleUpdate = _nextScheduleUpdate.AddDays(-1);
            _logger = logger;
             Log.Logger.Information("Target Schedule change time is {NextScheduleUpdate}", _nextScheduleUpdate.ToLocalTime().ToShortTimeString());
        }

        void CreateWorkerThread()
        {
            _lastWorkerAliveCheck = DateTime.UtcNow;
            _threadWorker = new Thread(Worker);
            _threadWorker.IsBackground = true;
            _threadWorker.Name = "BackgroundJobs";
            _threadWorker.Start();
        }

        void CreateWorkerHealthCheckThread()
        {
            _lastWorkerHealthAlive = DateTime.UtcNow;
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

                _lastWorkerHealthAlive = DateTime.UtcNow;
                var lastJobTime = DateTime.UtcNow - _lastWorkerAliveCheck;
                if (lastJobTime.TotalMinutes > 20)
                {
                    Log.Logger.Warning("Worker thread had stopped, restarting it.");
                    CreateWorkerThread();
                }
            }
        }

        async void Worker()
        {
            while (!_killWorker)
            {
                _lastWorkerAliveCheck = DateTime.UtcNow;

                if (_myUplinkAPI == null)
                {
                    Log.Logger.Debug("myUplink API is not ready");
                    continue;
                }

                if (_heaterStatus == null)
                {
                    _heaterStatus = new JobCheckHeaterStatus(_myUplinkAPI, _mqttService, _currentState);
                }

                if (Settings.Instance.ChangeSchedule)
                {
                    await WorkerSchedule();
                }                

                if(Settings.Instance.MQTTActive)
                {
                    await WorkerHeaterStatus();
                }                    

                Thread.Sleep(TimeSpan.FromMinutes(1));

                var lastJobTime = DateTime.UtcNow - _lastWorkerHealthAlive;
                if (!_killWorker && lastJobTime.TotalMinutes > 20)
                {
                    CreateWorkerHealthCheckThread();
                    Log.Logger.Warning("Main worker thread was dead, restaring. Has been down for {min} minutes", lastJobTime.TotalMinutes);
                }
            }
        }

        async Task WorkerSchedule()
        {
            var timeSinceLastChange = DateTime.Now - _myUplinkAPI.GetLastScheduleChange();
            var nextScheduleChange = DateTime.UtcNow - _nextScheduleUpdate;

            if(_myUplinkAPI.GetLastScheduleChange() == DateTime.MinValue)
            {
                timeSinceLastChange = nextScheduleChange;
            }
            
#if DEBUG
            if (true || Settings.Instance.ForceScheduleRebuild)
            //if (nextScheduleChange.TotalHours >= 24 && DateTime.UtcNow.Hour > _minimumHourForScheduleStart || _nextScheduleUpdate > DateTime.UtcNow && DateTime.UtcNow.Hour > _minimumHourForScheduleStart)
#else
            if (nextScheduleChange.TotalHours >= 24 && DateTime.UtcNow.Hour > _minimumHourForScheduleStart 
            ||  _nextScheduleUpdate > DateTime.UtcNow && DateTime.UtcNow.Hour > _minimumHourForScheduleStart 
            || Settings.Instance.ForceScheduleRebuild 
            || timeSinceLastChange.TotalHours >= 26 && DateTime.UtcNow.Hour > _minimumHourForScheduleStart)
#endif
            {
                Settings.Instance.ForceScheduleRebuild = false;
                Log.Logger.Debug("Last schedule was {hours} hours ago and above minimum hour for schedule start {minHour}", nextScheduleChange.TotalHours, (DateTime.UtcNow.Hour > _minimumHourForScheduleStart));
                try
                {
                    _myUplinkAPI.ClearCached();

                    var buildSchedule = new JobReScheuleheating(_logger ,_myUplinkAPI, _mqttService, _currentState);
                    var status = await buildSchedule.Work();

                    if (status)
                    {                        
                        _nextScheduleUpdate = _nextScheduleUpdate.AddDays(1);
                        _myUplinkAPI.SetLastScheduleChange();
                        Log.Logger.Debug("Schedule update was successfull next one will be {nextUpdate}", _nextScheduleUpdate.ToString());
                        _currentState.SetSuccess(States.Schedule);
                    }
                    else
                    {
                        _currentState.SetFailed(States.Schedule);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Failed to run heater schedule job");
                    _currentState.SetFailed(States.Schedule);
                }

                
                // We now update the MQTT with information about when the next update will happen
                var group = await _myUplinkAPI.GetDevices();
                if (group != null && _heaterStatus != null && Settings.Instance.MQTTActive)
                {
                    var devicesStatusUpdatedCount = 0;
                    foreach (var device in group)
                    {
                        if (device.devices == null || string.IsNullOrEmpty(device.name))
                            throw new NullReferenceException("device.devices or device.name is null");

                        foreach (var tmpdevice in device.devices)
                        {
                            await _mqttService.SendUpdate(device.name, Models.CurrentPointParameterType.LastScheduleChangeInHours, Convert.ToInt32(timeSinceLastChange.TotalHours),true);
                        }

                        devicesStatusUpdatedCount++;
                    }

                    Log.Logger.Debug("Updated status for {devicesStatusUpdatedCount} devices", devicesStatusUpdatedCount);
                }
                else
                {
                    Log.Logger.Debug("Failed to do device status updates, found no devices");
                }
            }
        }

        async Task WorkerHeaterStatus()
        {
            if (_heaterStatus == null)
            {
                Log.Logger.Debug("Cannot do status updates heaterstatus is null {_heaterStatus} or API is down {myapi}", (_heaterStatus is null), (_myUplinkAPI is null));
                return;
            }
            var nextStatusUpdate = DateTime.UtcNow - _nextStatusUpdate;
            bool updateNow = nextStatusUpdate.TotalMinutes >= (double)Settings.Instance.CheckRemoteStatsIntervalInMinutes;

            Log.Logger.Debug("Next status update in {Minutes} should update now {UpdateNow}", nextStatusUpdate.TotalMinutes, updateNow);
            if (updateNow)
            {
                try
                {
                    var itemsUpdated = await _heaterStatus.Work();

                    if(itemsUpdated != 0)
                    {
                        _nextStatusUpdate = DateTime.UtcNow;
                        _currentState.SetSuccess(States.HeaterStats);
                    }
                    else
                    {
                        _currentState.SetFailed(States.HeaterStats);
                    }                    
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Failed to run heater status job");

                    if (Settings.Instance.MQTTActive)
                        _currentState.SetFailed(States.HeaterStats);

                    _heaterStatus = null;
                    return;
                }
            }                        
        }
    }
}