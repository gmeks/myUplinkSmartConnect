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

namespace MyUplinkSmartConnect
{
    internal class myuplinkApi
    {
        RestClient _httpClient;
        AuthToken? _token;
        const string _tokenFile = "tokenfile_internal.json";
        readonly Uri _apiUrl;
        Dictionary<string, HeaterWeeklyRoot[]> _heaterScheduleRoot;

        public myuplinkApi()
        {
            _heaterScheduleRoot = new Dictionary<string, HeaterWeeklyRoot[]>();
            _apiUrl = new Uri("https://internalapi.myuplink.com");
            _httpClient = new RestClient(_apiUrl);
        }

        public async Task<bool> LoginToApi()
        {
            if (_token != null && !_token.IsExpired)
            {
                return true;
            }

            if (File.Exists(_tokenFile))
            {
                _token = JsonSerializer.Deserialize<AuthToken>(File.ReadAllText(_tokenFile));
                if (_token != null || _token?.IsExpired == false)
                {
                    _httpClient.AddDefaultHeader("authorization", "Bearer " + _token.access_token);

                    var verifyToken = await Ping();
                    if (!verifyToken)
                    {
                        _token = null;
                    }
                    else
                    {
                        Log.Logger.Information("Loaded old token from tokenfile");
                        return true;
                    }
                }
            }

            if (_token == null || _token.IsExpired)
            {
                var request = new RestRequest("oauth/token") { Method = Method.Post };
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("client_id", "My-Uplink-Web");
                request.AddParameter("grant_type", "password");
                request.AddParameter("username", Settings.Instance.UserName);
                request.AddParameter("password", Settings.Instance.Password);

                var tResponse = await _httpClient.ExecuteAsync(request);
                if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
                {
                    _token = JsonSerializer.Deserialize<AuthToken>(tResponse.Content);
                    if (_token != null)
                    {
                        _httpClient = new RestClient(_apiUrl);
                        _httpClient.AddDefaultHeader("authorization", "Bearer " + _token.access_token);

                        File.WriteAllText(_tokenFile, JsonSerializer.Serialize(_token));
                        Log.Logger.Information("Login via API got new token");
                        return true;
                    }
                }
                else
                {
                    Log.Logger.Information($"Login to myUplink API failed with status {tResponse.StatusCode} and message {tResponse.Content}");
                    return false;
                }
            }

            Log.Logger.Information("Login to myUplink API failed.");
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
                var devices = JsonSerializer.Deserialize<IEnumerable<CurrentValues>>(tResponse.Content);
                return devices;
            }

            return Array.Empty<CurrentValues>();
        }

        public async Task<IEnumerable<DeviceGroup>> GetDevices()
        {
            var loginStatus = await LoginToApi();
            if(!loginStatus)
                return Array.Empty<DeviceGroup>();

            var request = new RestRequest("/v2/groups/me") { Method = Method.Get };
            var tResponse = await _httpClient.ExecuteAsync(request);

            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
            {
                var devices = JsonSerializer.Deserialize<MyGroupRoot>(tResponse.Content);
                return devices.groups;
            }

            return Array.Empty<DeviceGroup>();
        }

        public async Task<List<HeaterWeeklyEvent>> GetWheeklySchedules(Device device)
        {
            var loginStatus = await LoginToApi();
            if (!loginStatus)
                return Array.Empty<HeaterWeeklyEvent>().ToList();

            var request = new RestRequest($"/v2/devices/{device.id}/weekly-schedules") { Method = Method.Get };

            var tResponse = await _httpClient.ExecuteAsync(request);
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
            {
                //fixme. this is likely not optimal, but for some reason this is also a array?
                var heaterRoot = JsonSerializer.Deserialize<HeaterWeeklyRoot[]>(tResponse.Content);
                _heaterScheduleRoot.Add(device.id, heaterRoot);
                return heaterRoot.First().events;
            }

            return Array.Empty<HeaterWeeklyEvent>().ToList();
        }

        public async Task<bool> SetWheeklySchedules(Device device, IEnumerable<HeaterWeeklyEvent> adjustedSchedule)
        {
            var loginStatus = await LoginToApi();
            if (!loginStatus)
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
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK  || tResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return true;
            }

            return false;
        }
    }
}
