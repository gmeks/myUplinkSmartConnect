using MyUplinkSmartConnect.Models;
using RestSharp;
using RestSharp.Authenticators;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect.Services
{
    internal class MyUplinkService
    {
        RestClient _httpClient;
        AuthToken? _token;
        readonly Uri _apiUrl;
        Dictionary<string, HeaterWeeklyRoot[]> _heaterScheduleRoot;
        List<DeviceGroup>? _devices;
        string _myUplinkDirectory;
        DateTime? _lastUpdateTime;

        public MyUplinkService()
        {
            _heaterScheduleRoot = new Dictionary<string, HeaterWeeklyRoot[]>();
            _apiUrl = new Uri("https://internalapi.myuplink.com");
            _httpClient = new RestClient(_apiUrl);

            _myUplinkDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\MyUplink-smartconnect";
            if (!Directory.Exists(_myUplinkDirectory))
                Directory.CreateDirectory(_myUplinkDirectory);
        }

        public void ClearCached()
        {
            _heaterScheduleRoot?.Clear();
            _devices?.Clear();
        }

        public DateTime GetLastScheduleChange()
        {
            if (_lastUpdateTime.HasValue)
                return _lastUpdateTime.Value;

            string tokenFileFullPath = System.IO.Path.Combine(_myUplinkDirectory + "\\lastScheduleChange.json");
            _lastUpdateTime = DateTime.MinValue;

            try
            {
                if (File.Exists(tokenFileFullPath))
                {
                    string fileContent = File.ReadAllText(tokenFileFullPath);
                    _lastUpdateTime = DateTime.Parse(fileContent);
                }
            }
            catch
            {

            }
            return _lastUpdateTime.Value;
        }

        public bool SetLastScheduleChange()
        {
            string tokenFileFullPath = System.IO.Path.Combine(_myUplinkDirectory + "\\lastScheduleChange.json");

            try
            {
                if (File.Exists(tokenFileFullPath))
                {
                    File.Delete(tokenFileFullPath);
                }

                _lastUpdateTime = DateTime.Now;
                File.WriteAllText(tokenFileFullPath,DateTime.Now.ToString());
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> LoginToApi()
        {
            string tokenFileFullPath = System.IO.Path.Combine(_myUplinkDirectory + "\\tokenfile_internal.json");

            if (_token != null && !_token.IsExpired)
            {
                return true;
            }
            else if (_token == null && File.Exists(tokenFileFullPath))
            {
                _token = JsonSerializer.Deserialize<AuthToken>(File.ReadAllText(tokenFileFullPath));
                if (_token != null && !string.IsNullOrEmpty(_token.access_token) && _token.IsExpired == false)
                {
                    CreateNewHttpClient(_token.access_token);

                    var verifyToken = await Ping().ConfigureAwait(true);
                    if (!verifyToken)
                    {
                        _token = null;
                    }
                    else
                    {
                        Log.Logger.Debug("Loaded old token from tokenfile");
                        return true;
                    }
                }
            }

            if (_token == null || _token.IsExpired)
            {
                if (_token == null)
                    Log.Logger.Debug("Login is required, no old bear token found");
                else if (_token.IsExpired)
                    Log.Logger.Debug("Login is required, existing token is expired");

                var request = new RestRequest("oauth/token") { Method = Method.Post };
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("client_id", "My-Uplink-Web");
                request.AddParameter("grant_type", "password");
                request.AddParameter("username", Settings.Instance.UserName);
                request.AddParameter("password", Settings.Instance.Password);

                var tResponse = await _httpClient.ExecuteAsync(request).ConfigureAwait(true);
                if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
                {
                    _token = JsonSerializer.Deserialize<AuthToken>(tResponse.Content);
                    if (_token != null)
                    {
                        if (string.IsNullOrEmpty(_token.access_token))
                            throw new NullReferenceException("Got token response without a token...");

                        CreateNewHttpClient(_token.access_token);
                        File.WriteAllText(tokenFileFullPath, JsonSerializer.Serialize(_token));

                        Log.Logger.Debug("Login via API got new token");
                        return true;
                    }
                }
                else
                {
                    Log.Logger.Warning("Login to myUplink API failed with status {StatusCode} and message {Content}", tResponse.StatusCode, tResponse.Content);
                    return false;
                }
            }

            Log.Logger.Warning("Login to myUplink API failed");
            return false;
        }

        public async Task<bool> Ping()
        {
            var request = new RestRequest("/v2/protected-ping") { Method = Method.Get };
            var tResponse = await _httpClient.ExecuteAsync(request);

            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK || tResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
                return true;

            return false;
        }

        public async Task<IEnumerable<CurrentValues>> GetDevicePoints(Device device, params CurrentPointParameterType[] points)
        {
            var loginStatus = await LoginToApi();
            if (!loginStatus)
                return Array.Empty<CurrentValues>();

            var parameters = new StringBuilder();
            foreach (var point in points)
            {
                if (parameters.Length > 0)
                    parameters.Append($",{(int)point}");
                else
                    parameters.Append($"{(int)point}");
            }

            var request = new RestRequest($"https://internalapi.myuplink.com/v2/devices/{device.id}/points?parameters={parameters.ToString()}") { Method = Method.Get };
            var tResponse = await _httpClient.ExecuteAsync(request);

            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
            {
                var devices = JsonSerializer.Deserialize<IEnumerable<CurrentValues>>(tResponse.Content) ?? Array.Empty<CurrentValues>();
                return devices;
            }

            return Array.Empty<CurrentValues>();
        }

        public async Task<Device> GetDefaultDevice()
        {
            var group = await GetDevices();

            return group?.FirstOrDefault()?.devices.FirstOrDefault() ?? throw new NullReferenceException("Failed to find default device");           
        }

        public async Task<IEnumerable<DeviceGroup>> GetDevices()
        {
            if (_devices?.Count > 0)
                return _devices;

            var loginStatus = await LoginToApi();
            if (!loginStatus)
            {
                Log.Logger.Debug("Not logged in to myUplink api, cannot get device list");
                return Array.Empty<DeviceGroup>();
            }

            var request = new RestRequest("/v2/groups/me") { Method = Method.Get };
            var tResponse = await _httpClient.ExecuteAsync(request);

            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
            {
                var devices = JsonSerializer.Deserialize<MyGroupRoot>(tResponse.Content);
                var deviceList = devices?.groups ?? new List<DeviceGroup>();

                foreach (var deviceGroup in deviceList)
                {
                    foreach (var device in deviceGroup.devices)
                    {
                        Log.Logger.Information("Found device with ID: {DeviceId}", device.id);
                    }
                }

                _devices = deviceList;
                return deviceList;
            }

            return Array.Empty<DeviceGroup>();
        }

        public async Task<List<HeaterWeeklyEvent>> GetWheeklySchedules(Device device)
        {
            var loginStatus = await LoginToApi();
            if (!loginStatus || string.IsNullOrEmpty(device.id))
                return Array.Empty<HeaterWeeklyEvent>().ToList();

            var request = new RestRequest($"/v2/devices/{device.id}/weekly-schedules") { Method = Method.Get };

            var tResponse = await _httpClient.ExecuteAsync(request);
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
            {
                //fixme. this is likely not optimal, but for some reason this is also a array?
                var heaterRoot = JsonSerializer.Deserialize<HeaterWeeklyRoot[]>(tResponse.Content) ?? Array.Empty<HeaterWeeklyRoot>();

                if (_heaterScheduleRoot.ContainsKey(device.id))
                    _heaterScheduleRoot[device.id] = heaterRoot;
                else
                    _heaterScheduleRoot.Add(device.id, heaterRoot);

                return heaterRoot.First().events ?? Array.Empty<HeaterWeeklyEvent>().ToList();
            }

            return Array.Empty<HeaterWeeklyEvent>().ToList();
        }

        public string GetCurrentDayOrder(Device device)
        {
            if (string.IsNullOrEmpty(device.id))
                throw new NullReferenceException("device.id is null");

            var heaterRoot = _heaterScheduleRoot[device.id];
            return heaterRoot.First().weekFormat ?? "mon,tue,wed,thu,fri,sat,sun";
        }

        public async Task<bool> SetWheeklySchedules(Device device, IEnumerable<HeaterWeeklyEvent> adjustedSchedule)
        {
            var loginStatus = await LoginToApi();
            if (!loginStatus || string.IsNullOrEmpty(device.id))
                return false;

            //https://internalapi.myuplink.com/v2/devices/HOIAX_3083989de217_35f19927-203c-4a6b-a84b-9d1c1a9b8d6c/weekly-schedules
            var heaterRoot = _heaterScheduleRoot[device.id];
            heaterRoot.First().events = adjustedSchedule.ToList();

            var request = new RestRequest($"/v2/devices/{device.id}/weekly-schedules") { Method = Method.Put };
            var json = JsonSerializer.Serialize(heaterRoot);
            request.AddBody(json, "application/json");

            var tResponse = await _httpClient.ExecuteAsync(request);
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK || tResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
                return true;

            Log.Logger.Warning("Failed to set schedule {StatusCode} and message {Content}", tResponse.StatusCode, tResponse.Content);
            return false;
        }

        public async Task<List<WaterHeaterMode>> GetCurrentModes(Device device)
        {
            var loginStatus = await LoginToApi();
            if (!loginStatus)
                return Array.Empty<WaterHeaterMode>().ToList();

            //https://internalapi.myuplink.com/v2/devices/HOIAX_3083989de217_35f19927-203c-4a6b-a84b-9d1c1a9b8d6c/schedule-modes
            //Put
            //[{"modeId":50,"name":"M1 (50C, 700W)","settings":[{"settingId":1,"value":50},{"settingId":2,"value":1}]},{"modeId":51,"name":"M2 (50C, 0W)","settings":[{"settingId":1,"value":50},{"settingId":2,"value":0}]},{"modeId":52,"name":"M3 (65C, 2000W)","settings":[{"settingId":1,"value":65},{"settingId":2,"value":3}]},{"modeId":53,"name":"M4 (70C, 700W)","settings":[{"settingId":1,"value":70},{"settingId":2,"value":1}]},{"modeId":54,"name":"M5 (70C, 1300W)","settings":[{"settingId":1,"value":70},{"settingId":2,"value":2}]},{"modeId":55,"name":"M6 (70C, 2000W)","settings":[{"settingId":1,"value":"71"},{"settingId":2,"value":3}]}]
            var request = new RestRequest($"/v2/devices/{device.id}/schedule-modes") { Method = Method.Get };
            var tResponse = await _httpClient.ExecuteAsync(request);
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
            {
                var devices = JsonSerializer.Deserialize<List<WaterHeaterMode>>(tResponse.Content) ?? Array.Empty<WaterHeaterMode>().ToList();
                return devices;
            }

            Log.Logger.Warning("Failed to get modes {StatusCode} and message {Content}", tResponse.StatusCode, tResponse.Content);
            return Array.Empty<WaterHeaterMode>().ToList();
        }

        public async Task<bool> SetCurrentModes(Device device, IEnumerable<WaterHeaterMode> modes)
        {
            var loginStatus = await LoginToApi();
            if (!loginStatus)
                return false;

            //            //Put
            //https://internalapi.myuplink.com/v2/devices/HOIAX_3083989de217_35f19927-203c-4a6b-a84b-9d1c1a9b8d6c/schedule-modes
            var request = new RestRequest($"/v2/devices/{device.id}/schedule-modes") { Method = Method.Put };
            request.AddJsonBody<IEnumerable<WaterHeaterMode>>(modes);
            var tResponse = await _httpClient.ExecuteAsync(request);
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK || tResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return true;
            }

            Log.Logger.Warning("Failed to update modes {StatusCode} and message {Content}", tResponse.StatusCode, tResponse.Content);
            return false;
        }

        public async Task<bool> SetVacation(Device device, VacationsSchedules vaction)
        {
            var loginStatus = await LoginToApi();
            if (!loginStatus)
                return false;

            //            //Put
            //https://internalapi.myuplink.com/v2/devices/HOIAX_3083989de217_35f19927-203c-4a6b-a84b-9d1c1a9b8d6c/schedule-modes
            var request = new RestRequest($"/v2/devices/{device.id}/vacation-schedules") { Method = Method.Put };
            request.AddJsonBody<IEnumerable<VacationsSchedules>>(new VacationsSchedules[] { vaction });
            var tResponse = await _httpClient.ExecuteAsync(request);
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK || tResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return true;
            }

            Log.Logger.Warning("Failed to update modes {StatusCode} and message {Content}", tResponse.StatusCode, tResponse.Content);
            return false;
        }

        void CreateNewHttpClient(string bearHeader)
        {
            _httpClient = new RestClient(_apiUrl);
            _httpClient.AddDefaultHeader("authorization", "Bearer " + bearHeader);
        }
    }
}
