﻿using System.Text.Json.Serialization;

namespace Luciarr.WebApi.Models.Sonarr
{
    public class SonarrStatistics
    {
        public int EpisodeCount { get; set; }
        public int TotalEpisodeCount { get; set; }

        [JsonPropertyName("percentOfEpisodes")]
        public double PercentOfEpisodes { get; set; }
    }
}
