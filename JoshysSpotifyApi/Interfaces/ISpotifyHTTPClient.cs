

namespace Main.Interfaces
{

    public interface ISpotifyHTTPClient
    {
        object response { get; set; }

        Task<HttpResponseMessage> Post(
            string url,
            Dictionary<string, string> data,
            string header,
            bool flag,
            string creds
     );
        Task<string> Get(string url);
    }
}
