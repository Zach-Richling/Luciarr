using Microsoft.Extensions.Options;
using Luciarr.WebApi.Models;
using Luciarr.WebApi.Models.Radarr;
using Luciarr.WebApi.Models.Tmdb;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Luciarr.WebApi.Clients
{
    public class RadarrClient : ClientBase, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly RadarrSettings _settings;
        private readonly ILogger<RadarrClient> _logger;

        public readonly bool InvalidURI = false;

        public RadarrClient(IHttpClientFactory factory, IOptionsSnapshot<RadarrSettings> radarrSettings, ILogger<RadarrClient> logger)
        {
            _settings = radarrSettings.Value;
            _logger = logger;

            _httpClient = factory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _settings.RadarrAPIKey);

            try
            {
                _httpClient.BaseAddress = new Uri(SanitizeUri(_settings.RadarrAPIURL));
            } 
            catch (UriFormatException) 
            {
                InvalidURI = true;
            }
        }

        public async Task<RadarrMovie> LookupRadarrMovieByTmdbId(int tmdbId)
        {
            var queryParameters = new Dictionary<string, object>()
            {
                { "tmdbId", tmdbId }
            };

            var response = await _httpClient.GetAsync("api/v3/movie/lookup/tmdb" + QueryString(queryParameters));
            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<RadarrMovie>(await response.Content.ReadAsStringAsync(), JsonSettings) 
                ?? throw new Exception($"Could not find movie with id {tmdbId}");
        }

        public async Task<RadarrMovie?> GetRadarrMovieByTmdbId(int tmdbId)
        {
            var queryParameters = new Dictionary<string, object>()
            {
                { "tmdbId", tmdbId }
            };

            var response = await _httpClient.GetAsync("api/v3/movie/" + QueryString(queryParameters));
            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<List<RadarrMovie>>(await response.Content.ReadAsStringAsync(), JsonSettings)?.FirstOrDefault();
        }

        public async Task PostRadarrMovie(RadarrMovie movie, RadarrRootFolder rootFolder, RadarrQualityProfile profile)
        {
            var response = await _httpClient.PostAsJsonAsync("api/v3/movie", 
                new
                {
                    Title = movie.Title,
                    QualityProfileId = profile.Id,
                    TitleSlug = movie.TitleSlug,
                    Monitored = true,
                    Images = movie.Images,
                    tmdbId = movie.TmdbId,
                    Year = movie.Year,
                    RootFolderPath = rootFolder.Path,
                    MinimumAvailability = _settings.MinimumAvailability,
                    AddOptions = new
                    {
                        IgnoreEpisodesWithFiles = false,
                        IgnoreEpisodesWithoutFiles = false,
                        SearchForMovies = _settings.SearchNewRequests
                    }
                },
                JsonSettings
            );

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Error when requesting {Movie}. {Json}", movie.Title, body);
            }
        }

        public async Task<RadarrRootFolder?> GetRootFolder() 
        {
            var response = await _httpClient.GetAsync("api/v3/rootfolder");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var folders = JsonSerializer.Deserialize<List<RadarrRootFolder>>(json, JsonSettings);
            return folders?.Where(x => x.Path == _settings.RootFolderName).FirstOrDefault();
        }

        public async Task<RadarrQualityProfile?> GetQualityProfile()
        {
            var response = await _httpClient.GetAsync("api/v3/qualityprofile");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var profiles = JsonSerializer.Deserialize<List<RadarrQualityProfile>>(json, JsonSettings);
            return profiles?.Where(x => x.Name == _settings.QualityProfileName).FirstOrDefault();
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
