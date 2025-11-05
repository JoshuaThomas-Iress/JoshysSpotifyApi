using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Framework;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging; // Add this using directive

namespace Main.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger; // Add logger field

        public HomeController(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<HomeController> logger) // Add logger parameter
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger; // Assign logger
        }

        public IActionResult Index()
        {


            return View();
        }

        [HttpGet]
        public async Task<string> Get_User_Id()
        {
            var httpClient = _httpClientFactory.CreateClient();

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


            var httpClient = _httpClientFactory.CreateClient();

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
        [HttpGet]

        public async Task<IActionResult> Get_Podcasts()
        {
            return View("Get_Podcasts");
        }

        [HttpPost]
        public async Task<IActionResult> Get_Podcasts(string query)
        {
            // Call Spotify API to search for tracks, albums, or artists
            string Encoded_Query = HttpUtility.UrlEncode(query);
            string endpointUrl = $"https://api.spotify.com/v1/search?q={Encoded_Query}&type=show";
            var response = await CallSpotifyApiAsync(endpointUrl);

            // Pass the search results to the view
            ViewBag.PodcastSearchResults = response["shows"]["items"];
            _logger.LogInformation("--- GET_PODCASTS (GET) METHOD HIT ---"); // Use _logger instead of ILogger
            return View("Get_Podcasts");
        }
    }
}





