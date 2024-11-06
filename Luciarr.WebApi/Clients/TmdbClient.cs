using Microsoft.Extensions.Options;
using Luciarr.WebApi.Models;
using Luciarr.WebApi.Models.Tmdb;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Luciarr.WebApi.Clients
{
    public class TmdbClient : ClientBase, IDisposable
    {
        private readonly HttpClient _httpClient;
        public TmdbClient(IHttpClientFactory factory, IOptionsSnapshot<AppSettings> config)
        {
            var settings = config.Value;

            _httpClient = factory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.TmdbAccessToken}");
            _httpClient.BaseAddress = new Uri("https://api.themoviedb.org/3/");
        }

        public async Task<List<TmdbMovie>> GetRecentlyReleasedMovies()
        {
            var parameters = new Dictionary<string, object>() 
            {
                { "include_adult", false },
                { "include_video", false },
                { "language", "en-US" },
                { "page", 1 },
                { "sort_by", "popularity.desc" },
                { "with_release_type", "2|3" },
                { "release_date.gte", DateTime.Now.AddDays(14).ToString("yyyy-MM-dd") },
                { "release_date.lte", DateTime.Now.ToString("yyyy-MM-dd") }
            };

            var response = await _httpClient.GetAsync("discover/movie" + QueryString(parameters));
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DiscoverMoviesWrapper>(json, JsonSettings);
            return result?.Movies ?? throw new Exception("Could not parse recently released movies");
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            GC.SuppressFinalize(this);
        }

        private class DiscoverMoviesWrapper 
        {
            [JsonPropertyName("results")]
            public List<TmdbMovie> Movies { get; set; }

            public int Page { get; set; }

            [JsonPropertyName("total_pages")]
            public int TotalPages { get; set; }

            [JsonPropertyName("total_results")]
            public int TotalResults { get; set; }
        }
    }
}
