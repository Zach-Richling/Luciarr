using Coravel.Invocable;
using Luciarr.WebApi.Clients;
using Luciarr.WebApi.Models;
using Microsoft.Extensions.Options;

namespace Luciarr.WebApi.Workers
{
    public class NewReleaseRequester : IInvocable
    {
        private readonly RadarrClient _radarrClient;
        private readonly TmdbClient _tmdbClient;
        private readonly AppSettings _settings;
        private readonly ILogger<NewReleaseRequester> _logger;

        public NewReleaseRequester(RadarrClient client, TmdbClient tmdbClient, IOptionsSnapshot<AppSettings> settings, ILogger<NewReleaseRequester> logger)
        {
            _radarrClient = client;
            _tmdbClient = tmdbClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task Invoke()
        {
            try
            {
                if (!_settings.RequestMovies)
                {
                    return;
                }

                _logger.LogInformation("Looking for new movies");
                var qualityProfile = await _radarrClient.GetQualityProfile();
                var rootFolder = await _radarrClient.GetRootFolder();

                if (qualityProfile == null || rootFolder == null)
                {
                    _logger.LogWarning("Root folder or quality profile not found.");
                    return;
                }

                var newMovies = await _tmdbClient.GetRecentlyReleasedMovies();

                foreach (var newMovie in newMovies)
                {
                    try
                    {
                        var movieCheck = await _radarrClient.GetRadarrMovieByTmdbId(newMovie.Id);
                        if (movieCheck == null)
                        {
                            var movie = await _radarrClient.LookupRadarrMovieByTmdbId(newMovie.Id);
                            await _radarrClient.PostRadarrMovie(movie, rootFolder, qualityProfile);
                            _logger.LogInformation("Requested {Title} ({Year})", movie.Title, movie.Year);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error when requesting {Title}", newMovie.Title);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error when gathering new movies");
            }
            finally 
            {
                _logger.LogInformation("Finished looking for movies");
            }
        }
    }
}
