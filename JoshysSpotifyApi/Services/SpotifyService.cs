using Main.Controllers;
using Main.Interfaces;
using Main.Models;
using Microsoft.AspNetCore.Mvc;
using Nest;
using Newtonsoft.Json.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Web;

namespace Main.Services
{
    public class SpotifyService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;
    
        private readonly ISpotifyHTTPClient _spotifyHttpClient;
        public SpotifyService(IConfiguration configuration,
                             IHttpClientFactory httpClientFactory,
                             ILogger<HomeController> logger,
                             ISpotifyHTTPClient spotifyClient)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _spotifyHttpClient = spotifyClient;
        }

        [HttpGet]
        public async Task<string> Get_User_Id() 
        {
            string endpointUrl = "https://api.spotify.com/v1/me";


            JObject userProfile = await CallSpotifyApiAsync(endpointUrl);
            if (userProfile != null)
            {
                string User_Id = userProfile["id"].ToString();
                return User_Id;
            }
            else
            {
                throw new Exception();
            }
           
        }

        [HttpPost]
        public async Task<(string Refresh_Token, string Access_Token, JObject ResponseJson)> Access_Token_Process(string code, string clientCredentials)//move to spotify services
        {
            var requestData = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", "https://127.0.0.1:7071/callback" }
            };

            

            string endpointUrl = "https://accounts.spotify.com/api/token";
            string Header = null;
            using HttpResponseMessage response = await _spotifyHttpClient.Post(endpointUrl, requestData, Header, false, clientCredentials);
            return await Access_Token_Process_Check(response);
        }
        
        public async Task<(string Refresh_Token, string Access_Token, JObject ResponseJson)> Access_Token_Process_Check(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                var ResponseContent = await response.Content.ReadAsStringAsync();
                var ResponseJson = JObject.Parse(ResponseContent);
                string Access_Token = ResponseJson["access_token"].ToString();
                string Refresh_Token_Output = ResponseJson["refresh_token"].ToString();

                return (Refresh_Token_Output, Access_Token, ResponseJson);
            }
            else
            {
                throw new Exception("Failed to retrieve access token from Spotify.");
            }

        }




            [HttpGet]
        public async Task<JObject> CallSpotifyApiAsync(string endpointUrl)
        {
            var response = await _spotifyHttpClient.Get(endpointUrl);
            return JObject.Parse(response);
        }

        

        [HttpGet]
        public async Task<PlaylistViewModel> Get_Playlists_Shared(string User_Id) 
        {
          

            string endpointUrl = $"https://api.spotify.com/v1/users/{User_Id}/playlists?fields=items(id,name)";

            var response = await CallSpotifyApiAsync(endpointUrl);

            var PlaylistViewModel = await Get_Playlist_Users_Details(response);


            return PlaylistViewModel;
        }

        [HttpGet]
        public async Task<PlaylistViewModel> Get_Playlist_Users_Details(JObject response)//move to spotify services
        {
            PlaylistViewModel PlaylistViewModel = new PlaylistViewModel
            {
                Total = response["total"]?.ToString(),
            };

            foreach (var item in response["items"])
            {


                var playlistItem = new PlaylistItemModel
                {
                    Name = item["name"].ToString(),
                    //Tracks = item["tracks"].ToString(),
                    Id = item["id"].ToString(),


                    Get_Playlists_Jarray = response["items"] as JArray,
                };

                PlaylistViewModel.Playlists.Add(playlistItem);
            }
            return PlaylistViewModel;
        }
        //Delete playlist method doesnt work since it passes a null JObject to this function
        public async Task<JObject> Create_Uri_JObject(JObject response)
        {
            JObject Uri_JObject = new JObject();

            foreach (var item in response["items"])
            {
                var track = item["track"];
                if (track != null && track["uri"] != null)
                {
                    string key = track["id"]?.ToString() ?? Guid.NewGuid().ToString();
                    string value = track["uri"].ToString();
                    Uri_JObject[key] = value;

                    return Uri_JObject;
                }
                else
                {
                    throw new Exception();
                }
            }
            throw new Exception();


        }


        public async Task<Dictionary<string, string>?> Convert_JObject_To_Dictionary(JObject jObject)//move to spotify services
        {
            var result = jObject.ToObject<Dictionary<string, string>>();

            return result;
        }

        public async Task<JObject> Convert_Dictionary_To_JObject(Dictionary<string, string> dictionary)//move to spotify services
        {
            JObject jObject = JObject.FromObject(dictionary);
            return jObject;

        }


        public async Task<ActionResult<List<ShowModel>>> Get_Podcasts(string query)
        {
            List<ShowModel> shows = new List<ShowModel>();

            if (!string.IsNullOrEmpty(query))
            {
                string Encoded_Query = HttpUtility.UrlEncode(query);
                string endpointUrl = $"https://api.spotify.com/v1/search?q={Encoded_Query}&type=show&limit=5";

                var response = await CallSpotifyApiAsync(endpointUrl);

                _logger.LogInformation("--- GET_PODCASTS (GET) METHOD HIT ---");

                int count = 0;

                foreach (var item in response["shows"]?["items"])
                {
                    string id = item["id"]?.ToString();

                    var (EpisodeNames, EpisodeDescriptions) = await Get_Podcasts_Episodes(id);

                    var show = new ShowModel
                    {
                        Id = id,
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
            return (shows);
        }

        [HttpGet]
        private async Task<(List<string>, List<string>)> Get_Podcasts_Episodes(string id)
        {
            List<string> name = new List<string>();
            List<string> description = new List<string>();

            string endpointUrl = $"https://api.spotify.com/v1/shows/{id}/episodes?limit=5";
            var response = await CallSpotifyApiAsync(endpointUrl);

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

        //seperate into 2 methods when possible
        internal async Task<PlaylistItemModel> Retrive_PlaylistItemModel(string User_Query) 
        {
            string userId = await Get_User_Id();
            PlaylistViewModel playlistViewModel = await Get_Playlists_Shared(userId);

            foreach (var item in playlistViewModel.Playlists)
            {
                if (item.Name != null && item.Id != null)
                {
                    item.NameIdKey.Add(item.Name, item.Id);
                }
                else
                {
                    continue;
                }
            }
            string FoundName = null;
            string FoundId = null;

            foreach (var playlist in playlistViewModel.Playlists)
            {
                foreach (var item in playlist.NameIdKey)
                {
                    if (User_Query.Contains(playlist.Name))
                    {
                        FoundId = item.Value;
                        FoundName = item.Key;
                    }
                }
            }

            if (FoundId == null)
            {
                throw new Exception("Playlist not found for the given query.");
            }

            string endpointUrl_PlaylistsURIs = $"https://api.spotify.com/v1/playlists/{FoundId}/tracks?fields=items(track(name,uri,total))";


            JObject response = await CallSpotifyApiAsync(endpointUrl_PlaylistsURIs);




            PlaylistItemModel MyItemModel = new PlaylistItemModel();




            foreach (var item in response["items"])
            {
                var track = item["track"];

                if (track != null && track.Type != JTokenType.Null)
                {
                   
                    if (track["name"] != null)
                    {
                        string key = track["name"].ToString();
                        string value = track["uri"]?.ToString();
                        MyItemModel.NameUriKey[key] = value;
                    }
                }
                else
                {
                   
                    string key = "Unknown_" + Guid.NewGuid().ToString(); 
                    MyItemModel.NameUriKey[key] = null;
                }



            }



            foreach (var item in MyItemModel.NameUriKey.ToList())
            {
                if (User_Query.Contains(item.Key))
                {
                    MyItemModel.NameUriKey.Remove(item.Key);
                }
            }
            return MyItemModel;
        }

        [HttpPost]
        public async Task<(string Refresh_Token, string Access_Token)> Refresh_Token_Process(string refreshToken, string Access_Token, string clientCredentials)//move to spotify services
        {
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var requestData = new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", refreshToken }
                };
             

                var httpClient = _httpClientFactory.CreateClient();

                string Header = "application/x-www-form-urlencoded";
                bool bearerBool = true;
                string endpoint_url = "https://accounts.spotify.com/api/token";
               
                using HttpResponseMessage response = await _spotifyHttpClient.Post(endpoint_url, requestData, clientCredentials, bearerBool,Header );

                if (response.IsSuccessStatusCode)
                {
                    var ResponseContent = await response.Content.ReadAsStringAsync();
                    var ResponseJson = JObject.Parse(ResponseContent);

                    Access_Token = ResponseJson["access_token"].ToString();
                    var Refresh_Token = ResponseJson["refresh_token"].ToString();

                    return (Refresh_Token, Access_Token);
                }
            }

            throw new Exception("Invalid refresh token");
        }



    }
}

