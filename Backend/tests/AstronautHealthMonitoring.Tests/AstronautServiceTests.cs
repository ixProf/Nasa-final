using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Astronaut;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Implementations;
using AstronautHealthMonitoring.Services.Interfaces;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class AstronautServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IMapper _mapper;
    private readonly AstronautService _astronautService;
    private readonly AuthService _authService;

    public AstronautServiceTests()
    {
        var dbName = $"AstronautServiceTestDb_{Guid.NewGuid()}";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        _context = provider.GetRequiredService<AppDbContext>();
        _userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        _roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

        // Seed roles
        _roleManager.CreateAsync(new IdentityRole(UserRoles.Astronaut)).GetAwaiter().GetResult();
        _roleManager.CreateAsync(new IdentityRole(UserRoles.Monitor)).GetAwaiter().GetResult();

        var config = new TypeAdapterConfig();
        new AstronautMappings().Register(config);
        _mapper = new Mapper(config);

        _astronautService = new AstronautService(
            _context,
            _userManager,
            _mapper,
            NullLogger<AstronautService>.Instance);

        var configValues = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "SuperSecretSigningKey_ThatIsAtLeast32BytesLong!",
            ["Jwt:Issuer"] = "AstronautHealthMonitoring",
            ["Jwt:Audience"] = "AstronautHealthMonitoringClient",
            ["Jwt:AccessTokenExpirationMinutes"] = "15",
            ["Jwt:RefreshTokenExpirationDays"] = "30",
            ["Jwt:RefreshTokenGraceWindowSeconds"] = "30"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
        var tokenService = new TokenService(configuration);
        _authService = new AuthService(_userManager, _context, tokenService, configuration);
    }

    [Fact]
    public async Task CreateAstronautAsync_ValidRequest_CreatesBothAstronautAndIdentityUser()
    {
        var request = new CreateAstronautRequest
        {
            Username = "astro.test",
            Email = "astro.test@station.org",
            InitialPassword = "ValidPassword123!",
            AstronautCode = "AST-TEST-01",
            FirstName = "Samantha",
            LastName = "Cristoforetti",
            DateOfBirth = new DateTime(1977, 4, 26, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "A+",
            HeightCm = 170,
            WeightKg = 60
        };

        var result = await _astronautService.CreateAstronautAsync(request);

        Assert.NotNull(result);
        Assert.Equal("AST-TEST-01", result.AstronautCode);
        Assert.Equal("Samantha Cristoforetti", result.FullName);
        Assert.True(result.IsActive);
        Assert.NotNull(result.UserId);

        // Verify Identity user
        var user = await _userManager.FindByNameAsync("astro.test");
        Assert.NotNull(user);
        Assert.Equal("astro.test@station.org", user.Email);
        Assert.Equal(result.UserId, user.Id);
        Assert.True(await _userManager.IsInRoleAsync(user, UserRoles.Astronaut));

        // Verify Astronaut entity
        var astronaut = await _context.Astronauts.FirstOrDefaultAsync(a => a.AstronautCode == "AST-TEST-01");
        Assert.NotNull(astronaut);
        Assert.Equal(user.Id, astronaut.UserId);
        Assert.True(astronaut.IsActive);
    }

    [Fact]
    public async Task CreateAstronautAsync_DuplicateUsername_ThrowsInvalidOperationException()
    {
        var request1 = new CreateAstronautRequest
        {
            Username = "dup.user",
            Email = "user1@station.org",
            InitialPassword = "ValidPassword123!",
            AstronautCode = "AST-DUP-01",
            FirstName = "First",
            LastName = "User",
            DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 180,
            WeightKg = 75
        };
        await _astronautService.CreateAstronautAsync(request1);

        var request2 = new CreateAstronautRequest
        {
            Username = "dup.user",
            Email = "user2@station.org",
            InitialPassword = "ValidPassword123!",
            AstronautCode = "AST-DUP-02",
            FirstName = "Second",
            LastName = "User",
            DateOfBirth = new DateTime(1985, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "B+",
            HeightCm = 165,
            WeightKg = 55
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _astronautService.CreateAstronautAsync(request2));
        Assert.Contains("Username 'dup.user' is already taken", ex.Message);
    }

    [Fact]
    public async Task CreateAstronautAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        var request1 = new CreateAstronautRequest
        {
            Username = "user.alpha",
            Email = "shared@station.org",
            InitialPassword = "ValidPassword123!",
            AstronautCode = "AST-ALPHA",
            FirstName = "Alpha",
            LastName = "Crew",
            DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 180,
            WeightKg = 75
        };
        await _astronautService.CreateAstronautAsync(request1);

        var request2 = new CreateAstronautRequest
        {
            Username = "user.beta",
            Email = "shared@station.org",
            InitialPassword = "ValidPassword123!",
            AstronautCode = "AST-BETA",
            FirstName = "Beta",
            LastName = "Crew",
            DateOfBirth = new DateTime(1985, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "B+",
            HeightCm = 165,
            WeightKg = 55
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _astronautService.CreateAstronautAsync(request2));
        Assert.Contains("Email 'shared@station.org' is already registered", ex.Message);
    }

    [Fact]
    public async Task CreateAstronautAsync_DuplicateAstronautCode_ThrowsInvalidOperationException()
    {
        var request1 = new CreateAstronautRequest
        {
            Username = "code.user1",
            Email = "code1@station.org",
            InitialPassword = "ValidPassword123!",
            AstronautCode = "AST-SAME-CODE",
            FirstName = "First",
            LastName = "Crew",
            DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 180,
            WeightKg = 75
        };
        await _astronautService.CreateAstronautAsync(request1);

        var request2 = new CreateAstronautRequest
        {
            Username = "code.user2",
            Email = "code2@station.org",
            InitialPassword = "ValidPassword123!",
            AstronautCode = "AST-SAME-CODE",
            FirstName = "Second",
            LastName = "Crew",
            DateOfBirth = new DateTime(1985, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "B+",
            HeightCm = 165,
            WeightKg = 55
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _astronautService.CreateAstronautAsync(request2));
        Assert.Contains("Astronaut code 'AST-SAME-CODE' already exists", ex.Message);
    }

    [Theory]
    [InlineData("short")] // < 8 chars
    [InlineData("alllowercase123!")] // no uppercase
    [InlineData("ALLUPPERCASE123!")] // no lowercase
    [InlineData("NoDigitsHere!")] // no digit
    [InlineData("NoSpecialChars123")] // no non-alphanumeric
    public async Task CreateAstronautAsync_WeakPassword_ThrowsArgumentException(string weakPassword)
    {
        var request = new CreateAstronautRequest
        {
            Username = $"user.{Guid.NewGuid():N}",
            Email = $"user.{Guid.NewGuid():N}@station.org",
            InitialPassword = weakPassword,
            AstronautCode = $"AST-{Guid.NewGuid():N}",
            FirstName = "Weak",
            LastName = "Password",
            DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 180,
            WeightKg = 75
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _astronautService.CreateAstronautAsync(request));
        Assert.Contains("Password does not meet requirements", ex.Message);
    }

    [Fact]
    public async Task DeactivateAstronautAsync_LocksAccount_RevokesRefreshTokens_AndSetsIsActiveFalse()
    {
        var request = new CreateAstronautRequest
        {
            Username = "astro.deactivate",
            Email = "deactivate@station.org",
            InitialPassword = "ValidPassword123!",
            AstronautCode = "AST-DEACT-01",
            FirstName = "Target",
            LastName = "Deactivate",
            DateOfBirth = new DateTime(1985, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "A+",
            HeightCm = 175,
            WeightKg = 70
        };

        var created = await _astronautService.CreateAstronautAsync(request);

        // Add active refresh tokens for this user
        _context.RefreshTokens.AddRange(
            new RefreshToken
            {
                UserId = created.UserId!,
                TokenHash = "hash1",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            },
            new RefreshToken
            {
                UserId = created.UserId!,
                TokenHash = "hash2",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Perform deactivation
        var success = await _astronautService.DeactivateAstronautAsync(created.Id);
        Assert.True(success);

        // Verify Astronaut entity
        var astronaut = await _context.Astronauts.FindAsync(created.Id);
        Assert.NotNull(astronaut);
        Assert.False(astronaut.IsActive);

        // Verify Identity User is locked out
        var user = await _userManager.FindByIdAsync(created.UserId!);
        Assert.NotNull(user);
        Assert.True(await _userManager.IsLockedOutAsync(user));
        Assert.True(user.LockoutEnd > DateTimeOffset.UtcNow.AddYears(50));

        // Verify all refresh tokens are revoked
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == created.UserId && rt.RevokedAt == null)
            .ToListAsync();
        Assert.Empty(activeTokens);
    }

    [Fact]
    public async Task DeactivatedAstronaut_CannotLoginViaAuthService()
    {
        var request = new CreateAstronautRequest
        {
            Username = "astro.locked",
            Email = "locked@station.org",
            InitialPassword = "ValidPassword123!",
            AstronautCode = "AST-LOCK-01",
            FirstName = "Locked",
            LastName = "Astronaut",
            DateOfBirth = new DateTime(1985, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "B+",
            HeightCm = 168,
            WeightKg = 62
        };

        var created = await _astronautService.CreateAstronautAsync(request);

        // Verify login succeeds initially
        var loginResponse = await _authService.LoginAsync(new LoginRequest
        {
            Username = "astro.locked",
            Password = "ValidPassword123!"
        });
        Assert.NotNull(loginResponse);
        Assert.NotEmpty(loginResponse.Token);

        // Deactivate astronaut
        await _astronautService.DeactivateAstronautAsync(created.Id);

        // Login attempt after deactivation must fail with lockout exception
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(new LoginRequest
            {
                Username = "astro.locked",
                Password = "ValidPassword123!"
            }));
        Assert.Contains("locked", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAstronautsPagedAsync_ReturnsPagedAndFilteredResults()
    {
        var mission1 = new Mission
        {
            Code = "MSN-01",
            Name = "Mission 1",
            Destination = "Mars",
            StartDate = DateTime.UtcNow
        };
        var mission2 = new Mission
        {
            Code = "MSN-02",
            Name = "Mission 2",
            Destination = "Moon",
            StartDate = DateTime.UtcNow
        };
        _context.Missions.AddRange(mission1, mission2);
        await _context.SaveChangesAsync();

        var astro1 = await _astronautService.CreateAstronautAsync(new CreateAstronautRequest
        {
            Username = "paged.astro1",
            Email = "paged1@station.org",
            InitialPassword = "ValidPassword123!",
            AstronautCode = "AST-PAGE-01",
            FirstName = "Page",
            LastName = "One",
            DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "A+",
            HeightCm = 175,
            WeightKg = 70,
            MissionId = mission1.Id
        });

        var astro2 = await _astronautService.CreateAstronautAsync(new CreateAstronautRequest
        {
            Username = "paged.astro2",
            Email = "paged2@station.org",
            InitialPassword = "ValidPassword123!",
            AstronautCode = "AST-PAGE-02",
            FirstName = "Page",
            LastName = "Two",
            DateOfBirth = new DateTime(1982, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "O+",
            HeightCm = 168,
            WeightKg = 60,
            MissionId = mission2.Id
        });

        // Query without mission filter
        var allPaged = await _astronautService.GetAstronautsPagedAsync(null, pageNumber: 1, pageSize: 10);
        Assert.True(allPaged.TotalCount >= 2);

        // Query filtered by mission1
        var mission1Paged = await _astronautService.GetAstronautsPagedAsync(mission1.Id, pageNumber: 1, pageSize: 10);
        Assert.Single(mission1Paged.Items);
        Assert.Equal("AST-PAGE-01", mission1Paged.Items[0].AstronautCode);
    }

    [Fact]
    public async Task UpdateAstronautAsync_UpdatesProfileFieldsOnly()
    {
        var created = await _astronautService.CreateAstronautAsync(new CreateAstronautRequest
        {
            Username = "astro.update",
            Email = "update@station.org",
            InitialPassword = "ValidPassword123!",
            AstronautCode = "AST-UPD-01",
            FirstName = "Original",
            LastName = "Name",
            DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 180,
            WeightKg = 75
        });

        var updateRequest = new UpdateAstronautRequest
        {
            FirstName = "Modified",
            LastName = "Surname",
            DateOfBirth = new DateTime(1981, 2, 2, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Non-binary",
            BloodType = "A+",
            HeightCm = 182,
            WeightKg = 78,
            EmergencyContactInfo = "HQ Comm Link"
        };

        var updated = await _astronautService.UpdateAstronautAsync(created.Id, updateRequest);

        Assert.Equal("Modified Surname", updated.FullName);
        Assert.Equal("AST-UPD-01", updated.AstronautCode); // AstronautCode unchanged
        Assert.Equal(created.UserId, updated.UserId); // UserId unchanged
        Assert.True(updated.IsActive); // IsActive unchanged
        Assert.Equal("HQ Comm Link", updated.EmergencyContactInfo);

        // Verify Identity user FullName was updated
        var user = await _userManager.FindByIdAsync(created.UserId!);
        Assert.NotNull(user);
        Assert.Equal("Modified Surname", user.FullName);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
