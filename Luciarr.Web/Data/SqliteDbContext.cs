using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.ComponentModel.DataAnnotations.Schema;

namespace Luciarr.Web.Data
{
    public class SqliteDbContext(string connectionString) : DbContext
    {
        public DbSet<ConfigSetting> ConfigSettings => Set<ConfigSetting>();

        public DbSet<Log> Logs => Set<Log>();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(connectionString);
        }
    }
}
