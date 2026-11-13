using AstronautHealthMonitoring.Controllers;
using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AstronautHealthMonitoring.Tests;

public class ClinicalReferenceTests
{
    [Fact]
    public async Task Guides_UseDatabaseLimitsAndIncludeArabicInsteadOfFrontendCopies()
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Database.EnsureCreated();
        var config=new TypeAdapterConfig();config.Scan(typeof(GuideMappings).Assembly);
        var controller=new ClinicalReferenceController(db,new Mapper(config));
        var response=Assert.IsType<OkObjectResult>(await controller.Guides(default));
        var guides=Assert.IsType<List<Dictionary<string,string>>>(response.Value);
        Assert.Equal(6,guides.Count);
        var hr=guides.Single(g=>g["code"]=="HR");
        Assert.False(string.IsNullOrEmpty(hr["nameAr"]));
        var range=await db.ReferenceRanges.FirstAsync(r=>r.VitalSignType.Code=="HR");
        range.MinNormal=61;await db.SaveChangesAsync();
        var updated=Assert.IsType<OkObjectResult>(await controller.Guide("HR",default));
        Assert.Contains("61",Assert.IsType<Dictionary<string,string>>(updated.Value)["normalRange"]);
    }
    [Fact]
    public async Task LatestQuery_KeepsOneReadingPerAstronautAndMetric_WithStableTimestampTies()
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var now=DateTime.UtcNow;
        db.VitalSignReadings.AddRange(
            new VitalSignReading{Id=1,AstronautId=1,VitalSignTypeId=1,RecordedAt=now.AddMinutes(-1)},
            new VitalSignReading{Id=2,AstronautId=1,VitalSignTypeId=1,RecordedAt=now},
            new VitalSignReading{Id=3,AstronautId=1,VitalSignTypeId=1,RecordedAt=now},
            new VitalSignReading{Id=4,AstronautId=2,VitalSignTypeId=1,RecordedAt=now},
            new VitalSignReading{Id=5,AstronautId=1,VitalSignTypeId=2,RecordedAt=now});
        await db.SaveChangesAsync();
        Assert.Equal(new[]{3,4,5},await db.VitalSignReadings.LatestPerMetric().OrderBy(r=>r.Id).Select(r=>r.Id).ToArrayAsync());
    }
    [Fact]
    public void LatestQuery_TranslatesToPostgreSqlWithoutLoadingHistory()
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=localhost;Database=query_only").Options);
        var sql=db.VitalSignReadings.LatestPerMetric().Where(r=>r.AstronautId==1).Include(r=>r.VitalSignType).ToQueryString();
        Assert.Contains("NOT EXISTS",sql);
    }
}
