using CommandService.Models;
using CommandService.SyncDataServices.Grpc;
using Microsoft.EntityFrameworkCore;

namespace CommandService.Data;

public static class PrepDb
{
    public static void PrepPopulation(IApplicationBuilder app, bool isProduction)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        var grpcClient = serviceScope.ServiceProvider.GetService<IPlatformDataClient>();
        var platforms = grpcClient.ReturnAllPlatforms();

        SeedData(
            serviceScope.ServiceProvider.GetService<AppDbContext>()!,
            serviceScope.ServiceProvider.GetService<ICommandRepo>()!,
            platforms,
            isProduction);
    }

    private static void SeedData(
        AppDbContext context,
        ICommandRepo commandRepo,
        IEnumerable<Platform> platforms,
        bool isProduction)
    {
        if (isProduction)
        {
            Console.WriteLine("--> Seeding Data...");
            try
            {
                context.Database.Migrate();
            }
            catch (Exception e)
            {
                Console.WriteLine("--> Could not run migrations.");
                Console.WriteLine(e);
            }
        }

        if (!context.Platforms.Any())
        {
            Console.WriteLine("--> Seeding new platforms...");
            foreach (var platform in platforms)
            {
                if (!commandRepo.PlatformExistsWithExternalId(platform.ExternalId))
                {
                    commandRepo.CreatePlatform(platform);
                }
            }

            commandRepo.SaveChanges();
        }
        else
        {
            Console.WriteLine("--> We already have data, skipping seeding.");
        }
    }
}