using System.Text.Json.Serialization;

namespace Luciarr.WebApi.Models.Sonarr
{
    public class SonarrImage
    {
        [JsonPropertyName("coverType")]
        public string CoverType { get; set; }

        [JsonPropertyName("remoteUrl")]
        public string RemoteUrl { get; set; }
    }
}
