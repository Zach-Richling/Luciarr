using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Luciarr.WebApi.Clients;
using Luciarr.WebApi.Models;
using Luciarr.WebApi.Models.Sonarr;
using System.Text.RegularExpressions;
using AuthorizeAttribute = Luciarr.WebApi.Middleware.AuthorizeAttribute;
using static Luciarr.WebApi.Controllers.SonarrController;
using System.Text.Json.Serialization;
using Serilog.Parsing;
using Serilog.Formatting.Display;
using System.Text;

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
        public async Task<ActionResult<MessageResult>> PostDownloadWebhook([FromBody] WebhookDownloadPayload webhook)
        {
            var processingMessages = new List<string>();

            if (webhook.EventType != "Download")
            {
                if (webhook.EventType == "Test")
                {
                    AccumulateAndLog(processingMessages, LogLevel.Information, "{Message}", "Good test!");
                    return Ok(new MessageResult(processingMessages));
                }

                AccumulateAndLog(processingMessages, LogLevel.Warning, "{Message}", "Only downloaded events are accepted.");
                return BadRequest(new MessageResult(processingMessages));
            } 
            else if (_sonarrSettings.IgnoreTvdbIds.Contains(webhook.Series.TvdbId))
            {
                AccumulateAndLog(processingMessages, LogLevel.Information, "Series with TvdbId {TvdbId} ignored.", webhook.Series.TvdbId);
                return Ok(new MessageResult(processingMessages));
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
                    AccumulateAndLog(processingMessages, LogLevel.Warning, "Series with TvdbId {TvdbId} could not be found.", webhookSeries.TvdbId);
                    return NotFound(new MessageResult(processingMessages));
                }

                if (_testMode) 
                {
                    series.Path = CreateSeriesFolder(series.Path, series.Seasons.Count);
                }

                if (!Path.Exists(series.Path))
                {
                    AccumulateAndLog(processingMessages, LogLevel.Warning, "{Path} does not exist", series.Path);
                    return BadRequest(new MessageResult(processingMessages));
                }

                var seasonFolders = Directory.GetDirectories(series.Path);
                var lastSeason = series.Seasons.Max(x => x.SeasonNumber);

                var previousSeasonIncomplete = false;
                foreach (var season in series.Seasons.OrderBy(x => x.SeasonNumber))
                {
                    try
                    {
                        if (season.SeasonNumber == 0)
                        {
                            continue;
                        }

                        //Try to find the season folder
                        var seasonFolder = seasonFolders.Where(x => GetSeasonRegex(season.SeasonNumber).IsMatch(x)).FirstOrDefault();
                        if (seasonFolder == null && season.Statistics.PercentOfEpisodes == 0)
                        {
                            previousSeasonIncomplete = true;
                            continue;
                        } 
                        else if (seasonFolder == null)
                        {
                            AccumulateAndLog(processingMessages, LogLevel.Warning, "Could not find season folder for {Title}, S{Season}", series.Title, season.SeasonNumber);
                            continue;
                        }

                        var plexIgnorePath = Path.Combine(seasonFolder, ".plexignore");

                        //If the season is complete and no previous seasons are incomplete, unhide the season
                        if (season.Statistics.PercentOfEpisodes == 100 && !previousSeasonIncomplete)
                        {
                            if (System.IO.File.Exists(plexIgnorePath))
                            {
                                AccumulateAndLog(processingMessages, LogLevel.Information, "Unhiding {Name}, S{SeasonNumber}", series.Title, season.SeasonNumber);
                                System.IO.File.Delete(plexIgnorePath);
                            }
                        }
                        //Unhide the last season of a currently airing series
                        else if (season.SeasonNumber == lastSeason && !series.Ended)
                        {
                            if (System.IO.File.Exists(plexIgnorePath))
                            {
                                AccumulateAndLog(processingMessages, LogLevel.Information, "Unhiding {Name}, S{SeasonNumber}", series.Title, season.SeasonNumber);
                                System.IO.File.Delete(plexIgnorePath);
                            }
                        }
                        else
                        {
                            try
                            {
                                //Get the currently hidden episodes in the season
                                var currentHidden = new List<string>();
                                if (System.IO.File.Exists(plexIgnorePath))
                                {
                                    currentHidden.AddRange(System.IO.File.ReadAllLines(plexIgnorePath));
                                }

                                //Truncate or create the .plexignore file
                                using var fileStream = new StreamWriter(System.IO.File.Create(plexIgnorePath));

                                //If any previous season are incomplete, hide all episodes
                                if (previousSeasonIncomplete)
                                {
                                    if (!currentHidden.Contains("*")) 
                                    {
                                        AccumulateAndLog(processingMessages, LogLevel.Information, "Hiding {Name}, S{SeasonNumber}", series.Title, season.SeasonNumber);
                                    }
                                    fileStream.WriteLine("*");
                                }
                                else
                                {
                                    var episodes = await _sonarrClient.GetSeasonEpisodesById(series.Id, season.SeasonNumber);
                                    var firstMissingEpisode = episodes.Where(x => !x.HasFile).Select(x => (int?)x.EpisodeNumber).Min();

                                    var filteredEpisodes = episodes.Where(x => x.EpisodeNumber >= firstMissingEpisode && !string.IsNullOrEmpty(x.EpisodeFile?.Path));

                                    //This loop is just to show the user what episoes are being unhidden due to rewriting the .plexignore file
                                    foreach (var episode in currentHidden.Except(filteredEpisodes.Select(x => Path.GetFileName(x.EpisodeFile?.Path))))
                                    {
                                        if (!string.IsNullOrEmpty(episode)) 
                                        {
                                            AccumulateAndLog(processingMessages, LogLevel.Information, "Unhiding {Name}, S{SeasonNumber} {EpisodePath}", series.Title, season.SeasonNumber, episode);
                                        }
                                    }

                                    //Hide every episode after the first missing episode
                                    foreach (var episode in filteredEpisodes.OrderBy(x => x.EpisodeNumber))
                                    {
                                        var fileName = Path.GetFileName(episode.EpisodeFile!.Path);
                                        if (!currentHidden.Contains(fileName)) 
                                        {
                                            AccumulateAndLog(processingMessages, LogLevel.Information, "Hiding {Name}, S{SeasonNumber}E{EpisodeNumber}", series.Title, episode.SeasonNumber, episode.EpisodeNumber);
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
                        AccumulateAndLogError(processingMessages, e, "{Exception}", e.Message);
                    }
                }

                return Ok(new MessageResult(processingMessages));
            }
            catch (Exception e) 
            {
                AccumulateAndLogError(processingMessages, e, "{Exception}", e.Message);
            }

            return StatusCode(500, new MessageResult(processingMessages));
        }

        [HttpPost]
        [Route("unhide")]
        public async Task<ActionResult<MessageResult>> PostUnhideRequest([FromBody] UnhideRequest unhideRequest)
        {
            var processingMessages = new List<string>();

            try
            {
                var series = await _sonarrClient.GetSeriesByTvdbId(unhideRequest.TvdbId);

                if (series == null)
                {
                    AccumulateAndLog(processingMessages, LogLevel.Warning, "Series with TvdbId {TvdbId} could not be found.", unhideRequest.TvdbId);
                    return NotFound(new MessageResult(processingMessages));
                }

                _logger.LogInformation("Processing unhide for {Name}", series.Title);

                if (_testMode)
                {
                    series.Path = CreateSeriesFolder(series.Path, series.Seasons.Count);
                }

                if (!Path.Exists(series.Path))
                {
                    AccumulateAndLog(processingMessages, LogLevel.Warning, "{Path} does not exist", series.Path);
                    return BadRequest(new MessageResult(processingMessages));
                }

                var seasonFolders = Directory.GetDirectories(series.Path);
                
                foreach (var folder in seasonFolders)
                {
                    var plexIgnoreFiles = Directory.GetFiles(folder).Where(x => Path.GetFileName(x) == ".plexignore");
                    foreach (var plexIgnoreFile in plexIgnoreFiles)
                    {
                        try
                        {
                            AccumulateAndLog(processingMessages, LogLevel.Information, "Unhiding {FilePath}", plexIgnoreFile);
                            System.IO.File.Delete(plexIgnoreFile);
                        } 
                        catch (Exception e)
                        {
                            AccumulateAndLogError(processingMessages, e, "Could not delete file {FilePath}", plexIgnoreFiles);
                        }
                    }
                }

                return Ok(new MessageResult(processingMessages));
            }
            catch (Exception e)
            {
                AccumulateAndLogError(processingMessages, e, "{Exception}", e);
            }

            return StatusCode(500, new MessageResult(processingMessages));
        }

        //Function for testing locally
        private string CreateSeriesFolder(string seriesPath, int seasons)
        {
            var path = "C:\\Temp";
            var folderName = new DirectoryInfo(seriesPath).Name;
            var newPath = Path.Combine(path, folderName);

            for (int i = 1; i <= seasons; i++)
            {
                Directory.CreateDirectory(Path.Combine(newPath, $"Season {i}"));
            }

            return newPath;
        }

        private static Regex GetSeasonRegex(int seasonNumber)
        {
            return new Regex($"[S|s](eason)? ?0?{seasonNumber}( .*)?$");
        }

        private void AccumulateAndLog(List<string> messages, LogLevel severity, string message, params object?[] args)
        {
            _logger.Log(severity, message, args);
            messages.Add(SerilogFormatString(message, args));
        }

        private void AccumulateAndLogError(List<string> messages, Exception exception, string message, params object?[] args)
        {
            _logger.LogError(exception, message, args);
            messages.Add(SerilogFormatString(message, args));
        }

        private static string SerilogFormatString(string input, params object?[] args)
        {
            var parser = new MessageTemplateParser();
            var template = parser.Parse(input);
            var format = new StringBuilder();
            var index = 0;

            foreach (var tok in template.Tokens)
            {
                if (tok is TextToken)
                    format.Append(tok);
                else
                    format.Append("{" + index++ + "}");
            }
            
            return string.Format(format.ToString(), args);
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

        public class MessageResult
        {
            public MessageResult()
            {
                
            }

            public MessageResult(List<string> messages)
            {
                Messages = messages;
            }

            [JsonPropertyName("messages")]
            public List<string> Messages { get; set; } = new List<string>();
        }
    }
}
