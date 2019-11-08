using Microsoft.EntityFrameworkCore;
 
namespace Shimakaze
{
    public class ShimaContext : DbContext
    {
        public DbSet<GuildPrefix> GuildPrefix { get; set; }
        public DbSet<GuildJoin> GuildJoin { get; set; }

        public ShimaContext()
        {
        }
         
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=ShimakazeDB;Username=postgres;Password=password");
        }
    }
}