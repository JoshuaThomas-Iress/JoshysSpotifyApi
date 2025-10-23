using System.Net.Http;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Nancy.Diagnostics;
using Newtonsoft.Json.Linq;

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


        [HttpGet]
        [Route("/callback")]
        public async Task<IActionResult> Callback(string code, string error)
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

                var httpClient = new HttpClient();

                try
                {
                    // Rename the local variable to avoid CS0136 error
                    var (refreshTokenOutput, accessToken, responseJson) = await Access_Token_Process(code, ClientCredentials);


                    //For testing
                    string ResponseJson_Temp = responseJson.ToString();
                    var Access_Token_Temp = accessToken;
                    var Refresh_Token_Temp = refreshTokenOutput;

                    TempData["ResponseJson"] = ResponseJson_Temp;
                    TempData["Access_Token"] = Access_Token_Temp;
                    TempData["Refresh_Token"] = Refresh_Token_Temp;
                }
                catch (Exception ex)
                {
                    return Content($"Error during token retrieval: {ex.Message}");
                }

                return RedirectToAction("Index", "Home");
            }
            

            return RedirectToAction("Index", "Home");
        }

        

        [HttpPost]
        public async Task<(string Refresh_Token, string Access_Token, JObject ResponseJson)> Access_Token_Process(string code, string clientCredentials)
        {

            var httpClient = new HttpClient();

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
            var ResponseJson = JObject.Parse(responseContent);
            string Access_Token = ResponseJson["access_token"].ToString();
            string Refresh_Token_Output = ResponseJson["refresh_token"].ToString();

            return (Refresh_Token_Output, Access_Token, ResponseJson);
        }

        [HttpPost]
        public async Task<(string Refresh_Token, string Access_Token)> Refresh_Token_Process(string refreshToken)
        {

            var RequestDataBody = new Dictionary<string, string>
                    {
                        { "grant_type", "refresh_token" },
                        { "refresh_token", refreshToken }
                    };
            var requestContent = new FormUrlEncodedContent(RequestDataBody);

            // FIX: Instantiate HttpClient instead of using the class name
            using var httpClient = new HttpClient();
            using HttpResponseMessage response = await httpClient.PostAsync("https://accounts.spotify.com/api/token", requestContent);

            var responseContent = await response.Content.ReadAsStringAsync();
            var ResponseJson = JObject.Parse(responseContent);

            var Access_Token = ResponseJson["access_token"].ToString();
            var Refresh_Token = ResponseJson["refresh_token"].ToString();

            return (Refresh_Token, Access_Token);
        }

        
    }

}
