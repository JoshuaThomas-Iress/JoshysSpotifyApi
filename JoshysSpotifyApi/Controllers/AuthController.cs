using Microsoft.AspNetCore.Mvc;
using System.Web; // 1. ADD THIS 'using' statement at the top of the file

namespace Main.Controllers
{
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Login()
        {
            var clientId = _configuration["Spotify:ClientId"];

            // 2. WRAP these strings with the UrlEncode method to safely format them
            var redirectUri = HttpUtility.UrlEncode("https://localhost:7071/callback");
            var scope = HttpUtility.UrlEncode("user-read-private playlist-modify-public");

            var spotifyUrl = $"https://accounts.spotify.com/authorize?client_id={clientId}&response_type=code&redirect_uri={redirectUri}&scope={scope}";

            return Redirect(spotifyUrl);
        }

        public async Task<IActionResult> Callback(string code)
        {
            // This is the next part you will build.
            return RedirectToAction("Index", "Home");
        }
    }
}