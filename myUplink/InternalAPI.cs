using myUplink.Models;
using myUplink.ModelsPublic.Internal;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace myUplink
{
    internal class InternalAPI
    {
        RestClient _httpClient;
        AuthToken? _token;
        const string _tokenFile = "tokenfile_internal.json";
        readonly Uri _apiUrl;

        public InternalAPI()
        {
            _apiUrl = new Uri("https://internalapi.myuplink.com");
            _httpClient = new RestClient(_apiUrl);
        }

        public async Task<bool> LoginToApi(string username, string password)
        {
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
                }
            }

            if (_token == null || _token.IsExpired)
            {
                var request = new RestRequest("oauth/token") { Method = Method.Post };
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("client_id", "My-Uplink-Web");
                request.AddParameter("grant_type", "password");
                request.AddParameter("username", username);
                request.AddParameter("password", password);

                var tResponse = await _httpClient.ExecuteAsync(request);
                if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
                {
                    _token = JsonSerializer.Deserialize<AuthToken>(tResponse.Content);
                    if (_token != null)
                    {
                        _httpClient = new RestClient(_apiUrl);
                        _httpClient.AddDefaultHeader("authorization", "Bearer " + _token.access_token);

                        File.WriteAllText(_tokenFile, JsonSerializer.Serialize(_token));
                        return true;
                    }
                }
            }
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

        public async Task<IEnumerable<Group>> GetDevices()
        {
            var request = new RestRequest("/v2/groups/me") { Method = Method.Get };
            var tResponse = await _httpClient.ExecuteAsync(request);

            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
            {
                var devices = JsonSerializer.Deserialize<MyGroupRoot>(tResponse.Content);
                return devices.groups;
            }

            return Array.Empty<Group>();
        }

        public async Task<IEnumerable<HeaterWeeklyEvent>> GetWheeklySchedules(Device device)
        {
            var request = new RestRequest($"/v2/devices/{device.id}/weekly-schedules") { Method = Method.Get };

            var tResponse = await _httpClient.ExecuteAsync(request);
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
            {
                //fixme. this is likely not optimal, but for some reason this is also a array?
                var devices = JsonSerializer.Deserialize<HeaterWeeklyRoot[]>(tResponse.Content).FirstOrDefault();
                return devices.events;
            }

            return Array.Empty<HeaterWeeklyEvent>();
        }

        /*
        public async Task<IEnumerable<Group>> GetDevices(Device device)
        {
            var request = new RestRequest($"/v2/subscriptions/{device.id}/devices") { Method = Method.Get };
            var tResponse = await _httpClient.ExecuteAsync(request);

            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
            {
                var devices = JsonSerializer.Deserialize<MyGroupRoot>(tResponse.Content);
                return devices.groups;
            }

            return Array.Empty<Group>();
        }
        //
        /*
        public async Task<ScheduleConfig> GetCurrentSchedule(Device device)
        {
            var request = new RestRequest($"/v2/devices/{device.id}/schedule-config") { Method = Method.Get };

            var tResponse = await _httpClient.ExecuteAsync(request);
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
            {
                var devices = JsonSerializer.Deserialize<ScheduleConfig>(tResponse.Content);
                return devices;
            }

            return null;
        }
        */
        public async Task<IEnumerable<WaterHeaterMode>> GetCurrentModes(Device device)
        {
            //https://internalapi.myuplink.com/v2/devices/HOIAX_3083989de217_35f19927-203c-4a6b-a84b-9d1c1a9b8d6c/schedule-modes
            //Put
            //[{"modeId":50,"name":"M1 (50C, 700W)","settings":[{"settingId":1,"value":50},{"settingId":2,"value":1}]},{"modeId":51,"name":"M2 (50C, 0W)","settings":[{"settingId":1,"value":50},{"settingId":2,"value":0}]},{"modeId":52,"name":"M3 (65C, 2000W)","settings":[{"settingId":1,"value":65},{"settingId":2,"value":3}]},{"modeId":53,"name":"M4 (70C, 700W)","settings":[{"settingId":1,"value":70},{"settingId":2,"value":1}]},{"modeId":54,"name":"M5 (70C, 1300W)","settings":[{"settingId":1,"value":70},{"settingId":2,"value":2}]},{"modeId":55,"name":"M6 (70C, 2000W)","settings":[{"settingId":1,"value":"71"},{"settingId":2,"value":3}]}]
            var request = new RestRequest($"/v2/devices/{device.id}/schedule-modes") { Method = Method.Get };
            var tResponse = await _httpClient.ExecuteAsync(request);
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
            {
                var devices = JsonSerializer.Deserialize<WaterHeaterMode[]>(tResponse.Content);
                return devices;
            }

            return Array.Empty<WaterHeaterMode>();
        }
    }
}
