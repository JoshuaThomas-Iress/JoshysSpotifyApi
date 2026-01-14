using Main.Services;
using Microsoft.AspNetCore.Mvc;
using System.Web;

namespace Main.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly SpotifyService _spotifyService;
        private readonly AuthSpotifyService _authSpotifyService;

        public AuthController(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<AuthController> logger,
            AuthSpotifyService authSpotifyService,
            SpotifyService spotifyService)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _spotifyService = spotifyService;
            _authSpotifyService = authSpotifyService;
        }

        public IActionResult Login(bool? fromPlaylist = false)
        {
            if (fromPlaylist == true)
            {
                TempData["ReturnUrl"] = "Playlist";
                
            }
            
                return Redirect(_authSpotifyService.login_Helper());
            
        }

        [HttpGet]
        [Route("/callback")]
        public async Task<IActionResult> Callback(string code, string error)
        {
             await _authSpotifyService.Callback_Helper(code, error);

            if (TempData["ReturnUrl"] != null && TempData["ReturnUrl"].ToString() == "Playlist")
            {
                return RedirectToAction("Get_Playlists", "Home");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}

