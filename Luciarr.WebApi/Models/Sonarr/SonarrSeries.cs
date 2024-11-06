﻿using Luciarr.Models.Sonarr;
using static Luciarr.WebApi.Controllers.SonarrController;

namespace Luciarr.WebApi.Models.Sonarr
{
    public class SonarrSeries
    {
        public int Id { get; set; }
        public int TvdbId { get; set; }
        public int TvRageId { get; set; }
        public int TvMazeId { get; set; }
        public string ImdbId { get; set; }
        public int TmdbId { get; set; }
        public string Title { get; set; }
        public string CleanTitle { get; set; }
        public List<SonarrSeason> Seasons { get; set; } = new List<SonarrSeason>();
        public List<SonarrImage> Images { get; set; } = new List<SonarrImage>();
        public string Path { get; set; }
        public int Year { get; set; }
    }
}
