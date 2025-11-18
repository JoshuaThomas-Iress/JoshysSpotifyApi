using System.Net.Http.Headers;
using System.Text;
using Main.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Main.Controllers
{
    public class SharedAuthHome
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SharedAuthHome(IHttpClientFactory httpClientFactory,
                              ILogger<HomeController> logger,
                              IConfiguration configuration,
                              IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }
        private ISession Session => _httpContextAccessor.HttpContext.Session;

        [HttpPost]
        public async Task<(string Refresh_Token, string Access_Token)> Refresh_Token_Process(string refreshToken, string Access_Token)
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

            return (null, null);
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
            string Access_Token = Session.GetString("SpotifyAccessToken");
            string Refresh_Token = Session.GetString("SpotifyRefreshToken");

            var httpClient = _httpClientFactory.CreateClient();

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Access_Token);
            using HttpResponseMessage response = await httpClient.GetAsync($"{endpointUrl}");
            if (response.IsSuccessStatusCode)
            {
                string UserContent = await response.Content.ReadAsStringAsync();

                var ResponseJson = JObject.Parse(UserContent);

                return (ResponseJson);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogInformation("--- Error ---");
                throw new Exception("Whoops");
            }

            return null;
        }
        [HttpGet]
        public async Task<PlaylistViewModel> Get_Playlists_Shared(string User_Id)
        {
            string endpointUrl = $"https://api.spotify.com/v1/users/{User_Id}/playlists";

            var response = await CallSpotifyApiAsync(endpointUrl);

            var PlaylistViewModel = await Get_Playlist_Users_Details(response);


            return PlaylistViewModel;
        }

        [HttpGet]
        private async Task<PlaylistViewModel> Get_Playlist_Users_Details(JObject response)
        {
            PlaylistViewModel PlaylistViewModel = new PlaylistViewModel
            {
                Total = response["total"]?.ToString(),
            };

            foreach (var item in response["items"])
            {


                var playlistItem = new PlaylistItemModel
                {
                    Name = item["name"]?.ToString(),
                    Tracks = item["tracks"]?.ToString(),
                    Id = item["id"]?.ToString(),
                    Uri = item["uri"]?.ToString(),

                    Get_Playlists_Jarray = response["items"] as JArray,

                    NameUriKey = new Dictionary<string, string>
                    {

                    }


                };

                PlaylistViewModel.Playlists.Add(playlistItem);
            }
            return PlaylistViewModel;
        }


    }
}
