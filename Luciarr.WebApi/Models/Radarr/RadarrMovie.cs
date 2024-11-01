namespace Luciarr.WebApi.Models.Radarr
{
    public class RadarrMovie
    {
        public int? Id { get; set; }
        public string Title { get; set; }
        public string Overview { get; set; }
        public string InCinemas { get; set; }
        public List<RadarrImage> Images { get; set; }
        public bool IsAvailable { get; set; }
        public int Year { get; set; }
        public bool Monitored { get; set; }
        public int TmdbId { get; set; }
        public string TitleSlug { get; set; }
        public int MovieFileId { get; set; }
        public RadarrMovieFile? MovieFile { get; set; }
        public bool Requested => Monitored || IsAvailable;
    }
}
