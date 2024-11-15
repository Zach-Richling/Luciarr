using Microsoft.Extensions.Options;
using Luciarr.WebApi.Models;
using Luciarr.WebApi.Models.Sonarr;
using System.Text.Json;
using static Luciarr.WebApi.Controllers.SonarrController;

namespace Luciarr.WebApi.Clients
{
    public class SonarrClient : ClientBase, IDisposable
    {
        private readonly HttpClient _httpClient;

        public readonly bool InvalidURI = false;

        public SonarrClient(IHttpClientFactory factory, IOptionsSnapshot<SonarrSettings> config)
        {
            var settings = config.Value;

            _httpClient = factory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", settings.SonarrAPIKey);
            try
            {
                _httpClient.BaseAddress = new Uri(SanitizeUri(settings.SonarrAPIURL));
            }
            catch (UriFormatException) 
            { 
                InvalidURI = true;
            }
        }

        public async Task<SonarrSeries> GetSeriesByTvdbId(int tvdbId)
        {
            var queryParameters = new Dictionary<string, object>() 
            {
                { "tvdbId", tvdbId }
            };

            var response = await _httpClient.GetAsync($"api/v3/series" + QueryString(queryParameters));
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            //Console.WriteLine(body);

            return JsonSerializer.Deserialize<List<SonarrSeries?>>(
                body,
                JsonSettings
            )?.FirstOrDefault() ?? throw new Exception("Series could not be found");
        }

        public async Task<IEnumerable<SonarrSeries>> GetAllSeries()
        {
            var response = await _httpClient.GetAsync($"api/v3/series");
            
            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<List<SonarrSeries>>(
                await response.Content.ReadAsStringAsync(),
                JsonSettings
            ) ?? new List<SonarrSeries>();
        }

        public async Task<List<SonarrEpisode>> GetSeasonEpisodesById(int seriesId, int seasonNumber)
        {
            var queryParameters = new Dictionary<string, object>()
            {
                { "seriesId", seriesId },
                { "seasonNumber", seasonNumber },
                { "includeEpisodeFile", true }
            };

            var response = await _httpClient.GetAsync($"api/v3/episode" + QueryString(queryParameters));
            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<List<SonarrEpisode>>(
                await response.Content.ReadAsStringAsync(),
                JsonSettings
            ) ?? throw new Exception("Could not find season episodes");
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
