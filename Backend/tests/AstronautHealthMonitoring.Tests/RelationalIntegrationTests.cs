using System.Data.Common;
using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Astronaut;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Chat;
using AstronautHealthMonitoring.DTOs.Mood;
using AstronautHealthMonitoring.DTOs.Sleep;
using AstronautHealthMonitoring.DTOs.Sync;
using AstronautHealthMonitoring.DTOs.VitalSigns;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Implementations;
using AstronautHealthMonitoring.Services.Interfaces;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

/// <summary>
/// Interceptor counting real ADO.NET SQL commands executed against relational SQLite.
/// </summary>
public class CommandCountingInterceptor : DbCommandInterceptor
{
    public int CommandCount { get; private set; }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        CommandCount++;
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        CommandCount++;
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public void Reset() => CommandCount = 0;
}

/// <summary>
/// Relational integration tests executed against SQLite in-memory (real relational DB provider with transactions,
/// foreign keys, and unique indexes) when Docker is unavailable in the environment.
/// </summary>
[Trait("Category", "Integration")]
public class RelationalIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CommandCountingInterceptor _interceptor;
    private readonly IServiceProvider _serviceProvider;
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public RelationalIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _interceptor = new CommandCountingInterceptor();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(_connection);
            options.AddInterceptors(_interceptor);
        });

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

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<AppDbContext>();

        // Create relational schema in SQLite
        _context.Database.EnsureCreated();

        // Seed Identity roles
        var roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        roleManager.CreateAsync(new IdentityRole(UserRoles.Astronaut)).GetAwaiter().GetResult();
        roleManager.CreateAsync(new IdentityRole(UserRoles.Monitor)).GetAwaiter().GetResult();

        var config = new TypeAdapterConfig();
        new VitalSignMappings().Register(config);
        new AlertMappings().Register(config);
        new SleepMappings().Register(config);
        new MoodMappings().Register(config);
        new ChatMappings().Register(config);
        new AstronautMappings().Register(config);
        new MissionMappings().Register(config);
        _mapper = new Mapper(config);

        _context.Astronauts.Add(new Astronaut
        {
            Id = 1,
            AstronautCode = "AST-REL-01",
            FirstName = "Relational",
            LastName = "Astronaut",
            FullName = "Relational Astronaut",
            DateOfBirth = new DateTime(1985, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 180,
            WeightKg = 75,
            IsActive = true
        });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task Unique_ClientGeneratedId_IsEnforcedByRelationalDatabase()
    {
        var sharedClientGeneratedId = Guid.NewGuid();

        var reading1 = new VitalSignReading
        {
            ClientGeneratedId = sharedClientGeneratedId,
            AstronautId = 1,
            VitalSignTypeId = 1,
            Value = 72,
            MeasurementMethod = "Continuous Telemetry",
            SensorLocation = "Chest Band",
            RecordedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            Status = VitalSignStatus.Routine
        };

        _context.VitalSignReadings.Add(reading1);
        await _context.SaveChangesAsync();

        var reading2 = new VitalSignReading
        {
            ClientGeneratedId = sharedClientGeneratedId, // Duplicate!
            AstronautId = 1,
            VitalSignTypeId = 1,
            Value = 75,
            MeasurementMethod = "Continuous Telemetry",
            SensorLocation = "Chest Band",
            RecordedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            Status = VitalSignStatus.Routine
        };

        _context.VitalSignReadings.Add(reading2);

        // SQLite relational provider must throw DbUpdateException due to unique index violation
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
        Assert.NotNull(ex.InnerException);
        Assert.Contains("UNIQUE constraint failed", ex.InnerException.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SyncService_Idempotency_RelationalDatabase_SameBatchTwice_ReturnsDuplicatesWithoutError()
    {
        var currentUserService = new TestCurrentUserService
        {
            Role = UserRoles.Astronaut,
            UserId = "rel-astro-01",
            AstronautId = 1,
            FullName = "Relational Astronaut",
            IsAuthenticated = true
        };

        var evaluator = new VitalSignStatusEvaluator();
        var alertService = new HealthAlertService(_context, currentUserService, _mapper);
        var vitalSignService = new VitalSignService(_context, currentUserService, evaluator, alertService, _mapper);

        var configValues = new Dictionary<string, string?>
        {
            { "Sync:MaxBatchSize", "200" },
            { "Sync:FutureToleranceMinutes", "5" },
            { "Sync:MaxAgeDays", "30" }
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();

        var sleepService = new SleepService(_context, currentUserService, _mapper);
        var moodService = new MoodService(_context, currentUserService, _mapper);
        var chatService = new ChatService(_context, _mapper, NullLogger<ChatService>.Instance);

        var syncService = new SyncService(
            _context,
            vitalSignService,
            sleepService,
            moodService,
            chatService,
            configuration,
            NullLogger<SyncService>.Instance);

        var now = DateTime.UtcNow.AddMinutes(-5);
        var batch = new SyncBatchRequest
        {
            DeviceSyncTime = DateTime.UtcNow,
            VitalSignReadings = new List<CreateVitalSignReadingRequest>
            {
                new()
                {
                    ClientGeneratedId = Guid.NewGuid(),
                    VitalSignTypeId = 1,
                    Value = 70,
                    MeasurementMethod = "Chest Sensor",
                    SensorLocation = "Chest",
                    RecordedAt = now
                }
            },
            SleepRecords = new List<CreateSleepRecordRequest>
            {
                new()
                {
                    ClientGeneratedId = Guid.NewGuid(),
                    RecordedAt = now,
                    StartTime = now.AddHours(-7),
                    EndTime = now,
                    DeepSleepMinutes = 100,
                    RemSleepMinutes = 90,
                    LightSleepMinutes = 210,
                    AwakeMinutes = 20
                }
            },
            MoodEntries = new List<CreateMoodEntryRequest>
            {
                new()
                {
                    ClientGeneratedId = Guid.NewGuid(),
                    RecordedAt = now,
                    MoodScore = 4,
                    StressLevel = 2,
                    DominantEmotion = "Calm"
                }
            },
            ChatMessages = new List<SendChatMessageRequest>
            {
                new()
                {
                    ClientGeneratedId = Guid.NewGuid(),
                    Content = "Testing relational sync idempotency.",
                    RecordedAt = now
                }
            }
        };

        // First execution: full acceptance
        var firstRun = await syncService.ProcessSyncBatchAsync(1, batch, "rel-astro-01");
        Assert.True(firstRun.Success);
        Assert.Equal(1, firstRun.VitalSignReadingsProcessed);
        Assert.Equal(1, firstRun.SleepRecordsProcessed);
        Assert.Equal(1, firstRun.MoodEntriesProcessed);
        Assert.Equal(1, firstRun.ChatMessagesProcessed);
        Assert.Equal(0, firstRun.DuplicatesIgnoredCount);

        var initialVitalCount = await _context.VitalSignReadings.CountAsync();
        var initialSleepCount = await _context.SleepRecords.CountAsync();
        var initialMoodCount = await _context.MoodEntries.CountAsync();
        var initialChatCount = await _context.ChatMessages.CountAsync();

        // Second execution: exact same batch -> all duplicates, zero new rows inserted!
        var secondRun = await syncService.ProcessSyncBatchAsync(1, batch, "rel-astro-01");
        Assert.True(secondRun.Success);
        Assert.Equal(0, secondRun.VitalSignReadingsProcessed);
        Assert.Equal(0, secondRun.SleepRecordsProcessed);
        Assert.Equal(0, secondRun.MoodEntriesProcessed);
        Assert.Equal(0, secondRun.ChatMessagesProcessed);
        Assert.Equal(4, secondRun.DuplicatesIgnoredCount);
        Assert.All(secondRun.ItemResults, r => Assert.Equal(SyncItemStatus.Duplicate, r.Status));

        // Assert database row count did not increase
        Assert.Equal(initialVitalCount, await _context.VitalSignReadings.CountAsync());
        Assert.Equal(initialSleepCount, await _context.SleepRecords.CountAsync());
        Assert.Equal(initialMoodCount, await _context.MoodEntries.CountAsync());
        Assert.Equal(initialChatCount, await _context.ChatMessages.CountAsync());
    }

    [Fact]
    public async Task AstronautService_ProvisioningRollback_WhenIdentityFails_AstronautRowIsRolledBack()
    {
        var userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var astronautService = new AstronautService(_context, userManager, _mapper, NullLogger<AstronautService>.Instance);

        // Pre-create a user with username "existing.user" to force an Identity duplicate failure
        var duplicateUser = new ApplicationUser
        {
            UserName = "existing.user",
            Email = "other@space.org",
            FullName = "Existing User"
        };
        await userManager.CreateAsync(duplicateUser, "StrongPass123!");

        var request = new CreateAstronautRequest
        {
            Username = "existing.user", // Will conflict with existing Identity user!
            Email = "new.unique@space.org",
            AstronautCode = "AST-CONFLICT-01",
            FirstName = "Should",
            LastName = "Rollback",
            InitialPassword = "StrongPass123!",
            DateOfBirth = new DateTime(1985, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "A+",
            HeightCm = 170,
            WeightKg = 65
        };

        // Provisioning fails due to duplicate username
        await Assert.ThrowsAsync<InvalidOperationException>(() => astronautService.CreateAstronautAsync(request));

        // Relational transaction check: Astronaut code must NOT exist in SQLite database!
        var astronautExists = await _context.Astronauts.AnyAsync(a => a.AstronautCode == "AST-CONFLICT-01");
        Assert.False(astronautExists, "Astronaut row must be rolled back and not persisted when user creation fails.");
    }

    [Fact]
    public async Task DashboardService_IssuesBoundedDbCommands_RegardlessOfCrewSize_RealRelationalInterceptor()
    {
        // Clear astronauts and add 3 astronauts
        var existing = await _context.Astronauts.ToListAsync();
        _context.Astronauts.RemoveRange(existing);
        await _context.SaveChangesAsync();

        for (int i = 1; i <= 3; i++)
        {
            _context.Astronauts.Add(new Astronaut
            {
                Id = 100 + i,
                AstronautCode = $"AST-CREW-{i:D2}",
                FirstName = $"Crew",
                LastName = $"{i}",
                FullName = $"Crew {i}",
                DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Male",
                BloodType = "O+",
                HeightCm = 175,
                WeightKg = 70,
                IsActive = true
            });
        }
        await _context.SaveChangesAsync();

        var dashboardService = new DashboardService(
            _context,
            _mapper,
            NullLogger<DashboardService>.Instance);

        // Run dashboard with 3 crew members
        _interceptor.Reset();
        var dashboard3 = await dashboardService.GetMonitorDashboardAsync();
        Assert.Equal(3, dashboard3.CrewSummaries.Count);
        var commandsFor3 = _interceptor.CommandCount;

        // Now add 7 more crew members (total 10 crew members)
        for (int i = 4; i <= 10; i++)
        {
            _context.Astronauts.Add(new Astronaut
            {
                Id = 100 + i,
                AstronautCode = $"AST-CREW-{i:D2}",
                FirstName = $"Crew",
                LastName = $"{i}",
                FullName = $"Crew {i}",
                DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Male",
                BloodType = "O+",
                HeightCm = 175,
                WeightKg = 70,
                IsActive = true
            });
        }
        await _context.SaveChangesAsync();

        // Run dashboard with 10 crew members
        _interceptor.Reset();
        var dashboard10 = await dashboardService.GetMonitorDashboardAsync();
        Assert.Equal(10, dashboard10.CrewSummaries.Count);
        var commandsFor10 = _interceptor.CommandCount;

        // Relational Command Interceptor Proof:
        // Command count must be identical whether crew size is 3 or 10 ($O(1)$ commands, zero N+1 queries).
        Assert.True(commandsFor3 > 0, "Queries must be executed via ADO.NET DbCommand.");
        Assert.Equal(commandsFor3, commandsFor10);
    }
}

