using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SIG.BeautyDesk.Data;

public sealed class DesignTimeBeautyDeskDbContextFactory : IDesignTimeDbContextFactory<BeautyDeskDbContext>
{
    public BeautyDeskDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BeautyDeskDbContext>();
        var connectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=BeautyDesk;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        optionsBuilder.UseSqlServer(connectionString);
        return new BeautyDeskDbContext(optionsBuilder.Options);
    }
}
