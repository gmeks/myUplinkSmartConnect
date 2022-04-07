using myUplink.Models;
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
    internal class Login
    {
        RestClient _httpClient;
        AuthToken? _token;
        const string _tokenFile = "tokenfile.json";

        public Login()
        {
            string url = "https://api.myuplink.com";
            _httpClient = new RestClient(url);
        }

        public async Task<bool> LoginToApi(string clientIdentifier,string clientSecret)
        {
            //https://api.myuplink.com/oauth/authorize?response_type=code&client_id=qwerty-123456-4072-b881&scope=READSYSTEM WRITESYSTEM offline_access&redirect_uri=http://validurl.local:1234&state=x
            //var client = new RestClient("https://api.myuplink.com/oauth/token");
            //client.Authenticator = new HttpBasicAuthenticator("client-app", "secret");

            if (File.Exists(_tokenFile))
            {
                _token = JsonSerializer.Deserialize<AuthToken>(File.ReadAllText(_tokenFile));
                if(_token != null && !_token.IsExpired)
                {
                    _httpClient.AddDefaultHeader("authorization", "Bearer " + _token.access_token);

                    var verifyToken = await Ping();
                    if(!verifyToken)
                    {
                        _token = null;
                    }
                }
            }

            if(_token == null || _token.IsExpired)
            {
                var request = new RestRequest("oauth/token") { Method = Method.Post };
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("client_id", clientIdentifier);
                request.AddParameter("client_secret", clientSecret);
                request.AddParameter("grant_type", "client_credentials");
                var tResponse = await _httpClient.ExecuteAsync(request);

                if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
                {
                    _token = JsonSerializer.Deserialize<AuthToken>(tResponse.Content);
                    if (_token != null)
                    {
                        string url = "https://api.myuplink.com";
                        _httpClient = new RestClient(url);
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

            if (tResponse.StatusCode ==  System.Net.HttpStatusCode.OK || tResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
                return true;

            return false;
        }

        public async Task<IEnumerable<myUplinkSystem>> GetUserSystems()
        {
            var request = new RestRequest("/v2/systems/me") { Method = Method.Get };
            var tResponse = await _httpClient.ExecuteAsync(request);

            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
            {
                var devices = JsonSerializer.Deserialize<RootDevices>(tResponse.Content);
                return devices.systems;
            }

            return Array.Empty<myUplinkSystem>();
        }

        public async Task<IEnumerable<DeviceInfo>> GetDeviceInfo(string deviceId)
        {
            var request = new RestRequest($"/v2/devices/{deviceId}") { Method = Method.Get };
            var tResponse = await _httpClient.ExecuteAsync(request);

            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
            {
                var devices = JsonSerializer.Deserialize<DeviceInfo[]>(tResponse.Content);
                return devices;
            }

            return Array.Empty<DeviceInfo>();
        }

        public async Task<IEnumerable<DeviceInfo>> GetDeviceInfoPoints(string deviceId)
        {
            var request = new RestRequest($"/v2/devices/{deviceId}/points") { Method = Method.Get };
            var tResponse = await _httpClient.ExecuteAsync(request);

            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(tResponse.Content))
            {
                var devices = JsonSerializer.Deserialize<DeviceInfo[]>(tResponse.Content);
                return devices;
            }

            return Array.Empty<DeviceInfo>();
        }
    }
}
