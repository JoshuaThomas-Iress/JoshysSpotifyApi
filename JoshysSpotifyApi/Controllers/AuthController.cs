using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nancy.Json;
using Newtonsoft.Json.Linq;
using System.Web; 

namespace Main.Controllers
{
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Login()
        {
            var clientId = _configuration["Spotify:ClientId"];

            var redirectUri = "https://127.0.0.1:7071/callback";
            const string scope = "user-read-private playlist-modify-public";

            var spotifyUrl = $"https://accounts.spotify.com/authorize?client_id={clientId}&response_type=code&redirect_uri={redirectUri}&scope={scope}";

            return Redirect(spotifyUrl);
        }
        [Route("/callback")]
        public async Task<IActionResult> Callback(string code, string error)
        {
            if (error != null)
            {
                return Content($"Error during authentication: {error}");
            }
            if (code != null)
            {
                TempData["LoginStatus"] = "Code caught";
                var clientId = _configuration["Spotify:ClientId"];
                var clientSecret = _configuration["Spotify:ClientSecret"];

                string clientCredentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

                if (error != null)
                {
                    return Content($"Access denied: {error}");

                }

                var httpClient = new HttpClient();
                try
                {
                    var requestData = new Dictionary<string, string>
                    {
                        { "grant_type", "authorization_code" },
                        { "code", code },
                        { "redirect_uri", "https://127.0.0.1:7071/callback" }
                    };

                    var requestContent = new FormUrlEncodedContent(requestData);

                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", clientCredentials);

                    using HttpResponseMessage response = await httpClient.PostAsync("https://accounts.spotify.com/api/token", requestContent);
                    
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var Access_Token = JObject.Parse(responseContent);

                    string ResponseString = new JavaScriptSerializer().Serialize(responseContent);
                    HttpContext.Session.SetString("SpotifyAccessToken", ResponseString);
                    TempData["LoginStatus"] = ResponseString;

                    return RedirectToAction("Index", "Home");

                }
                catch (Exception ex)
                {
                    return Content($"Error during token retrieval: {ex.Message}");
                }
                
            }
            return RedirectToAction("Index", "Home");
        }
    }
}