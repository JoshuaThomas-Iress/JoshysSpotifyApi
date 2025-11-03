using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Main.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
           

            return View();
        }

        [HttpGet]
        public async Task<string> Get_User_Id()
        {
            var httpClient = new HttpClient();

            string Access_Token = HttpContext.Session.GetString("SpotifyAccessToken");
            string Refresh_Token = HttpContext.Session.GetString("SpotifyRefreshToken");

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Access_Token);
            using HttpResponseMessage response = await httpClient.GetAsync("https://api.spotify.com/v1/me");

            string endpointUrl = "https://api.spotify.com/v1/me";

            
            JObject userProfile = await CallSpotifyApiAsync(endpointUrl);
            
            string User_Id = userProfile["id"]?.ToString();
            
            if (User_Id == null)
            {
                throw new Exception();
            }    
            return User_Id;
        }


        [HttpGet]
        [Route("/Playlist/Get_Playlists")]
        public async Task<IActionResult> Get_Playlists()
        {


            string User_Id = await Get_User_Id();

            string endpointUrl = $"https://api.spotify.com/v1/users/{User_Id}/playlists";

            TempData["UserId"] = User_Id;

            var response = await CallSpotifyApiAsync(endpointUrl);

            ViewBag.Playlists_Name = response["items"];
            ViewBag.Playlists_Total = response["total"];

            return View("Get_Playlists");

        }




        [HttpGet]   
        private async Task<JObject> CallSpotifyApiAsync(string endpointUrl)
        {

            string Access_Token = HttpContext.Session.GetString("SpotifyAccessToken");
            string Refresh_Token = HttpContext.Session.GetString("SpotifyRefreshToken");


            var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Access_Token);
            using HttpResponseMessage response = await httpClient.GetAsync($"{endpointUrl}");
            if (response.IsSuccessStatusCode)
            {
                string UserPlaylists = await response.Content.ReadAsStringAsync();


                TempData["UserPlaylists"] = UserPlaylists;


                var ResponseJson = JObject.Parse(UserPlaylists);


                return (ResponseJson);

            }
            else
            {
                throw new Exception($"Failed to call Spotify API: {response.StatusCode}");
            }
        }

    }
}

