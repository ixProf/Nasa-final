using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Recommendations;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Implementations;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class RecommendationServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly RecommendationService _service;

    public RecommendationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"RecommendationServiceDb_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        var config = new TypeAdapterConfig();
        new RecommendationMappings().Register(config);
        _mapper = new Mapper(config);

        _service = new RecommendationService(_context, _mapper, NullLogger<RecommendationService>.Instance);
    }

    [Fact]
    public async Task CreateRecommendationAsync_ValidRequest_CreatesPendingRecommendation()
    {
        var astronaut = new Astronaut
        {
            AstronautCode = "AST-REC-01",
            FirstName = "Kjell",
            LastName = "Lindgren",
            FullName = "Kjell Lindgren",
            DateOfBirth = new DateTime(1973, 1, 23, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 178,
            WeightKg = 74,
            IsActive = true
        };
        _context.Astronauts.Add(astronaut);
        await _context.SaveChangesAsync();

        var request = new CreateRecommendationRequest
        {
            AstronautId = astronaut.Id,
            Title = "Post-EVA Rehydration Protocol",
            Content = "Consume 750ml electrolyte solution within 1 hour post-hatch closure.",
            Reason = "Prevent hypovolemia and counteract perspiration losses during spacewalk.",
            Category = RecommendationCategory.Medical,
            Priority = RecommendationPriority.High,
            ActionDueDate = DateTime.UtcNow.AddHours(2)
        };

        var result = await _service.CreateRecommendationAsync(astronaut.Id, request);

        Assert.NotNull(result);
        Assert.Equal("Post-EVA Rehydration Protocol", result.Title);
        Assert.Equal("Kjell Lindgren", result.AstronautFullName);
        Assert.Equal(astronaut.Id, result.AstronautId);
        Assert.Equal(RecommendationStatus.Pending, result.Status);
        Assert.Equal(RecommendationPriority.High, result.Priority);
        Assert.Equal(RecommendationCategory.Medical, result.Category);

        var inDb = await _context.Recommendations.FindAsync(result.Id);
        Assert.NotNull(inDb);
        Assert.Equal(RecommendationStatus.Pending, inDb.Status);
    }

    [Fact]
    public async Task CreateRecommendationAsync_WhenAstronautDeactivated_ThrowsInvalidOperationException()
    {
        var astronaut = new Astronaut
        {
            AstronautCode = "AST-REC-DEACT",
            FirstName = "Grounded",
            LastName = "Astronaut",
            FullName = "Grounded Astronaut",
            DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "A+",
            HeightCm = 165,
            WeightKg = 58,
            IsActive = false
        };
        _context.Astronauts.Add(astronaut);
        await _context.SaveChangesAsync();

        var request = new CreateRecommendationRequest
        {
            AstronautId = astronaut.Id,
            Title = "Sleep Hygiene Protocol",
            Content = "Wind-down routine.",
            Reason = "Circadian alignment",
            Category = RecommendationCategory.Sleep,
            Priority = RecommendationPriority.Medium
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateRecommendationAsync(astronaut.Id, request));
        Assert.Contains("Cannot issue recommendations for deactivated astronaut", ex.Message);
    }

    [Fact]
    public async Task CreateRecommendationAsync_WhenAstronautDoesNotExist_ThrowsKeyNotFoundException()
    {
        var request = new CreateRecommendationRequest
        {
            AstronautId = 999999,
            Title = "Non-existent astronaut",
            Content = "Test content",
            Reason = "Test reason"
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.CreateRecommendationAsync(999999, request));
    }

    [Fact]
    public async Task GetRecommendationsForAstronautAsync_FiltersPendingAndOrdersByPriority()
    {
        var astronaut = new Astronaut
        {
            AstronautCode = "AST-REC-QUERY",
            FirstName = "Sunita",
            LastName = "Williams",
            FullName = "Sunita Williams",
            DateOfBirth = new DateTime(1965, 9, 19, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "A+",
            HeightCm = 173,
            WeightKg = 64,
            IsActive = true
        };
        _context.Astronauts.Add(astronaut);
        await _context.SaveChangesAsync();

        _context.Recommendations.AddRange(
            new Recommendation
            {
                AstronautId = astronaut.Id,
                Title = "Low Priority Pending",
                Content = "Content A",
                Reason = "Reason A",
                Priority = RecommendationPriority.Low,
                Status = RecommendationStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new Recommendation
            {
                AstronautId = astronaut.Id,
                Title = "High Priority Pending",
                Content = "Content B",
                Reason = "Reason B",
                Priority = RecommendationPriority.High,
                Status = RecommendationStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            },
            new Recommendation
            {
                AstronautId = astronaut.Id,
                Title = "Completed Recommendation",
                Content = "Content C",
                Reason = "Reason C",
                Priority = RecommendationPriority.Urgent,
                Status = RecommendationStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            }
        );
        await _context.SaveChangesAsync();

        // 1. Pending only
        var pendingRecs = await _service.GetRecommendationsForAstronautAsync(astronaut.Id, pendingOnly: true);
        Assert.Equal(2, pendingRecs.Count);
        Assert.Equal("High Priority Pending", pendingRecs[0].Title); // High priority first

        // 2. All recommendations
        var allRecs = await _service.GetRecommendationsForAstronautAsync(astronaut.Id, pendingOnly: false);
        Assert.Equal(3, allRecs.Count);
        Assert.Equal("Completed Recommendation", allRecs[0].Title); // Urgent priority first
    }

    [Fact]
    public async Task UpdateStatusAsync_AstronautUpdatesOwnRecommendation_Succeeds()
    {
        var astronaut = new Astronaut
        {
            AstronautCode = "AST-REC-STATUS",
            FirstName = "Drew",
            LastName = "Morgan",
            FullName = "Drew Morgan",
            DateOfBirth = new DateTime(1976, 2, 5, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "B+",
            HeightCm = 180,
            WeightKg = 77,
            IsActive = true
        };
        _context.Astronauts.Add(astronaut);
        await _context.SaveChangesAsync();

        var rec = new Recommendation
        {
            AstronautId = astronaut.Id,
            Title = "Resistance Band Exercise",
            Content = "Perform 3 sets of leg presses on ARED.",
            Reason = "Maintain bone mineral density in microgravity.",
            Status = RecommendationStatus.Pending
        };
        _context.Recommendations.Add(rec);
        await _context.SaveChangesAsync();

        // Step 1: Pending -> Acknowledged
        var ackRequest = new UpdateRecommendationStatusRequest
        {
            Status = RecommendationStatus.Acknowledged
        };
        var acked = await _service.UpdateStatusAsync(rec.Id, astronaut.Id, ackRequest);
        Assert.Equal(RecommendationStatus.Acknowledged, acked.Status);

        // Step 2: Acknowledged -> Completed
        var completeRequest = new UpdateRecommendationStatusRequest
        {
            Status = RecommendationStatus.Completed
        };
        var updated = await _service.UpdateStatusAsync(rec.Id, astronaut.Id, completeRequest);
        Assert.Equal(RecommendationStatus.Completed, updated.Status);

        var inDb = await _context.Recommendations.FindAsync(rec.Id);
        Assert.Equal(RecommendationStatus.Completed, inDb!.Status);
        Assert.NotNull(inDb.UpdatedAt);
    }

    [Fact]
    public async Task UpdateStatusAsync_AstronautCannotUpdateAnotherAstronautsRecommendation()
    {
        var astronaut1 = new Astronaut
        {
            AstronautCode = "AST-REC-A1",
            FirstName = "Astro",
            LastName = "One",
            FullName = "Astro One",
            DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 175,
            WeightKg = 70,
            IsActive = true
        };
        var astronaut2 = new Astronaut
        {
            AstronautCode = "AST-REC-A2",
            FirstName = "Astro",
            LastName = "Two",
            FullName = "Astro Two",
            DateOfBirth = new DateTime(1982, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "A+",
            HeightCm = 168,
            WeightKg = 60,
            IsActive = true
        };
        _context.Astronauts.AddRange(astronaut1, astronaut2);
        await _context.SaveChangesAsync();

        var rec = new Recommendation
        {
            AstronautId = astronaut1.Id,
            Title = "Astro1 Recommendation",
            Content = "Content",
            Reason = "Reason",
            Status = RecommendationStatus.Pending
        };
        _context.Recommendations.Add(rec);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateRecommendationStatusRequest
        {
            Status = RecommendationStatus.Acknowledged
        };

        // Astronaut2 attempts to update Astronaut1's recommendation
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.UpdateStatusAsync(rec.Id, astronaut2.Id, updateRequest));
        Assert.Contains("Astronauts can only update their own recommendations", ex.Message);
    }

    [Theory]
    // 4 Valid Transitions
    [InlineData(RecommendationStatus.Pending, RecommendationStatus.Acknowledged, true)]
    [InlineData(RecommendationStatus.Pending, RecommendationStatus.Dismissed, true)]
    [InlineData(RecommendationStatus.Acknowledged, RecommendationStatus.Completed, true)]
    [InlineData(RecommendationStatus.Acknowledged, RecommendationStatus.Dismissed, true)]
    // 12 Invalid Transitions
    [InlineData(RecommendationStatus.Pending, RecommendationStatus.Pending, false)]
    [InlineData(RecommendationStatus.Pending, RecommendationStatus.Completed, false)]
    [InlineData(RecommendationStatus.Acknowledged, RecommendationStatus.Pending, false)]
    [InlineData(RecommendationStatus.Acknowledged, RecommendationStatus.Acknowledged, false)]
    [InlineData(RecommendationStatus.Completed, RecommendationStatus.Pending, false)]
    [InlineData(RecommendationStatus.Completed, RecommendationStatus.Acknowledged, false)]
    [InlineData(RecommendationStatus.Completed, RecommendationStatus.Completed, false)]
    [InlineData(RecommendationStatus.Completed, RecommendationStatus.Dismissed, false)]
    [InlineData(RecommendationStatus.Dismissed, RecommendationStatus.Pending, false)]
    [InlineData(RecommendationStatus.Dismissed, RecommendationStatus.Acknowledged, false)]
    [InlineData(RecommendationStatus.Dismissed, RecommendationStatus.Completed, false)]
    [InlineData(RecommendationStatus.Dismissed, RecommendationStatus.Dismissed, false)]
    public async Task UpdateStatusAsync_EnforcesTransitionMatrix(RecommendationStatus initialStatus, RecommendationStatus targetStatus, bool isValid)
    {
        var astronaut = new Astronaut
        {
            AstronautCode = $"AST-TR-{Guid.NewGuid():N}",
            FirstName = "Test",
            LastName = "Transition",
            FullName = "Test Transition",
            DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 175,
            WeightKg = 70,
            IsActive = true
        };
        _context.Astronauts.Add(astronaut);
        await _context.SaveChangesAsync();

        var rec = new Recommendation
        {
            AstronautId = astronaut.Id,
            Title = "Transition Test",
            Content = "Content",
            Reason = "Reason",
            Status = initialStatus
        };
        _context.Recommendations.Add(rec);
        await _context.SaveChangesAsync();

        var request = new UpdateRecommendationStatusRequest { Status = targetStatus };

        if (isValid)
        {
            var result = await _service.UpdateStatusAsync(rec.Id, astronaut.Id, request);
            Assert.Equal(targetStatus, result.Status);
        }
        else
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateStatusAsync(rec.Id, astronaut.Id, request));
            Assert.Contains("Invalid recommendation status transition", ex.Message);
        }
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
