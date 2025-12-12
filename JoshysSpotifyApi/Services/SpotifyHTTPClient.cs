using Main.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
//MOVE ALL TO SERVICES
namespace Main.Services
{
    public class SpotifyHTTPClient
    {
        private readonly ILogger<SpotifyHTTPClient> _logger;
        private readonly HttpClient _client;
     

        public SpotifyHTTPClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<SpotifyHTTPClient> logger)
        {
            var session = httpContextAccessor?.HttpContext?.Session ?? throw new InvalidOperationException("HTTP context is not available.");

            var accessToken = session.GetString("SpotifyAccessToken");


            _client = httpClientFactory.CreateClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            _logger = logger;
           
        
           
        }

        public async Task<string> Get(string endpointUrl)
        {
            var response = await _client.GetAsync($"{endpointUrl}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError("--- Error ---");
                throw new Exception("Unauthaorized");
            }

            throw new Exception("Fell out of if statement most likely null");
        }

        public async Task<HttpResponseMessage> Post(string endpointUrl, Dictionary<string, string> requestData, string? Header, bool bearerBool, string clientCredentials)
        {
            var requestContent = new FormUrlEncodedContent(requestData);

            if (bearerBool = true)
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", clientCredentials);
                var response = await _client.PostAsync(endpointUrl, requestContent);

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogError("--- Error ---");
                    throw new Exception("Unauthaorized");
                }
            }
            else
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Header);
                var response = await _client.PostAsync(endpointUrl, requestContent);

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogError("--- Error ---");
                    throw new Exception("Unauthaorized");
                }
            }

             

            

            throw new Exception("Fell out of if statement");
        }
    }

}

