using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Implementations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class IdentityDataSeederTests
{
    [Fact]
    public async Task SeedAsync_WhenRequiredSecretsAreMissing_ThrowsInvalidOperationException_ListingKeyNames()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = provider.GetRequiredService<AppDbContext>();
        var logger = provider.GetRequiredService<ILogger<IdentityDataSeeder>>();

        // Empty configuration: missing Jwt:Key and all passwords
        var emptyConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        var seeder = new IdentityDataSeeder(roleManager, userManager, dbContext, emptyConfig, logger);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync());

        // Verifies clear error message listing missing KEY NAMES, never values
        Assert.Contains("Jwt:Key", ex.Message);
        Assert.Contains("SeedUsers:Astronauts:0:Password", ex.Message);
        Assert.Contains("SeedUsers:Astronauts:1:Password", ex.Message);
        Assert.Contains("SeedUsers:Astronauts:2:Password", ex.Message);
        Assert.Contains("SeedUsers:Monitor:Password", ex.Message);
    }

    [Fact]
    public async Task SeedAsync_WhenAllSecretsAreProvided_ExecutesSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = provider.GetRequiredService<AppDbContext>();
        var logger = provider.GetRequiredService<ILogger<IdentityDataSeeder>>();

        var configValues = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "__TEST_MOCK_JWT_KEY_FOR_UNIT_TESTING_PURPOSES_ONLY_32_BYTES__",
            ["SeedUsers:Astronauts:0:Password"] = "MockPassword123!",
            ["SeedUsers:Astronauts:1:Password"] = "MockPassword123!",
            ["SeedUsers:Astronauts:2:Password"] = "MockPassword123!",
            ["SeedUsers:Monitor:Password"] = "MockPassword123!"
        };

        var config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
        var seeder = new IdentityDataSeeder(roleManager, userManager, dbContext, config, logger);

        await seeder.SeedAsync();

        // Verify 1 Mission was seeded
        var mission = await dbContext.Missions.FirstOrDefaultAsync(m => m.Code == "ARES-III");
        Assert.NotNull(mission);

        // Verify 3 Astronauts were seeded and linked
        var astronauts = await dbContext.Astronauts.ToListAsync();
        Assert.Equal(3, astronauts.Count);
        Assert.All(astronauts, a => Assert.Equal(mission.Id, a.MissionId));
        Assert.All(astronauts, a => Assert.NotNull(a.UserId));

        // Verify Monitor user was seeded
        var monitorUser = await userManager.FindByNameAsync("monitor.rivera");
        Assert.NotNull(monitorUser);
        Assert.Equal("Casey Rivera", monitorUser.FullName);
        Assert.True(await userManager.IsInRoleAsync(monitorUser, "Monitor"));
    }
}

