using System.Text.Json.Serialization;

namespace Luciarr.WebApi.Models.Tmdb
{
    public class TmdbMovie
    {
        public int Id { get; set; }
        public string Title { get; set; }
        [JsonPropertyName("release_date")]
        public DateTime ReleaseDate { get; set; }
        public double Popularity { get; set; }

        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }
    }
}
