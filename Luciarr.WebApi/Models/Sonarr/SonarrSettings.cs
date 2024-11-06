namespace Luciarr.WebApi.Models.Sonarr
{
    public class SonarrSettings
    {
        public List<int> IgnoreTvdbIds { get; set; } = new List<int>();
        public string SonarrAPIKey { get; set; }
        public string SonarrAPIURL { get; set; }
    }
}
