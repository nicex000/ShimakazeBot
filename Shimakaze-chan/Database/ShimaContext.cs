using Microsoft.EntityFrameworkCore;

namespace Shimakaze
{
    public class ShimaContext : DbContext
    {
        public DbSet<GuildPrefix> GuildPrefix { get; set; }
        public DbSet<GuildJoin> GuildJoin { get; set; }
        public DbSet<StreamingGuild> StreamingGuild { get; set; }
        public DbSet<UserPermissionLevel> UserPermissionLevel { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=127.0.0.1;Port=5432;Database=ShimakazeDB;Username=postgres;Password=password");
        }
    }
}