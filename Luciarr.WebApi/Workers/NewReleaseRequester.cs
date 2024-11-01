using Coravel.Invocable;
using Luciarr.WebApi.Clients;

namespace Luciarr.WebApi.Workers
{
    public class NewReleaseRequester : IInvocable
    {
        private readonly RadarrClient _radarrClient;
        private readonly TmdbClient _tmdbClient;
        private readonly ILogger<NewReleaseRequester> _logger;

        public NewReleaseRequester(RadarrClient client, TmdbClient tmdbClient,  ILogger<NewReleaseRequester> logger)
        {
            _radarrClient = client;
            _tmdbClient = tmdbClient;
            _logger = logger;
        }

        public async Task Invoke()
        {
            try
            {
                _logger.LogInformation("Looking for new movies");
                var qualityProfile = await _radarrClient.GetQualityProfile();
                var rootFolder = await _radarrClient.GetRootFolder();

                if (qualityProfile == null || rootFolder == null)
                {
                    _logger.LogWarning("Root folder or quality profile not found.");
                    return;
                }

                var newMovies = await _tmdbClient.GetRecentlyReleasedMovies();

                foreach (var newMovie in newMovies.Take(10))
                {
                    try
                    {
                        var movieCheck = await _radarrClient.GetRadarrMovieByTmdbId(newMovie.Id);
                        if (movieCheck == null)
                        {
                            var movie = await _radarrClient.LookupRadarrMovieByTmdbId(newMovie.Id);
                            await _radarrClient.PostRadarrMovie(movie, rootFolder, qualityProfile);
                            _logger.LogInformation("Requested {Title}({Year})", movie.Title, movie.Year);
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

            _logger.LogInformation("Finished looking for movies");
        }
    }
}
