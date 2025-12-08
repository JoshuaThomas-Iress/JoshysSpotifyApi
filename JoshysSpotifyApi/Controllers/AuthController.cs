using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using Main.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Main.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly SpotifyService _spotifyService;
  

        public AuthController(IConfiguration configuration,
                              IHttpClientFactory httpClientFactory,
                              ILogger<AuthController> logger,
                     
                              SpotifyService spotifyService)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _spotifyService = spotifyService;
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
            var scope = HttpUtility.UrlEncode("user-read-private playlist-modify-public playlist-modify-private user-read-email playlist-read-collaborative");

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
                    var (refreshToken, accessToken, responseJson) = await _spotifyService.Access_Token_Process(code, ClientCredentials);


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



    }

        


}

