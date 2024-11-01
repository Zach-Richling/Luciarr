using static Luciarr.WebApi.Controllers.SonarrController;

namespace Luciarr.WebApi.Models.Sonarr
{
    public class SonarrEpisode
    {
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public bool HasFile { get; set; }
        public SonarrEpisodeFile? EpisodeFile { get; set; }
    }
}
