# Luciarr (Beta)
Luciarr is a complimentary program to Sonarr and Radarr, that integrates with Plex.

## Main Functions
Hiding incomplete seasons and non-contiguous episodes of series as they are downloaded by Sonarr.
 - This is achieved by managing .plexignore files inside the season directory of series.
 - Sonarr must be configured to post download webhooks to the /api/sonarr/downloaded endpoint for this functionality.
 - You can also manage this manually on the Series page.

Requesting popular new releases automatically using Radarr.
 - This funcationality is ran on a weekly basis on Sundays. It looks back two weeks in time for the top 10 most popular releases.

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