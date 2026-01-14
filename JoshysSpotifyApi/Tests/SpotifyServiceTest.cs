using Main.Controllers;
using Main.Interfaces;
using Main.Models;
using Main.Services;
using Moq;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;
namespace Main.Tests
{



    public class SpotifyServiceTest
    {

        private readonly Mock<ISpotifyHTTPClient> _mockSpotifyService;
        private readonly Mock<ISpotifyHTTPClient> _mockSpotifyResponseMessage;


        public SpotifyServiceTest()
        {
            _mockSpotifyService = new Mock<ISpotifyHTTPClient>();
            _mockSpotifyResponseMessage = new Mock<ISpotifyHTTPClient>();
        }

        [Fact]
        public async Task TestAccessTokenProcessSuccess()
        {
            // Arrange

            string testCode = "test_code";
            string testClientCredentials = "test_creds";

            var fakeJsonResponse = new JObject
            {
                { "access_token", "fake_access_token_123" },
                { "refresh_token", "fake_refresh_token_456" }
            };

            var service = new SpotifyService(
                Mock.Of<IConfiguration>(),
                Mock.Of<IHttpClientFactory>(),
                Mock.Of<ILogger<HomeController>>(),
                _mockSpotifyService.Object
            );

            _mockSpotifyService.Setup(x => x.Post(
                It.Is<string>(s => s == "https://accounts.spotify.com/api/token"),
                It.Is<Dictionary<string, string>>(d =>
                    d.Count == 3 &&
                    d["grant_type"] == "authorization_code" &&
                    d["code"] == testCode &&
                    d["redirect_uri"] == "https://127.0.0.1:7071/callback"
                ),
                It.Is<string>(s => s == null),
                It.Is<bool>(b => b == false),
                It.Is<string>(s => s == testClientCredentials)))
                .ReturnsAsync(new HttpResponseMessage
                {
                    Content = new StringContent(fakeJsonResponse.ToString())
                });

            // Act
            var resultJson = await service.Access_Token_Process(testCode, testClientCredentials);

            // Assert
            resultJson.Access_Token.ShouldBe("fake_access_token_123");
            resultJson.Refresh_Token.ShouldBe("fake_refresh_token_456");
        }


        [Fact]
        public async Task TestAccessTokenProcessFailure()
        {
            // Arrange

            string testCode = "test_code";
            string testClientCredentials = "test_creds";

            var service = new SpotifyService(
                Mock.Of<IConfiguration>(),
                Mock.Of<IHttpClientFactory>(),
                Mock.Of<ILogger<HomeController>>(),
                _mockSpotifyResponseMessage.Object

            );


            var response = new HttpResponseMessage();
            response.StatusCode = (System.Net.HttpStatusCode)418;

            _mockSpotifyResponseMessage.SetupGet(x => x.response).Returns(response);

            // Act

            var exception = Should.Throw<Exception>(async () =>
            {
                await service.Access_Token_Process_Check(response);
            });
            //Assert 
            exception.Message.ShouldBe("Failed to retrieve access token from Spotify.");
        }

        [Fact]
        public async Task GetUserPlaylistDetailsTotalTest()
        {
            // Arrange 
            JObject response = new JObject
            {
                { "total", "1" },
                { "items", new JArray
                    {
                        new JObject
                        {
                            { "name", "My Playlist" },
                            { "id", "string" },
                            { "type", "string" },
                            { "uri", "string" },
                            { "tracks", new JObject
                                {
                                    { "href", "string" },
                                    { "total", 0 }
                                }
                            }
                        }
                    }
                }
            };

            var service = new SpotifyService(
                Mock.Of<IConfiguration>(),
                Mock.Of<IHttpClientFactory>(),
                Mock.Of<ILogger<HomeController>>(),
                _mockSpotifyService.Object
            );
            PlaylistViewModel expected = new PlaylistViewModel
            {
                Total = "1",
                Playlists = new List<PlaylistItemModel>
                {
                    new PlaylistItemModel
                    {
                        Name = "My Playlist",
                        Id = "string",

                    }
                }
            };

            // Act
            var result = await service.Get_Playlist_Users_Details(response);

            // Assert

            result.Total.ShouldBe(expected.Total);
            result.Playlists[0].Name.ShouldBe(expected.Playlists[0].Name);
            result.Playlists[0].Id.ShouldBe(expected.Playlists[0].Id);
        }




        /*[Fact]
        public async Task GetPodcastsTest()
        {
            //Act
            string query = "Joe Rogan";


            var service = new SpotifyService(
               Mock.Of<IConfiguration>(),
               Mock.Of<IHttpClientFactory>(),
               Mock.Of<ILogger<HomeController>>(),
               _mockSpotifyService.Object
           );

            var searchResponse = new JObject
            {
                { "shows", new JObject
                    {
                        { "items", new JArray
                            {
                                new JObject
                                {
                                    { "name", "The Joe Rogan Experience" },
                                    { "id", "4rOoJ6Egrf8K2IrywzwOMk" },
                                    { "description", "Podcast Description" }
                                }
                            }
                        }
                    }
                }
            };

            _mockSpotifyService.Setup(x => x.Get(
                It.Is<string>(s => s.Contains("https://api.spotify.com/v1/search"))
            )).ReturnsAsync(searchResponse.ToString());








            var excpectedName = searchResponse["Name"];
            var excpectedId = searchResponse["Id"];
            //Arrange 


            await service.Get_Podcasts(query);

            var resultName = ShowModel.(searchResponse["Name"]);
            //Assert

            resultName.ShouldBe(excpectedName);
            resultId.ShouldBe(excpectedId);


        }*/
    }
}
