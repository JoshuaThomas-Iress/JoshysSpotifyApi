namespace Main.Models
{
    public class EpisodeModel
    {
        public string Name { get; set; } = "Missing";
        public string Description { get; set; }
    }

    public class ShowModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Id { get; set; }
        public List<EpisodeModel> Episodes { get; set; }

        public ShowModel()
        {
            Episodes = new List<EpisodeModel>();
        }
    }

    public class EpisodesViewModel
    {
        public List<EpisodeModel> firstShowEpisode { get; set; }
    }
}
