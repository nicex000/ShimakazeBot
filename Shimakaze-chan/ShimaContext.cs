using Microsoft.EntityFrameworkCore;
 
namespace Shimakaze
{
    public class ShimaContext : DbContext
    {
        public DbSet<GuildPrefix> GuildPrefix { get; set; }
         
        public ShimaContext()
        {
            Database.EnsureCreated();
        }
         
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=ShimakazeDB;Username=postgres;Password=password");
        }
    }
}