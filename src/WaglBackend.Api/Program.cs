using WaglBackend.Infrastructure.Persistence.Seeding;

namespace WaglBackend.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        // Seed database (temporarily disabled for testing)
        try
        {
            using (var scope = host.Services.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                await seeder.SeedAsync();
            }
        }
        catch (Exception ex)
        {
            // Log and continue - allow API to start even if database seeding fails
            Console.WriteLine($"Database seeding failed: {ex.Message}");
            Console.WriteLine("API will start without database seeding");
        }

        await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}