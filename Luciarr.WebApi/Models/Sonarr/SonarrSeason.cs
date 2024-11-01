using System.Text.Json.Serialization;
using static Luciarr.WebApi.Controllers.SonarrController;

namespace Luciarr.WebApi.Models.Sonarr
{
    public class SonarrSeason
    {
        public int SeasonNumber { get; set; }

        [JsonPropertyName("statistics")]
        public SonarrSeasonStatistics Statistics { get; set; }
    }
}
