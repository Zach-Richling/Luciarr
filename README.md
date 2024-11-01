# Luciarr
Luciarr is a complimentary program to Radarr and Sonarr, that integrates with Plex.

## Main Functions
Hiding incomplete seasons and non-contiguous episodes of series as they are downloaded by Sonarr.
 - This is achieved by managing .plexignore files inside the season directory of series.
 - Sonarr must be configured to post download webhooks to the /api/sonarr/downloaded endpoint for this functionality.

Requesting popular new releases automatically to Radarr.
 - This funcationality is ran on a weekly basis on Sundays. It looks back a month in time for the top 10 most popular releases.

## Configuration
An appsettings.json file must be configured in the root directory of the executable with the following items.

- SonarrAPIKey
- SonarrAPIURL
- RadarrAPIKey
- RadarrAPIURL
- TmdbAccessToken
- AuthUsername
- AuthPassword

- RadarrSettings
  - QualityProfileName
  - RootFolderName
  - MinimumAvailability
  - SearchNewRequests : boolean
  - ImportNewMovies : boolean

- SonarrSettings
  - IgnoreTvdbIds : list of integers
