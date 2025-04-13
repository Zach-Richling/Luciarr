using Luciarr.Web.Pages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Luciarr.Web.Data
{
    public class SqliteConfigurationProvider(string connectionString) : ConfigurationProvider
    {
        private static readonly Dictionary<string, string?> DefaultSettings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AllowedHosts"] = "*",
            ["LuciarrSettings:TmdbAccessToken"] = null,
            ["LuciarrSettings:AuthUsername"] = null,
            ["LuciarrSettings:AuthPassword"] = null,
            ["LuciarrSettings:TestMode"] = "True",
            ["SonarrSettings:SonarrAPIKey"] = null,
            ["SonarrSettings:SonarrAPIURL"] = null,
            ["RadarrSettings:RadarrAPIKey"] = null,
            ["RadarrSettings:RadarrAPIURL"] = null,
            ["RadarrSettings:QualityProfileName"] = null,
            ["RadarrSettings:RootFolderName"] = null,
            ["RadarrSettings:MinimumAvailability"] = "Released",
            ["RadarrSettings:SearchNewRequests"] = "True",
            ["RadarrSettings:ImportNewMovies"] = "True",
        };

        public override void Load()
        {
            using var dbContext = new SqliteDbContext(connectionString);
            
            dbContext.Database.EnsureCreated();
             
            var currentSettings = dbContext.ConfigSettings.ToList();
            foreach (var setting in DefaultSettings)
            {
                if (!currentSettings.Any(x => x.Id == setting.Key))
                {
                    dbContext.ConfigSettings.Add(new ConfigSetting(setting.Key, setting.Value));
                }
            }

            dbContext.SaveChanges();

            Data = dbContext.ConfigSettings.Select(x => KeyValuePair.Create(x.Id, x.Value)).ToDictionary();
        }
    }
}
