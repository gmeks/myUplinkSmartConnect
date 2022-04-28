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

        HeaterWeeklyRoot[] _heaterRoot;
        public async Task<List<HeaterWeeklyEvent>> GetWheeklySchedules(Device device)
        {
            var request = new RestRequest($"/v2/devices/{device.id}/weekly-schedules") { Method = Method.Get };

            var tResponse = await _httpClient.ExecuteAsync(request);
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
            {
                //fixme. this is likely not optimal, but for some reason this is also a array?
                _heaterRoot = JsonSerializer.Deserialize<HeaterWeeklyRoot[]>(tResponse.Content);
                return _heaterRoot.First().events;
            }

            return Array.Empty<HeaterWeeklyEvent>().ToList();
        }

        public async Task<bool> SetWheeklySchedules(Device device, IEnumerable<HeaterWeeklyEvent> adjustedSchedule)
        {
            //https://internalapi.myuplink.com/v2/devices/HOIAX_3083989de217_35f19927-203c-4a6b-a84b-9d1c1a9b8d6c/weekly-schedules
            //[{"weeklyScheduleId":0,"weekFormat":"mon,tue,wed,thu,fri,sat,sun","events":[{"enabled":true,"modeId":55,"startDay":"Monday","startTime":"00:00:00","stopDay":null,"stopTime":null,"phantom_id":"7f44f57a-dba7-4f38-8e29-6cbb1acc1434"},{"enabled":true,"modeId":55,"startDay":"Monday","startTime":"22:30:00","stopDay":null,"stopTime":null,"phantom_id":"35159bc7-27d6-426c-b913-f38c6b80d060"},{"enabled":true,"modeId":55,"startDay":"Tuesday","startTime":"00:00:00","stopDay":null,"stopTime":null,"phantom_id":"ef1040a2-0755-4aca-81b1-d3f035ab3cfa"},{"enabled":true,"modeId":51,"startDay":"Tuesday","startTime":"05:30:00","stopDay":null,"stopTime":null,"phantom_id":"f783ef57-1230-4525-97e6-219c7f84e303"},{"enabled":true,"modeId":50,"startDay":"Tuesday","startTime":"13:00:00","stopDay":null,"stopTime":null,"phantom_id":"c16c8404-adc2-45b8-b34f-86a963686a1c"},{"enabled":true,"modeId":55,"startDay":"Tuesday","startTime":"22:30:00","stopDay":null,"stopTime":null,"phantom_id":"60eee2c0-5546-49f8-b134-5b9931174559"},{"enabled":true,"modeId":55,"startDay":"Wednesday","startTime":"00:00:00","stopDay":null,"stopTime":null,"phantom_id":"4194a325-2c25-4378-ad87-65dd86b6aa6e"},{"enabled":true,"modeId":51,"startDay":"Wednesday","startTime":"05:30:00","stopDay":null,"stopTime":null,"phantom_id":"750fbcea-f617-42dc-af87-3da947d477fa"},{"enabled":true,"modeId":50,"startDay":"Wednesday","startTime":"13:00:00","stopDay":null,"stopTime":null,"phantom_id":"7d88f42f-4240-4d73-bdff-b5723fa47767"},{"enabled":true,"modeId":55,"startDay":"Wednesday","startTime":"22:30:00","stopDay":null,"stopTime":null,"phantom_id":"1fd99685-6c1e-4dce-83f8-f032ab91840d"},{"enabled":true,"modeId":55,"startDay":"Thursday","startTime":"00:00:00","stopDay":null,"stopTime":null,"phantom_id":"e2d03160-afbd-49fe-a585-7d5392fcc602"},{"enabled":true,"modeId":51,"startDay":"Thursday","startTime":"05:30:00","stopDay":null,"stopTime":null,"phantom_id":"62312750-eee0-4369-9e0c-b4d1a7cb36b3"},{"enabled":true,"modeId":50,"startDay":"Thursday","startTime":"13:00:00","stopDay":null,"stopTime":null,"phantom_id":"0ecb4a21-7227-410b-9c4e-57376a7dcf4a"},{"enabled":true,"modeId":55,"startDay":"Thursday","startTime":"22:30:00","stopDay":null,"stopTime":null,"phantom_id":"bec1bde5-ad64-479e-a7f3-60b5676587a6"},{"enabled":true,"modeId":55,"startDay":"Friday","startTime":"00:00:00","stopDay":null,"stopTime":null,"phantom_id":"483c1d7a-0dba-4ba9-b45a-4b60c68902db"},{"enabled":true,"modeId":51,"startDay":"Friday","startTime":"05:30:00","stopDay":null,"stopTime":null,"phantom_id":"bf2fb3d1-6def-4df3-9c42-9a3e4344dba1"},{"enabled":true,"modeId":50,"startDay":"Friday","startTime":"13:00:00","stopDay":null,"stopTime":null,"phantom_id":"8a198900-c72c-4b8b-9300-e4a125e04c70"},{"enabled":true,"modeId":55,"startDay":"Friday","startTime":"22:30:00","stopDay":null,"stopTime":null,"phantom_id":"eb65a4e8-fa00-4156-a136-c20b7eab273c"},{"enabled":true,"modeId":55,"startDay":"Saturday","startTime":"00:00:00","stopDay":null,"stopTime":null,"phantom_id":"41b4f930-d3e6-4dab-ba60-8da86e4b6edb"},{"enabled":true,"modeId":51,"startDay":"Saturday","startTime":"05:30:00","stopDay":null,"stopTime":null,"phantom_id":"5aa757fa-513f-4ced-b707-96eac0700e75"},{"enabled":true,"modeId":50,"startDay":"Saturday","startTime":"13:00:00","stopDay":null,"stopTime":null,"phantom_id":"4aa697d7-4c9b-4d68-9aac-a0cf6a39073e"},{"enabled":true,"modeId":55,"startDay":"Saturday","startTime":"22:30:00","stopDay":null,"stopTime":null,"phantom_id":"06d75e88-eb4e-4fc7-a332-1dd6e1325fc3"},{"enabled":true,"modeId":55,"startDay":"Sunday","startTime":"00:00:00","stopDay":null,"stopTime":null,"phantom_id":"20af009b-ba0d-4d5c-b3ff-1d6f5beef116"},{"enabled":true,"modeId":51,"startDay":"Sunday","startTime":"05:30:00","stopDay":null,"stopTime":null,"phantom_id":"4a9b8c88-42f5-459c-8fa0-e040dd1bfd39"},{"enabled":true,"modeId":50,"startDay":"Sunday","startTime":"13:00:00","stopDay":null,"stopTime":null,"phantom_id":"6dc691f1-5a87-4c0f-a60b-70b2d9db69a6"},{"enabled":true,"modeId":55,"startDay":"Sunday","startTime":"22:30:00","stopDay":null,"stopTime":null,"phantom_id":"e2c3594c-fc7b-477b-90f3-755fc83c0ff8"},{"enabled":true,"phantom_id":"7e699500-ff74-4c82-af28-b571af79a0ba","modeId":53,"startDay":"Monday","startTime":"05:30:00"},{"enabled":true,"phantom_id":"517bc6dc-0fb5-4b21-b5de-0b377ff7e27d","modeId":54,"startDay":"Monday","startTime":"13:00:00"}]}]
            _heaterRoot.First().events = adjustedSchedule.ToList();

            var request = new RestRequest($"/v2/devices/{device.id}/weekly-schedules") { Method = Method.Put };
            request.AddJsonBody<IEnumerable<HeaterWeeklyRoot>>(_heaterRoot);

            var tResponse = await _httpClient.ExecuteAsync(request);
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && tResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
                return true;

            return false;
        }

        public async Task<List<WaterHeaterMode>> GetCurrentModes(Device device)
        {
            //https://internalapi.myuplink.com/v2/devices/HOIAX_3083989de217_35f19927-203c-4a6b-a84b-9d1c1a9b8d6c/schedule-modes
            //Put
            //[{"modeId":50,"name":"M1 (50C, 700W)","settings":[{"settingId":1,"value":50},{"settingId":2,"value":1}]},{"modeId":51,"name":"M2 (50C, 0W)","settings":[{"settingId":1,"value":50},{"settingId":2,"value":0}]},{"modeId":52,"name":"M3 (65C, 2000W)","settings":[{"settingId":1,"value":65},{"settingId":2,"value":3}]},{"modeId":53,"name":"M4 (70C, 700W)","settings":[{"settingId":1,"value":70},{"settingId":2,"value":1}]},{"modeId":54,"name":"M5 (70C, 1300W)","settings":[{"settingId":1,"value":70},{"settingId":2,"value":2}]},{"modeId":55,"name":"M6 (70C, 2000W)","settings":[{"settingId":1,"value":"71"},{"settingId":2,"value":3}]}]
            var request = new RestRequest($"/v2/devices/{device.id}/schedule-modes") { Method = Method.Get };
            var tResponse = await _httpClient.ExecuteAsync(request);
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
            {
                var devices = JsonSerializer.Deserialize<List<WaterHeaterMode>>(tResponse.Content);
                return devices;
            }

            return Array.Empty<WaterHeaterMode>().ToList();
        }

        public async Task<bool> SetCurrentModes(Device device, IEnumerable<WaterHeaterMode> modes)
        {
            //            //Put
            //https://internalapi.myuplink.com/v2/devices/HOIAX_3083989de217_35f19927-203c-4a6b-a84b-9d1c1a9b8d6c/schedule-modes
            var request = new RestRequest($"/v2/devices/{device.id}/schedule-modes") { Method = Method.Put };
            request.AddJsonBody<IEnumerable<WaterHeaterMode>>(modes);
            var tResponse = await _httpClient.ExecuteAsync(request);
            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK  && tResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return true;
            }

            return false;
        }
    }
}
