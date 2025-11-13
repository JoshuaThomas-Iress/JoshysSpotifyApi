using Newtonsoft.Json.Linq;

public class PlaylistItemModel
{
    public string Name { get; set; }
    public string Tracks { get; set; }
    
    public string Id { get; set; }
    public string Uri { get; set; }

    public Dictionary<string, string> NameUriKey { get; set; }
    public Dictionary<string, string> NameIdKey { get; set; } = new Dictionary<string, string>();   
    public JArray Get_Playlists_Jarray { get; set; } = new JArray();
    public JArray Get_Tracks_Jarray { get; set; }
}

// Represents ALL the data for the Get_Playlists.cshtml page
public class PlaylistViewModel
{
    public string Total { get; set; }
    public List<PlaylistItemModel> Playlists { get; set; }

    public PlaylistViewModel()
    {
        Playlists = new List<PlaylistItemModel>();
    }
}