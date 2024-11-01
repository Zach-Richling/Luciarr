namespace Luciarr.WebApi.Models.Radarr
{
    public class RadarrSettings
    {
        public string QualityProfileName { get; set; }
        public string RootFolderName { get; set; }
        public string MinimumAvailability { get; set; }
        public bool SearchNewRequests { get; set; }
        public bool ImportNewMovies { get; set; }
    }
}
