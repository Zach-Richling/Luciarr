﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Luciarr.WebApi.Clients;
using Luciarr.WebApi.Models;
using Luciarr.WebApi.Models.Sonarr;
using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthorizeAttribute = Luciarr.WebApi.Middleware.AuthorizeAttribute;
using Luciarr.Models.Sonarr;

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

        public SonarrController(SonarrClient sonarrClient, IOptions<SonarrSettings> sonarrSettings, ILogger<SonarrController> logger)
        {
            _logger = logger;
            _sonarrClient = sonarrClient;
            _sonarrSettings = sonarrSettings.Value;
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
                        if (seasonFolder == null)
                        {
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
                                        if (!currentHidden.Contains(episode.EpisodeFile!.Path)) 
                                        {
                                            _logger.LogInformation("Hiding {Name}, S{SeasonNumber}E{EpisodeNumber}", series.Title, episode.SeasonNumber, episode.EpisodeNumber);
                                        }

                                        fileStream.WriteLine(episode.EpisodeFile!.Path);
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
            return new Regex("Season ?0?" + seasonNumber);
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
            public string Path { get; set; }
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
