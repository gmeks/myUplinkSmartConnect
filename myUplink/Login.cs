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
        AuthToken _token;

        public Login()
        {

        }

        public async Task<bool> LoginToApi(string clientIdentifier,string clientSecret)
        {
            //https://api.myuplink.com/oauth/authorize?response_type=code&client_id=qwerty-123456-4072-b881&scope=READSYSTEM WRITESYSTEM offline_access&redirect_uri=http://validurl.local:1234&state=x

            //var client = new RestClient("https://api.myuplink.com/oauth/token");
            //client.Authenticator = new HttpBasicAuthenticator("client-app", "secret");


            string url = "https://api.myuplink.com";
            //request token
            _httpClient = new RestClient(url);
            var request = new RestRequest("oauth/token") { Method = Method.Post };
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("client_id", clientIdentifier);
            request.AddParameter("client_secret", clientSecret);
            request.AddParameter("grant_type", "client_credentials");
            var tResponse = await _httpClient.ExecuteAsync(request);
            _token = JsonSerializer.Deserialize<AuthToken>(tResponse.Content);

            _httpClient.AddDefaultHeader("authorization", "Bearer " + _token.access_token);
            return true;
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

            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK || tResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                var devices = JsonSerializer.Deserialize<RootDevices>(tResponse.Content);
                return devices.systems;
            }

            return Array.Empty<myUplinkSystem>();
        }

        public async Task<IEnumerable<myUplinkSystem>> GetUserSystems(string deviceId)
        {
            var request = new RestRequest($"/v2/devices/{deviceId}/points") { Method = Method.Get };
            var tResponse = await _httpClient.ExecuteAsync(request);

            if (tResponse.StatusCode == System.Net.HttpStatusCode.OK || tResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                var devices = JsonSerializer.Deserialize<RootDevices>(tResponse.Content);
                return devices.systems;
            }

            return Array.Empty<myUplinkSystem>();
        }
    }
}
