using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AstronautHealthMonitoring.Services.Implementations;

public class IdentityDataSeeder : IIdentityDataSeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentityDataSeeder> _logger;

    public IdentityDataSeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext,
        IConfiguration configuration,
        ILogger<IdentityDataSeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Fail-fast validation of required secrets before proceeding
        ValidateRequiredSecrets();

        // 1. Ensure Roles exist (Only Astronaut and Monitor)
        await EnsureRoleExistsAsync(UserRoles.Astronaut);
        await EnsureRoleExistsAsync(UserRoles.Monitor);

        // 2. Ensure 1 Mission exists
        var mission = await EnsureMissionExistsAsync(cancellationToken);

        // 3. Seed 3 Demo Astronauts assigned to the Mission
        await SeedDemoAstronautsAsync(mission, cancellationToken);

        // 4. Seed 1 Demo Monitor
        await SeedDemoMonitorAsync();
    }

    private void ValidateRequiredSecrets()
    {
        var missingKeys = new List<string>();

        if (string.IsNullOrWhiteSpace(_configuration["Jwt:Key"]))
        {
            missingKeys.Add("Jwt:Key");
        }

        var fallbackAstroPassword = _configuration["SeedUsers:Astronaut:Password"];
        for (var i = 0; i < 3; i++)
        {
            var key = $"SeedUsers:Astronauts:{i}:Password";
            if (string.IsNullOrWhiteSpace(_configuration[key]) && string.IsNullOrWhiteSpace(fallbackAstroPassword))
            {
                missingKeys.Add(key);
            }
        }

        if (string.IsNullOrWhiteSpace(_configuration["SeedUsers:Monitor:Password"]))
        {
            missingKeys.Add("SeedUsers:Monitor:Password");
        }

        if (missingKeys.Count > 0)
        {
            var message = $"Startup failed: Seeding is enabled, but the following required configuration/user-secrets keys are missing: {string.Join(", ", missingKeys)}. Set them via 'dotnet user-secrets set \"<Key>\" \"<Value>\"'.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded)
            {
                _logger.LogInformation("Role '{RoleName}' created successfully.", roleName);
            }
            else
            {
                _logger.LogError("Failed to create role '{RoleName}': {Errors}",
                    roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task<Mission> EnsureMissionExistsAsync(CancellationToken cancellationToken)
    {
        var mission = await _dbContext.Missions
            .FirstOrDefaultAsync(m => m.Code == "ARES-III", cancellationToken);

        if (mission == null)
        {
            mission = new Mission
            {
                Code = "ARES-III",
                Name = "Ares III Expedition",
                Destination = "Mars - Acidalia Planitia",
                StartDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 12, 15, 0, 0, 0, DateTimeKind.Utc),
                Status = MissionStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Missions.Add(mission);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded demo Mission '{Code} - {Name}'.", mission.Code, mission.Name);
        }

        return mission;
    }

    private async Task SeedDemoAstronautsAsync(Mission mission, CancellationToken cancellationToken)
    {
        var defaultAstronauts = new[]
        {
            new
            {
                Index = 0,
                DefaultUsername = "astro.vance",
                DefaultEmail = "morgan.vance@mission.space",
                DefaultFullName = "Morgan Vance",
                AstronautCode = "AST-001",
                DateOfBirth = new DateTime(1988, 5, 12, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Non-binary",
                BloodType = "O+",
                HeightCm = 175.0,
                WeightKg = 70.0
            },
            new
            {
                Index = 1,
                DefaultUsername = "astro.lee",
                DefaultEmail = "jordan.lee@mission.space",
                DefaultFullName = "Jordan Lee",
                AstronautCode = "AST-002",
                DateOfBirth = new DateTime(1991, 8, 24, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Female",
                BloodType = "A+",
                HeightCm = 168.0,
                WeightKg = 62.0
            },
            new
            {
                Index = 2,
                DefaultUsername = "astro.quinn",
                DefaultEmail = "taylor.quinn@mission.space",
                DefaultFullName = "Taylor Quinn",
                AstronautCode = "AST-003",
                DateOfBirth = new DateTime(1985, 11, 3, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Male",
                BloodType = "B+",
                HeightCm = 182.0,
                WeightKg = 78.0
            }
        };

        foreach (var def in defaultAstronauts)
        {
            var username = _configuration[$"SeedUsers:Astronauts:{def.Index}:Username"]
                ?? _configuration["SeedUsers:Astronaut:Username"]
                ?? def.DefaultUsername;

            var email = _configuration[$"SeedUsers:Astronauts:{def.Index}:Email"]
                ?? _configuration["SeedUsers:Astronaut:Email"]
                ?? def.DefaultEmail;

            var fullName = _configuration[$"SeedUsers:Astronauts:{def.Index}:FullName"]
                ?? _configuration["SeedUsers:Astronaut:FullName"]
                ?? def.DefaultFullName;

            var code = _configuration[$"SeedUsers:Astronauts:{def.Index}:AstronautCode"]
                ?? def.AstronautCode;

            var password = _configuration[$"SeedUsers:Astronauts:{def.Index}:Password"]
                ?? _configuration["SeedUsers:Astronaut:Password"];

            if (string.IsNullOrWhiteSpace(password))
            {
                continue;
            }

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = username,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("Failed to create demo astronaut user '{Username}': {Errors}",
                        username, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    continue;
                }

                await _userManager.AddToRoleAsync(user, UserRoles.Astronaut);
                _logger.LogInformation("Demo astronaut user '{Username}' created and assigned role '{Role}'.", username, UserRoles.Astronaut);
            }

            // Link to domain Astronaut record assigned to mission
            var astronaut = await _dbContext.Astronauts
                .FirstOrDefaultAsync(a => a.AstronautCode == code, cancellationToken);

            var names = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var firstName = names.Length > 0 ? names[0] : fullName;
            var lastName = names.Length > 1 ? names[1] : string.Empty;

            if (astronaut == null)
            {
                astronaut = new Astronaut
                {
                    AstronautCode = code,
                    FirstName = firstName,
                    LastName = lastName,
                    FullName = fullName,
                    DateOfBirth = def.DateOfBirth,
                    Gender = def.Gender,
                    BloodType = def.BloodType,
                    HeightCm = def.HeightCm,
                    WeightKg = def.WeightKg,
                    UserId = user.Id,
                    MissionId = mission.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.Astronauts.Add(astronaut);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Created new Astronaut profile '{Code}' assigned to Mission '{MissionCode}' and linked to user '{UserId}'.",
                    code, mission.Code, user.Id);
            }
            else
            {
                var modified = false;
                if (astronaut.UserId != user.Id)
                {
                    astronaut.UserId = user.Id;
                    modified = true;
                }
                if (astronaut.MissionId != mission.Id)
                {
                    astronaut.MissionId = mission.Id;
                    modified = true;
                }
                if (modified)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Updated Astronaut profile '{Code}' assignment to Mission '{MissionCode}' and link to user '{UserId}'.",
                        code, mission.Code, user.Id);
                }
            }
        }
    }

    private async Task SeedDemoMonitorAsync()
    {
        var username = _configuration["SeedUsers:Monitor:Username"] ?? "monitor.rivera";
        var email = _configuration["SeedUsers:Monitor:Email"] ?? "casey.rivera@missioncontrol.space";
        var fullName = _configuration["SeedUsers:Monitor:FullName"] ?? "Casey Rivera";
        var password = _configuration["SeedUsers:Monitor:Password"];

        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                _logger.LogError("Failed to create demo monitor user '{Username}': {Errors}",
                    username, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            await _userManager.AddToRoleAsync(user, UserRoles.Monitor);
            _logger.LogInformation("Demo monitor user '{Username}' created and assigned role '{Role}'.", username, UserRoles.Monitor);
        }
    }
}
