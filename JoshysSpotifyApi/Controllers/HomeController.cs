using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Nancy;
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
            ViewBag.StatusMessage = TempData["ResponseJson"];
            ViewBag.StatusMessage = TempData["Access_Token"];
            ViewBag.StatusMessage = TempData["Refresh_Token"];

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

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                return (Refresh_Token);
            }
            else
            {
                throw new Exception("Failed to retrieve user id from Spotify API.");
            }
        }


        [HttpGet]
        [Route("/Playlist/Get_Playlists")]
        public async Task<IActionResult> Get_Playlists()
        {
            


            string Access_Token = HttpContext.Session.GetString("SpotifyAccessToken");
            string Refresh_Token = HttpContext.Session.GetString("SpotifyRefreshToken");


            var httpClient = new HttpClient();
            string User_Id = await Get_User_Id();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Access_Token);
            using HttpResponseMessage response = await httpClient.GetAsync($"https://api.spotify.com/v1/users/{User_Id}/playlists");

            string  UserPlaylists = await response.Content.ReadAsStringAsync();


            TempData["UserPlaylists"] = UserPlaylists;
            TempData["UserId"] = User_Id;





            return View("Get_Playlists");

        }
    
    }
}

