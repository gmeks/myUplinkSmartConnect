using MQTTnet;
using MyUplinkSmartConnect.Models;
using MyUplinkSmartConnect.Services;
using Serilog;

namespace MyUplinkSmartConnect
{
    internal class JobCheckHeaterStatus
    {
        IEnumerable<DeviceGroup>? _deviceGroup;
        readonly MyUplinkService _myUplinkAPI;
        readonly MQTTService _mqttService;
        readonly CurrentStateService _currentState;

        public JobCheckHeaterStatus(MyUplinkService myUplinkAPI, MQTTService mqttService,CurrentStateService currentState)
        {
            _myUplinkAPI = myUplinkAPI;
            _mqttService = mqttService;
            _currentState = currentState;
        }

        public async Task<int> Work()
        {
            if (_myUplinkAPI == null)
            {
                Log.Logger.Debug("myUplink APi is null, cannot do status updates");
                return 0;
            }

            if(_deviceGroup == null)
            {
                _deviceGroup = await _myUplinkAPI.GetDevices();
            }
            
            Log.Logger.Debug("Found {DeviceCount} devices, will attemt to check for status updates", _deviceGroup.Count());

            if (_deviceGroup.Count() == 0)
            {
                Log.Logger.Debug("Unable to find any devices to check in cache for stats update, will reset cache.");
                _myUplinkAPI.ClearCached();
                _deviceGroup = await _myUplinkAPI.GetDevices();

                if(_deviceGroup.Count() == 0)
                {
                    Log.Logger.Warning("Reset device cache failed, device list returned is still 0, updating stats will fail");
                }
            }

            var deviceStatsUpdatesSendt = 0;

            foreach (var device in _deviceGroup)
            {
                if (string.IsNullOrEmpty(device.name) || device.devices == null)
                    throw new NullReferenceException("device name or device.devices is null");

                foreach (var tmpdevice in device.devices)
                {
                    var devicePointsList = await _myUplinkAPI.GetDevicePoints(tmpdevice, CurrentPointParameterType.TargetTemprature, CurrentPointParameterType.CurrentTemprature, 
                        CurrentPointParameterType.EstimatedPower, CurrentPointParameterType.EnergyTotal, CurrentPointParameterType.EnergiStored, CurrentPointParameterType.FillLevel);

                    foreach (var devicePoint in devicePointsList)
                    {
                        var parm = (CurrentPointParameterType)int.Parse(devicePoint.parameterId ?? "0");

                        switch (parm)
                        {
                            case CurrentPointParameterType.EnergiStored:
                                _currentState.CurrentTankEnergi = devicePoint.value;
                                break;
                        }

                        switch (parm)
                        {
                            case CurrentPointParameterType.FillLevel:
                            case CurrentPointParameterType.EnergiStored:
                            case CurrentPointParameterType.EnergyTotal:
                            case CurrentPointParameterType.EstimatedPower:
                            case CurrentPointParameterType.TargetTemprature:
                            case CurrentPointParameterType.CurrentTemprature:
                                deviceStatsUpdatesSendt++;
                                await _mqttService.SendUpdate(device.name, parm, devicePoint.value);
                                break;
                        }                        
                    }
                }
            }

            if(deviceStatsUpdatesSendt == 0)
            {
                Log.Logger.Debug("When doing stats update for water heaters, 0 stats items where sendt");
            }

            return deviceStatsUpdatesSendt;
        }        
    }
}
