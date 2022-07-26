using MQTTnet;
using MQTTnet.Client;
using MyUplinkSmartConnect.Models;
using Serilog;

namespace MyUplinkSmartConnect
{
    internal class JobCheckHeaterStatus
    {
        IEnumerable<DeviceGroup>? _deviceGroup;

        public async Task<int> Work()
        {
            if (Settings.Instance.myuplinkApi == null)
            {
                Log.Logger.Debug("myUplink APi is null, cannot do status updates");
                return 0;
            }

            if(_deviceGroup == null)
            {
                _deviceGroup = await Settings.Instance.myuplinkApi.GetDevices();
            }
            
            Log.Logger.Debug("Found {DeviceCount} devices, will attemt to check for status updates", _deviceGroup.Count());

            if (_deviceGroup.Count() == 0)
            {
                Log.Logger.Debug("Unable to find any devices to check in cache for stats update, will reset cache.");
                Settings.Instance.myuplinkApi.ClearCached();
                _deviceGroup = await Settings.Instance.myuplinkApi.GetDevices();

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
                    var devicePointsList = await Settings.Instance.myuplinkApi.GetDevicePoints(tmpdevice, CurrentPointParameterType.TargetTemprature, CurrentPointParameterType.CurrentTemprature, 
                        CurrentPointParameterType.EstimatedPower, CurrentPointParameterType.EnergyTotal, CurrentPointParameterType.EnergiStored, CurrentPointParameterType.FillLevel);

                    foreach (var devicePoint in devicePointsList)
                    {
                        var parm = (CurrentPointParameterType)int.Parse(devicePoint.parameterId ?? "");

                        switch(parm)
                        {
                            case CurrentPointParameterType.FillLevel:
                            case CurrentPointParameterType.EnergiStored:
                            case CurrentPointParameterType.EnergyTotal:
                            case CurrentPointParameterType.EstimatedPower:
                            case CurrentPointParameterType.TargetTemprature:
                            case CurrentPointParameterType.CurrentTemprature:
                                deviceStatsUpdatesSendt++;
                                await Settings.Instance.MQTTSender.SendUpdate(device.name, parm, devicePoint.value);
                                break;
                        }                        
                    }
                }
            }

            if(deviceStatsUpdatesSendt == 0)
            {
                Log.Logger.Warning("When doing stats update for water heaters, 0 stats items where sendt");
            }

            return deviceStatsUpdatesSendt;
        }        
    }
}
