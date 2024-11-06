using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Luciarr.WebApi.Clients;
using Luciarr.WebApi.Models;
using Luciarr.WebApi.Models.Sonarr;
using System.Text.RegularExpressions;
using AuthorizeAttribute = Luciarr.WebApi.Middleware.AuthorizeAttribute;
using Microsoft.AspNetCore;
using static Luciarr.WebApi.Controllers.SonarrController;

namespace Luciarr.WebApi.Controllers
{
    [Authorize]
    [Route("api/sonarr")]
    [ApiController]
    public class SonarrController : ControllerBase
    {
        private readonly ILogger<SonarrController> _logger;
        private readonly SonarrClient _sonarrClient;
        private readonly SonarrSettings _sonarrSettings;

        private readonly bool _testMode;

        public SonarrController(SonarrClient sonarrClient, IOptionsSnapshot<SonarrSettings> sonarrSettings, IOptionsSnapshot<AppSettings> appSettings, ILogger<SonarrController> logger)
        {
            _logger = logger;
            _sonarrClient = sonarrClient;
            _sonarrSettings = sonarrSettings.Value;

            _testMode = appSettings.Value.TestMode;
        }

        [HttpPost]
        [Route("downloaded")]
        public async Task<IActionResult> PostDownloadWebhook([FromBody] WebhookDownloadPayload webhook)
        {
            if (webhook.EventType != "Download")
            {
                if (webhook.EventType == "Test")
                {
                    return Ok();
                }

                return BadRequest("Only downloaded events are accepted.");
            } 
            else if (_sonarrSettings.IgnoreTvdbIds.Contains(webhook.Series.TvdbId))
            {
                return Ok();
            }

            var webhookSeries = webhook.Series;

            try
            {
                _logger.LogInformation(
                    "Processing {Name}: {EpisodesList}",
                    webhookSeries.Title,
                    string.Join(", ", webhook.Episodes.Select(x => $"S{x.SeasonNumber}E{x.EpisodeNumber}"))
                );

                var series = await _sonarrClient.GetSeriesByTvdbId(webhookSeries.TvdbId);
                
                if (series == null)
                {
                    return NotFound("Series could not be found");
                }

                if (_testMode) 
                {
                    series.Path = CreateSeriesFolder(series.Path, series.Seasons.Count);
                }

                if (!Path.Exists(series.Path))
                {
                    _logger.LogWarning("{Path} does not exist", series.Path);
                    return BadRequest("Series path does not exist");
                }

                var seasonFolders = Directory.GetDirectories(series.Path);

                var previousSeasonIncomplete = false;
                foreach (var season in series.Seasons.OrderBy(x => x.SeasonNumber))
                {
                    try
                    {
                        if (season.SeasonNumber == 0)
                        {
                            continue;
                        }

                        var seasonFolder = seasonFolders.Where(x => GetSeasonRegex(season.SeasonNumber).IsMatch(x)).FirstOrDefault();
                        if (seasonFolder == null && season.Statistics.PercentOfEpisodes == 0)
                        {
                            previousSeasonIncomplete = true;
                            continue;
                        } 
                        else if (seasonFolder == null)
                        {
                            _logger.LogWarning("Could not find season folder for {Title}, S{Season}", series.Title, season.SeasonNumber);
                            continue;
                        }

                        var plexIgnorePath = Path.Combine(seasonFolder, ".plexignore");
                        if (season.Statistics.PercentOfEpisodes == 100 && !previousSeasonIncomplete)
                        {
                            if (System.IO.File.Exists(plexIgnorePath))
                            {
                                _logger.LogInformation("Unhiding {Name}, S{SeasonNumber}", series.Title, season.SeasonNumber);
                                System.IO.File.Delete(plexIgnorePath);
                            }
                        }
                        else
                        {
                            try
                            {
                                var currentHidden = new List<string>();
                                if (System.IO.File.Exists(plexIgnorePath))
                                {
                                    currentHidden.AddRange(System.IO.File.ReadAllLines(plexIgnorePath));
                                }

                                using StreamWriter fileStream = new StreamWriter(System.IO.File.Create(plexIgnorePath));

                                if (previousSeasonIncomplete)
                                {
                                    if (!currentHidden.Contains("*")) 
                                    {
                                        _logger.LogInformation("Hiding {Name}, S{SeasonNumber}", series.Title, season.SeasonNumber);
                                    }
                                    fileStream.WriteLine("*");
                                }
                                else
                                {
                                    var episodes = await _sonarrClient.GetSeasonEpisodesById(series.Id, season.SeasonNumber);
                                    var firstMissingEpisode = episodes.Where(x => !x.HasFile).Select(x => (int?)x.EpisodeNumber).Min();

                                    var filteredEpisodes = episodes.Where(x => x.EpisodeNumber >= firstMissingEpisode && !string.IsNullOrEmpty(x.EpisodeFile?.Path));
                                    foreach (var episode in currentHidden.Except(filteredEpisodes.Select(x => x.EpisodeFile?.Path)))
                                    {
                                        if (!string.IsNullOrEmpty(episode)) 
                                        {
                                            _logger.LogInformation("Unhiding {Name}, S{SeasonNumber} {EpisodePath}", series.Title, season.SeasonNumber, episode);
                                        }
                                    }

                                    foreach (var episode in filteredEpisodes.OrderBy(x => x.EpisodeNumber))
                                    {
                                        var fileName = Path.GetFileName(episode.EpisodeFile!.Path);
                                        if (!currentHidden.Contains(fileName)) 
                                        {
                                            _logger.LogInformation("Hiding {Name}, S{SeasonNumber}E{EpisodeNumber}", series.Title, episode.SeasonNumber, episode.EpisodeNumber);
                                        }

                                        fileStream.WriteLine(fileName);
                                    }
                                }
                            }
                            catch
                            {
                                throw;
                            }
                            finally
                            {
                                previousSeasonIncomplete = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Exception: {Exception}", e);
                    }
                }

                return Ok();
            }
            catch (Exception e) 
            {
                _logger.LogError("Exception: {Exception}", e);
                throw;
            }
        }

        [HttpPost]
        [Route("unhide")]
        public async Task<IActionResult> PostUnhideRequest([FromBody] UnhideRequest unhideRequest)
        {
            try
            {
                var series = await _sonarrClient.GetSeriesByTvdbId(unhideRequest.TvdbId);

                if (series == null)
                {
                    return NotFound("Series could not be found.");
                }

                _logger.LogInformation("Processing unhide for {Name}", series.Title);

                if (_testMode)
                {
                    series.Path = CreateSeriesFolder(series.Path, series.Seasons.Count);
                }

                if (!Path.Exists(series.Path))
                {
                    _logger.LogWarning("{Path} does not exist", series.Path);
                    return BadRequest("Series path does not exist");
                }

                var seasonFolders = Directory.GetDirectories(series.Path);
                
                foreach (var folder in seasonFolders)
                {
                    var plexIgnoreFiles = Directory.GetFiles(folder).Where(x => Path.GetFileName(x) == ".plexignore");
                    foreach (var plexIgnoreFile in plexIgnoreFiles)
                    {
                        try
                        {
                            _logger.LogInformation("Unhiding {FilePath}", plexIgnoreFile);
                            System.IO.File.Delete(plexIgnoreFile);
                        } 
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Could not delete file {FilePath}", plexIgnoreFiles);
                        }
                    }
                }

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError("Exception: {Exception}", e);
                throw;
            }
        }

        //Function for testing locally
        private string CreateSeriesFolder(string seriesPath, int seasons)
        {
            var path = "C:\\Temp";
            var folderName = new DirectoryInfo(seriesPath).Name;
            var newPath = Path.Combine(path, folderName);
            Directory.CreateDirectory(newPath);
            for (int i = 1; i <= seasons; i++)
            {
                Directory.CreateDirectory(Path.Combine(newPath, $"Season {i}"));
            }

            return newPath;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("log")]
        public async Task<IActionResult> PostDownloadWebhookForLogging()
        {
            try
            {
                string result = await new StreamReader(Request.Body).ReadToEndAsync();
                _logger.LogInformation("{Payload}", result);
            } 
            catch (Exception e)
            {
                _logger.LogError("Error: {Exception}", e);
            }
            return Ok();
        }

        private static Regex GetSeasonRegex(int seasonNumber)
        {
            return new Regex($"[S|s](eason)? ?0?{seasonNumber}( .*)?$");
        }

        public class UnhideRequest
        {
            public int TvdbId { get; set; }
        }

        public class WebhookDownloadPayload
        {
            public WebhookSeries Series { get; set; }
            public List<WebhookEpisode> Episodes { get; set; } = new List<WebhookEpisode>();
            public string EventType { get; set; }
        }

        public class WebhookSeries
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string? TitleSlug { get; set; }
            public string? Path { get; set; }
            public int TvdbId { get; set; }
            public int? TvMazeId { get; set; }
            public int? TmdbId { get; set; }
            public string? ImdbId { get; set; }
            public int Year { get; set; }
        }

        public class WebhookEpisode
        {
            public int SeasonNumber { get; set; }
            public int EpisodeNumber { get; set; }
        }
    }
}
