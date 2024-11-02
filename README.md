# Luciarr (Beta)
Luciarr is a complimentary program to Radarr and Sonarr, that integrates with Plex.

## Main Functions
Hiding incomplete seasons and non-contiguous episodes of series as they are downloaded by Sonarr.
 - This is achieved by managing .plexignore files inside the season directory of series.
 - Sonarr must be configured to post download webhooks to the /api/sonarr/downloaded endpoint for this functionality.

Requesting popular new releases automatically using Radarr.
 - This funcationality is ran on a weekly basis on Sundays. It looks back two weeks in time for the top 10 most popular releases.

## Configuration
An appsettings.json file must be configured in the root directory of the executable with the following items.

- SonarrAPIKey : string. API Key to authenticate with the Sonarr API.
- SonarrAPIURL : string. Your Sonarr URL.
- RadarrAPIKey : string. API Key to authenticate with the Radarr API.
- RadarrAPIURL : string. Your Radarr URL.
- TmdbAccessToken : string. Access Token to authenticate with the Tmdb API.
- AuthUsername : string. Username used to restrict access to the Luciarr API.
- AuthPassword : string. Password used to restrict access to the Luciarr API.

- RadarrSettings. These settings are used when importing new movies to Radarr.
  - QualityProfileName : string. Quality profile name from Radarr.
  - RootFolderName : string. Root folder name from Radarr.
  - MinimumAvailability : string. "Released" is recommended.
  - SearchNewRequests : boolean
  - ImportNewMovies : boolean

- SonarrSettings
  - IgnoreTvdbIds : list of integers. Any series you would like Luciarr to ignore.

- Urls : string. The protocol, IP, and Port you would like Luciarr to bind to.

## Considerations
Luciarr expects season folders to be in the following formats (Season 1 for example): <br>
Any number of characters can follow the season name.
- S1
- S01
- S 1
- S 01
- Season1
- Season01
- Season 1
- Season 01
- s1
- s01
- s 1
- s 01
- season1
- season01
- season 1
- season 01