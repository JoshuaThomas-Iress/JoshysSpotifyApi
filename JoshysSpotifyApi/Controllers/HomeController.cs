using System.Collections.Generic;
using System.Web;
using Main.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nest;
using Newtonsoft.Json.Linq;
using static System.Net.WebRequestMethods;

namespace Main.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;


        private readonly SharedAuthHome _sharedAuthHome;

        public HomeController(IConfiguration configuration,
                              IHttpClientFactory httpClientFactory,
                              ILogger<HomeController> logger,
                              SharedAuthHome sharedAuthHome)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _sharedAuthHome = sharedAuthHome;
        }

        public IActionResult Index()
        {


            return View();
        }

        [HttpGet]
        public async Task<string> Get_User_Id()
        {



            string endpointUrl = "https://api.spotify.com/v1/me";


            JObject userProfile = await _sharedAuthHome.CallSpotifyApiAsync(endpointUrl);

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

            var response = await _sharedAuthHome.CallSpotifyApiAsync(endpointUrl);

            ViewBag.Playlists_Name = response["items"];
            ViewBag.Playlists_Total = response["total"];

            return View("Get_Playlists");

        }







        [HttpGet]
        public async Task<IActionResult> Get_Podcasts(string query)
        {
            List<ShowModel> shows = new List<ShowModel>();

            if (!string.IsNullOrEmpty(query))
            {
                string Encoded_Query = HttpUtility.UrlEncode(query);
                string endpointUrl = $"https://api.spotify.com/v1/search?q={Encoded_Query}&type=show";

                var response = await _sharedAuthHome.CallSpotifyApiAsync(endpointUrl);

                _logger.LogInformation("--- GET_PODCASTS (GET) METHOD HIT ---");

                int count = 0;

                foreach (var item in response["shows"]?["items"])
                {
                    var show = new ShowModel
                    {
                        Id = new List<string> { item["id"]?.ToString() },
                        Name = item["name"]?.ToString(),
                        Description = item["description"]?.ToString()
                    };


                    if (show.Id != null && show.Id.Count > 0)
                    {


                        foreach (string Id in show.Id)
                        {
                            
                            var Episodes = await Get_Podcasts_Episodes(Id);
                        }

                    }
                   

                    shows.Add(show);

                    if (++count == 5) break;
                }
            }

            return View("Get_Podcasts", shows);
        }



        private async Task<string> Get_Podcasts_Episodes(string ids)
        {
            string episodes =  "";



            string endpointUrl = $"https://api.spotify.com/v1/shows/{ids}/episodes?limit=5";
            var response = await _sharedAuthHome.CallSpotifyApiAsync(endpointUrl);

           
                // Fix: Cast 'response["episodes"]?["items"]' to JArray to allow indexing
                foreach (var item in (response["episodes"]?["items"] as JArray) ?? new JArray())
                {
                    var episode = new EpisodeModel
                    {
                        Name = item["name"]?.ToString(),
                        Description = item["description"]?.ToString()
                    };
                }
                return episodes;

        }
            

    }
}


