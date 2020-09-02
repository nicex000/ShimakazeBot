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
            optionsBuilder.UseNpgsql($"Host={ShimakazeBot.Config.database.host};" +
                $"Port={ShimakazeBot.Config.database.port};" +
                $"Database={ShimakazeBot.Config.database.name};" +
                $"Username={ShimakazeBot.Config.database.username};" +
                $"Password={ShimakazeBot.Config.database.password}");
        }
    }
}