using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UrlShortener.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql("Host=urlshortnerdb.postgres.database.azure.com;Database=postgres;Username=usadmin;Password=Test@123;Ssl Mode=Require");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
