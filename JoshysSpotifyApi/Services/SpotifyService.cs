using Main.Controllers;
using Main.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace Main.Services
{
    public class SpotifyService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;
    
        private readonly SpotifyHTTPClient _spotifyHttpClient;
        public SpotifyService(IConfiguration configuration,
                             IHttpClientFactory httpClientFactory,
                             ILogger<HomeController> logger,
                      
                             SpotifyHTTPClient spotifyHttpClient)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _spotifyHttpClient = spotifyHttpClient;
        }

        [HttpGet]
        public async Task<string> Get_User_Id() 
        {
            string endpointUrl = "https://api.spotify.com/v1/me";


            JObject userProfile = await CallSpotifyApiAsync(endpointUrl);

            string User_Id = userProfile["id"]?.ToString();

            if (User_Id == null)
            {
                throw new Exception();
            }
            return User_Id;
        }

        [HttpPost]
        public async Task<(string Refresh_Token, string Access_Token, JObject ResponseJson)> Access_Token_Process(string code, string clientCredentials)//move to spotify services
        {
            var httpClient = _httpClientFactory.CreateClient();

            var requestData = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", "https://127.0.0.1:7071/callback" }
            };

            var requestContent = new FormUrlEncodedContent(requestData);

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", clientCredentials);

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

        [HttpGet]
        public async Task<JObject> CallSpotifyApiAsync(string endpointUrl)
        {
            var response = await _spotifyHttpClient.Get(endpointUrl);
            return JObject.Parse(response);
        }


        //https://developer.spotify.com/documentation/web-api/reference/get-list-users-playlists
        //Am I extracting the users playlists id?
        [HttpGet]
        public async Task<PlaylistViewModel> Get_Playlists_Shared(string User_Id) //move to spotify services
        {
            string endpointUrl = $"https://api.spotify.com/v1/users/{User_Id}/playlists?fields=items(id,name)";

            var response = await CallSpotifyApiAsync(endpointUrl);

            var PlaylistViewModel = await Get_Playlist_Users_Details(response);


            return PlaylistViewModel;
        }

        [HttpGet]
        private async Task<PlaylistViewModel> Get_Playlist_Users_Details(JObject response)//move to spotify services
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

        public async Task<JObject> Create_Uri_JObject(JObject response)//move to spotify services
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


        [HttpGet]
        public async Task<(List<string>, List<string>)> Get_Podcasts_Episodes(string id)
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


        public async Task<PlaylistItemModel> Retrive_PlaylistItemModel(string User_Query) 
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


            var response = await CallSpotifyApiAsync(endpointUrl_PlaylistsURIs);




            PlaylistItemModel MyItemModel = new PlaylistItemModel();




            foreach (var item in response["items"])
            {
                var track = item["track"];



                string key = track["name"].ToString();
                string value = track["uri"].ToString();

                MyItemModel.NameUriKey[key] = value;
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
        public async Task<(string Refresh_Token, string Access_Token)> Refresh_Token_Process(string refreshToken, string Access_Token)//move to spotify services
        {
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var RequestDataBody = new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", refreshToken }
                };
                var requestContent = new FormUrlEncodedContent(RequestDataBody);

                var httpClient = _httpClientFactory.CreateClient();

                string Content_Type = "application/x-www-form-urlencoded";

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Content_Type);
                using HttpResponseMessage response = await httpClient.PostAsync("https://accounts.spotify.com/api/token", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var ResponseJson = JObject.Parse(responseContent);

                    Access_Token = ResponseJson["access_token"].ToString();
                    var Refresh_Token = ResponseJson["refresh_token"].ToString();

                    return (Refresh_Token, Access_Token);
                }
            }

            throw new Exception("Invalid refresh token");
        }



    }
}
