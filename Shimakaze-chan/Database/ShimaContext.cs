using Microsoft.EntityFrameworkCore;

namespace Shimakaze
{
    public class ShimaContext : DbContext
    {
        public DbSet<GuildPrefix> GuildPrefix { get; set; }
        public DbSet<GuildJoin> GuildJoin { get; set; }
        public DbSet<GuildSelfAssign> GuildSelfAssign { get; set; }
        public DbSet<GuildWarn> GuildWarn { get; set; }
        public DbSet<StreamingGuild> StreamingGuild { get; set; }
        public DbSet<UserPermissionLevel> UserPermissionLevel { get; set; }
        public DbSet<ShimaGeneric> ShimaGeneric { get; set; }
        public DbSet<TimedEvent> TimedEvents { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            ShimaConfig config = ShimaConfig.LoadConfig();
            optionsBuilder.UseNpgsql($"Host={config.database.host};" +
                $"Port={config.database.port};" +
                $"Database={config.database.name};" +
                $"Username={config.database.username};" +
                $"Password={config.database.password}");
        }
    }
}