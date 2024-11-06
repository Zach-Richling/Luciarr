namespace Luciarr.Web.Data
{
    public class SqliteConfigurationSource(string connectionString) : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder) => new SqliteConfigurationProvider(connectionString);
    }
}
