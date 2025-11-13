using System.Web;
using Main.Models;
using Microsoft.AspNetCore.Mvc;
using Nest;
using Newtonsoft.Json.Linq;

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

            var PlaylistViewModel = await _sharedAuthHome.Get_Playlists_Shared(User_Id);

            return View("Get_Playlists", PlaylistViewModel);

        }





        [Route("/Delete_Playlist")]

        public async Task<IActionResult> Get_Tracks_To_Query_For_Deletion(string User_Query)
        {
            string User_Id = await Get_User_Id();

            var PlaylistViewModel = await _sharedAuthHome.Get_Playlists_Shared(User_Id);

            foreach (var item in PlaylistViewModel.Playlists)
            {
                item.NameIdKey.Add(item.Name, item.Id);
            }
            string FoundName = null;
            string FoundId = null;

            foreach (var playlist in PlaylistViewModel.Playlists)
            {
                foreach (var item in playlist.NameIdKey)
                {
                    if (User_Query.Contains(playlist.Name))
                    {
                        FoundId = item.Value;
                        FoundName = item.Key;
                        break;
                    }
                }
                if (FoundId != null)
                    break;
            }

            if (FoundId == null)
            {
                throw new Exception("Playlist not found for the given query.");
            }

            string endpointUrl_PlaylistsURIs = $"https://api.spotify.com/v1/playlists/{FoundId}/tracks.items(track(name,uri,total))";

            var response = await _sharedAuthHome.CallSpotifyApiAsync(endpointUrl_PlaylistsURIs);

            PlaylistItemModel MyItemModel = new PlaylistItemModel();
            MyItemModel.Get_Tracks_Jarray.Add(response);

            foreach (var item in response["items"])
            {
                MyItemModel.NameUriKey.Add(item["name"].ToString(), item["uri"].ToString());
            }

            return View();
        }



        [HttpDelete]
        [Route("/Delete_Playlists")]
        public async Task<IActionResult> Delete_Item_Playlist(List<string> User_Query_Tracks, string Playlist_Id)
        {
            PlaylistItemModel MyItemModel = new PlaylistItemModel();
            var Tracks = new List<JToken>(); // FIX: Declare and initialize Tracks

            foreach (string song in User_Query_Tracks)
            {
                string NameOfTrackToAdd = song;

                JToken JTokenToDeletename = MyItemModel.Get_Tracks_Jarray
                    .FirstOrDefault(token => token["uri"]?.Value<string>() == NameOfTrackToAdd);

                if (JTokenToDeletename != null) // FIX: Only add if found
                {
                    Tracks.Add(JTokenToDeletename);
                }
            }

            string endpointUrl = $"https://api.spotify.com/v1/playlists/{Playlist_Id}/{Tracks}";

            var response = await _sharedAuthHome.CallSpotifyApiAsync(endpointUrl);

            return View();
        }
            
            
            
            
            
            

          



        
        
        
        [HttpGet]
        [Route("/GetPlaylistitems")]
        public async Task<IActionResult> Get_Playlist_Items(string PlaylistId)
        {

            var PlaylistItemsViewModel = await _sharedAuthHome.Get_Playlists_Shared(PlaylistId);

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

                var response = await _sharedAuthHome.CallSpotifyApiAsync(endpointUrl);

                _logger.LogInformation("--- GET_PODCASTS (GET) METHOD HIT ---");

                int count = 0;

                foreach (var item in response["shows"]?["items"])
                {
                    string id = item["id"]?.ToString();

                    var (EpisodeNames, EpisodeDescriptions) = await Get_Podcasts_Episodes(id);

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


        private async Task<(List<string>, List<string>)> Get_Podcasts_Episodes(string id)
        {
            List<string> name = new List<string>();
            List<string> description = new List<string>();

            string endpointUrl = $"https://api.spotify.com/v1/shows/{id}/episodes?limit=5";
            var response = await _sharedAuthHome.CallSpotifyApiAsync(endpointUrl);

            foreach (var item in (response["items"] as JArray) ?? new JArray())
            {
                if (item != null && item.Type == JTokenType.Object)
                {

                    var episodeName = item["name"]?.ToString();
                    var episodeDescription = item["description"]?.ToString();


                    if (episodeName != null)
                        name.Add(episodeName);
                    if (episodeDescription != null)
                        description.Add(episodeDescription);

                }
            }
            return (name, description);
        }


    }
}
