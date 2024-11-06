namespace Luciarr.WebApi.Models
{
    public class AppSettings
    {
        public string TmdbAccessToken { get; set; }
        public string AuthUsername { get; set; }
        public string AuthPassword { get; set; }
        public bool TestMode { get; set; } = false;
    }
}
