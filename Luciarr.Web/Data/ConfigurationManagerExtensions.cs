using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;

namespace Luciarr.Web.Data
{
    public static class ConfigurationManagerExtensions
    {
        public static ConfigurationManager AddSqliteConfiguration(this ConfigurationManager manager, string connectionString)
        {
            IConfigurationBuilder configBuilder = manager;
            configBuilder.Add(new SqliteConfigurationSource(connectionString));
            return manager;
        }
    }
}
