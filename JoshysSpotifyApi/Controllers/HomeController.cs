using Main.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Web;
using Main.Services;

namespace Main.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;


     
        private readonly SpotifyService _spotifyService;

        public HomeController(IConfiguration configuration,
                              IHttpClientFactory httpClientFactory,
                              ILogger<HomeController> logger,
                      
                              SpotifyService spotifyService)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
       
            _spotifyService = spotifyService;
        }

        public IActionResult Index()
        {


            return View();
        }

        


        [HttpGet]
        [Route("/Playlist/Get_Playlists")]
        public async Task<IActionResult> Get_Playlists()
        {


            string User_Id = await _spotifyService.Get_User_Id();

            var PlaylistViewModel = await _spotifyService.Get_Playlists_Shared(User_Id);

            return View("Get_Playlists", PlaylistViewModel);

        }

        [Route("/Home/Delete_Playlists")]
        [HttpGet]
        public IActionResult Delete_Playlist()
        {
            return View("Delete_Playlists");
        }

       
        [HttpGet] 
        public async Task<IActionResult> Get_Tracks_To_Query_For_Deletion(string User_Query)
        {
            string User_Id = await _spotifyService.Get_User_Id();

            var PlaylistViewModel = await _spotifyService.Get_Playlists_Shared(User_Id);

            var MyItemModel = await _spotifyService.Retrive_PlaylistItemModel(User_Query);

  
            var Uri_JObject = await _spotifyService.Create_Uri_JObject(MyItemModel.playlist_tracks_JObject);

            var Uri_Dictionary = await _spotifyService.Convert_JObject_To_Dictionary(Uri_JObject);

            var Tracks_Dictionary = Uri_Dictionary.Except(MyItemModel.NameUriKey).Concat(MyItemModel.NameUriKey.Except(Uri_Dictionary));

            var tracks = await _spotifyService.Convert_Dictionary_To_JObject(Tracks_Dictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

            MyItemModel.playlist_tracks_JObject = new JObject();

            foreach (var item in tracks)
            {


                MyItemModel.playlist_tracks_JObject.Add(item.Key, item.Value);


            }
           
            JObject testtracks = MyItemModel.playlist_tracks_JObject;

            return Content(MyItemModel.playlist_tracks_JObject.ToString(), "application/json");
        }

     
           

        public async Task<IActionResult> Song_Query(string User_Query)
        {
            PlaylistItemModel MyItemModel = new PlaylistItemModel();
            MyItemModel.playlist_tracks_JObject = new JObject();

            foreach (string track in User_Query.Trim().Split(','))
            {
                if (MyItemModel.playlist_tracks_JObject.ContainsKey(track))
                {
                    MyItemModel.playlist_tracks_JObject.Remove(track);
                    return View(MyItemModel.playlist_tracks_JObject);
                }
            }
            return View("Delete_Playlists",MyItemModel.playlist_tracks_JObject);
        }
        





        [HttpPost]
        [Route("/Delete_Playlists")]
        public async Task<IActionResult> Delete_Item_Playlist(List<string>? User_Query_Tracks, string Playlist_Id)
        {
            PlaylistItemModel MyItemModel = new PlaylistItemModel();
            
            var tracks = MyItemModel.playlist_tracks_JObject;

            string endpointUrl = $"https://api.spotify.com/v1/playlists/{Playlist_Id}/{tracks}";

            var response = await _spotifyService.CallSpotifyApiAsync(endpointUrl);
             
            return View();
        }
            
            
            
            
            
            

          



        
        
        
        [HttpGet]
        [Route("/GetPlaylistitems")]
        public async Task<IActionResult> Get_Playlist_Items(string PlaylistId)
        {

            var PlaylistItemsViewModel = await _spotifyService.Get_Playlists_Shared(PlaylistId);

            return View("Get_Playlist_Items", PlaylistItemsViewModel);

        }




        //[HttpPut]
        //public async Task<IActionResult> Update_Playlist_Details() { }



        //[HttpPost]
        //public async Task<IActionResult> Add_Song_To_Playlist(string playlistName, string playlistDescription)
        //{

        //}






        [HttpGet]
        public async Task<IActionResult> Get_Podcasts(string query)
        {
            List<ShowModel> shows = new List<ShowModel>();

            if (!string.IsNullOrEmpty(query))
            {
                string Encoded_Query = HttpUtility.UrlEncode(query);
                string endpointUrl = $"https://api.spotify.com/v1/search?q={Encoded_Query}&type=show&limit=5";

                var response = await _spotifyService.CallSpotifyApiAsync(endpointUrl);

                _logger.LogInformation("--- GET_PODCASTS (GET) METHOD HIT ---");

                int count = 0;

                foreach (var item in response["shows"]?["items"])
                {
                    string id = item["id"]?.ToString();

                    var (EpisodeNames, EpisodeDescriptions) = await _spotifyService.Get_Podcasts_Episodes(id);

                    var show = new ShowModel
                    {
                        Id = new List<string> { id },
                        Name = item["name"]?.ToString(),
                        Description = item["description"]?.ToString(),
                        Episodes = new List<EpisodeModel>()
                    };
                    for (int i = 0; i < EpisodeNames.Count; i++)
                    {
                        show.Episodes.Add(new EpisodeModel
                        {
                            Name = EpisodeNames[i],

                        });
                    }

                    shows.Add(show);

                    if (++count == 5) break;
                }
            }

            return View("Get_Podcasts", shows);
        }

       


    }
}
