using Main.Controllers;
using Main.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Web;

namespace Main.Services
{
    public class AuthSpotifyService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;
        private readonly SpotifyService _spotifyService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // ✅ We only keep the Interface here
        private readonly ISpotifyHTTPClient _client;

        public AuthSpotifyService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<HomeController> logger,
            SpotifyService spotifyService,
            IHttpContextAccessor httpContextAccessor,
            ISpotifyHTTPClient client) 
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _spotifyService = spotifyService;
            _httpContextAccessor = httpContextAccessor;
            _client = client;
        }

        public string login_Helper()
        {
            var redirectUri = HttpUtility.UrlEncode("https://127.0.0.1:7071/callback");
            var scope = HttpUtility.UrlEncode("user-read-private playlist-modify-public playlist-modify-private user-read-email playlist-read-collaborative");

            var clientId = _configuration["Spotify:ClientId"];

            var spotifyUrl = $"https://accounts.spotify.com/authorize?client_id={clientId}&response_type=code&redirect_uri={redirectUri}&scope={scope}";
            return spotifyUrl;
        }

        public async Task<string> Callback_Helper(string code, string error)
        {
            if (error != null)
            {
                return Content($"Error during authentication: {error}");
            }
            else if (code != null)
            {
                var clientId = _configuration["Spotify:ClientId"];
                var clientSecret = _configuration["Spotify:ClientSecret"];

                string ClientCredentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

                var httpClient = _httpClientFactory.CreateClient();

                try
                {
                    var (refreshToken, accessToken, responseJson) = await _spotifyService.Access_Token_Process(code, ClientCredentials);

                    var httpContext = _httpContextAccessor.HttpContext;
                    if (httpContext != null && httpContext.Session != null)
                    {
                        httpContext.Session.SetString("SpotifyAccessToken", accessToken);
                        httpContext.Session.SetString("SpotifyRefreshToken", refreshToken);
                    }

                    if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                    {
                        return Content("Failed to retrieve tokens from Spotify.");
                    }
                }
                catch (Exception ex)
                {
                    return Content($"Error during token retrieval: {ex.Message}");
                }
            }
            return string.Empty;
        }

        private string Content(string message)
        {
       
            return message;
        }
    }
}