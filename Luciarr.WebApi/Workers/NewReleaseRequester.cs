using Coravel.Invocable;
using Luciarr.WebApi.Clients;
using Luciarr.WebApi.Models;
using Microsoft.Extensions.Options;

namespace Luciarr.WebApi.Workers
{
    public class NewReleaseRequester(RadarrClient radarrClient, TmdbClient tmdbClient, IOptionsSnapshot<AppSettings> settings, ILogger<NewReleaseRequester> logger) : IInvocable
    {
        private readonly AppSettings settings = settings.Value;

        public async Task Invoke()
        {
            try
            {
                if (!settings.RequestMovies)
                {
                    return;
                }

                logger.LogInformation("Looking for new movies");
                var qualityProfile = await radarrClient.GetQualityProfile();
                var rootFolder = await radarrClient.GetRootFolder();

                if (qualityProfile == null || rootFolder == null)
                {
                    logger.LogWarning("Root folder or quality profile not found.");
                    return;
                }

                var newMovies = await tmdbClient.GetRecentlyReleasedMovies();

                foreach (var newMovie in newMovies)
                {
                    try
                    {
                        var movieCheck = await radarrClient.GetRadarrMovieByTmdbId(newMovie.Id);
                        if (movieCheck == null)
                        {
                            var movie = await radarrClient.LookupRadarrMovieByTmdbId(newMovie.Id);
                            await radarrClient.PostRadarrMovie(movie, rootFolder, qualityProfile);
                            logger.LogInformation("Requested {Title} ({Year})", movie.Title, movie.Year);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Error when requesting {Title}", newMovie.Title);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error when gathering new movies");
            }
            finally 
            {
                logger.LogInformation("Finished looking for movies");
            }
        }
    }
}
