using System.Net.Http;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Main.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Login(bool? fromPlaylist = false)
        {



            if (fromPlaylist == true)
            {
                string spotifyUrl = Login_Helper();
                TempData["ReturnUrl"] = "Playlist";
                return Redirect(spotifyUrl);

            }
            else
            {
                string spotifyUrl = Login_Helper();
                return Redirect(spotifyUrl);
            }


        }

        private string Login_Helper()
        {
            var redirectUri = HttpUtility.UrlEncode("https://127.0.0.1:7071/callback");
            var scope = HttpUtility.UrlEncode("user-read-private playlist-modify-public user-read-private user-read-email playlist-read-collaborative");

            var clientId = _configuration["Spotify:ClientId"];

            var spotifyUrl = $"https://accounts.spotify.com/authorize?client_id={clientId}&response_type=code&redirect_uri={redirectUri}&scope={scope}";

            return spotifyUrl;
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

                var httpClient = _httpClientFactory.CreateClient();

                try
                {
                    // Rename the local variable to avoid CS0136 error
                    var (refreshToken, accessToken, responseJson) = await Access_Token_Process(code, ClientCredentials);


                    //For testing
                    string ResponseJson_Temp = responseJson.ToString();
                    var Access_Token_Temp = accessToken;
                    var Refresh_Token_Temp = refreshToken;

                    //TempData["ResponseJson"] = ResponseJson_Temp;
                    //TempData["Access_Token"] = Access_Token_Temp;
                    TempData["Refresh_Token"] = Refresh_Token_Temp;


                    HttpContext.Session.SetString("SpotifyAccessToken", accessToken);
                    HttpContext.Session.SetString("SpotifyRefreshToken", refreshToken);

                    if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                    {

                        return Content("Failed to retrieve tokens from Spotify.");
                    }

                }
                catch (Exception ex)
                {
                    return Content($"Error during token retrieval: {ex.Message}");
                }

                if (TempData["ReturnUrl"] != null && TempData["ReturnUrl"].ToString() == "Playlist")

                {
                    return RedirectToAction("Get_Playlists", "Home");
                }



                return RedirectToAction("Index", "Home");
            }


            return RedirectToAction("Index", "Home");
        }



        [HttpPost]
        public async Task<(string Refresh_Token, string Access_Token, JObject ResponseJson)> Access_Token_Process(string code, string clientCredentials)
        {

            var httpClient = _httpClientFactory.CreateClient();

            var requestData = new Dictionary<string, string>
                    {
                        { "grant_type", "authorization_code" },
                        { "code", code },
                        { "redirect_uri", "https://127.0.0.1:7071/callback" }
                    };


                var requestContent = new FormUrlEncodedContent(requestData);

                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", clientCredentials);
       
                using HttpResponseMessage response = await httpClient.PostAsync("https://accounts.spotify.com/api/token", requestContent);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var ResponseJson = JObject.Parse(responseContent);
                string Access_Token = ResponseJson["access_token"].ToString();
                string Refresh_Token_Output = ResponseJson["refresh_token"].ToString();

                return (Refresh_Token_Output, Access_Token, ResponseJson);
            }
            else
            {
                return (null, null, null);
            }
        }

        [HttpPost]
        public async Task<(string Refresh_Token, string Access_Token)> Refresh_Token_Process(string refreshToken)
        {

            Console.WriteLine("helloworld");

            var RequestDataBody = new Dictionary<string, string>
                    {
                        { "grant_type", "refresh_token" },
                        { "refresh_token", refreshToken }
                    };
            var requestContent = new FormUrlEncodedContent(RequestDataBody);


            var httpClient = _httpClientFactory.CreateClient();
            using HttpResponseMessage response = await httpClient.PostAsync("https://accounts.spotify.com/api/token", requestContent);

            var responseContent = await response.Content.ReadAsStringAsync();
            var ResponseJson = JObject.Parse(responseContent);

            var Access_Token = ResponseJson["access_token"].ToString();
            var Refresh_Token = ResponseJson["refresh_token"].ToString();

            return (Refresh_Token, Access_Token);
        }


    }


}
