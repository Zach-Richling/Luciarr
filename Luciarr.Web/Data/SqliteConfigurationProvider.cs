using Luciarr.Web.Pages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Luciarr.Web.Data
{
    public class SqliteConfigurationProvider(string connectionString) : ConfigurationProvider
    {
        public override void Load()
        {
            using var dbContext = new SqliteDbContext(connectionString);
            
            dbContext.Database.EnsureCreated();
            
            Data = dbContext.ConfigSettings.Any()
                ? dbContext.ConfigSettings.ToDictionary(
                    static c => c.Id,
                    static c => c.Value)
                : CreateAndSaveDefaultValues(dbContext);
        }

        static Dictionary<string, string?> CreateAndSaveDefaultValues(SqliteDbContext context)
        {
            var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["AllowedHosts"] = "*",
                ["LuciarrSettings:TmdbAccessToken"] = null,
                ["LuciarrSettings:AuthUsername"] = null,
                ["LuciarrSettings:AuthPassword"] = null,
                ["LuciarrSettings:TestMode"] = true.ToString(),
                ["SonarrSettings:SonarrAPIKey"] = null,
                ["SonarrSettings:SonarrAPIURL"] = null,
                ["RadarrSettings:RadarrAPIKey"] = null,
                ["RadarrSettings:RadarrAPIURL"] = null,
                ["RadarrSettings:QualityProfileName"] = null,
                ["RadarrSettings:RootFolderName"] = null,
                ["RadarrSettings:MinimumAvailability"] = "Released",
                ["RadarrSettings:SearchNewRequests"] = true.ToString(),
                ["RadarrSettings:ImportNewMovies"] = true.ToString(),
            };

            context.ConfigSettings.AddRange(settings.Select(x => new ConfigSetting(x.Key, x.Value)));

            context.SaveChanges();

            return settings;
        }
    }
}
