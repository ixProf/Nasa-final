using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Implementations;
using AstronautHealthMonitoring.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class VitalSignStatusEvaluatorTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IVitalSignStatusEvaluator _evaluator;

    public VitalSignStatusEvaluatorTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"AstronautHealth_EvalTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated(); // Seeds all data via ModelBuilderExtensions.Seed()

        _evaluator = new VitalSignStatusEvaluator();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private ReferenceRange GetRangeByCode(string code)
    {
        return _context.ReferenceRanges
            .Include(r => r.VitalSignType)
            .First(r => r.VitalSignType.Code == code && r.IsActive);
    }

    [Theory]
    [InlineData(45, VitalSignStatus.MedicalAttention)]
    [InlineData(105, VitalSignStatus.MedicalAttention)]
    [InlineData(150, VitalSignStatus.Emergency)]
    public void HeartRate_Evaluates_Correctly(double heartRate, VitalSignStatus expectedStatus)
    {
        var range = GetRangeByCode("HR");
        var status = _evaluator.Evaluate(range, heartRate);
        Assert.Equal(expectedStatus, status);
    }

    [Theory]
    [InlineData(125, 70, VitalSignStatus.MedicalAttention)]
    [InlineData(95, 55, VitalSignStatus.MedicalAttention)]
    [InlineData(170, 70, VitalSignStatus.Emergency)]
    public void BloodPressure_Evaluates_Correctly(double systolic, double diastolic, VitalSignStatus expectedStatus)
    {
        var range = GetRangeByCode("BP");
        var status = _evaluator.Evaluate(range, systolic, diastolic);
        Assert.Equal(expectedStatus, status);
    }

    [Theory]
    [InlineData(97, VitalSignStatus.Routine)]
    [InlineData(92, VitalSignStatus.MedicalAttention)]
    [InlineData(85, VitalSignStatus.Emergency)]
    public void SpO2_Evaluates_Correctly(double spO2, VitalSignStatus expectedStatus)
    {
        var range = GetRangeByCode("SPO2");
        var status = _evaluator.Evaluate(range, spO2);
        Assert.Equal(expectedStatus, status);
    }

    [Theory]
    [InlineData(37.5, VitalSignStatus.MedicalAttention)]
    [InlineData(39.0, VitalSignStatus.Emergency)]
    public void Temperature_Evaluates_Correctly(double temp, VitalSignStatus expectedStatus)
    {
        var range = GetRangeByCode("TEMP");
        var status = _evaluator.Evaluate(range, temp);
        Assert.Equal(expectedStatus, status);
    }

    [Theory]
    [InlineData(105, VitalSignStatus.MedicalAttention)]
    [InlineData(45, VitalSignStatus.Emergency)]
    public void Glucose_Evaluates_Correctly(double glucose, VitalSignStatus expectedStatus)
    {
        var range = GetRangeByCode("GLUCOSE");
        var status = _evaluator.Evaluate(range, glucose);
        Assert.Equal(expectedStatus, status);
    }

    [Theory]
    [InlineData(20, VitalSignStatus.MedicalAttention)]
    public void RespiratoryRate_Evaluates_Correctly(double resp, VitalSignStatus expectedStatus)
    {
        var range = GetRangeByCode("RESP");
        var status = _evaluator.Evaluate(range, resp);
        Assert.Equal(expectedStatus, status);
    }

    [Theory]
    [InlineData(60, VitalSignStatus.Routine)]          // exactly MinNormal
    [InlineData(100, VitalSignStatus.Routine)]         // exactly MaxNormal
    [InlineData(40, VitalSignStatus.MedicalAttention)] // exactly EmergencyBelow (not emergency)
    [InlineData(140, VitalSignStatus.MedicalAttention)]// exactly EmergencyAbove (not emergency)
    public void HeartRate_BoundaryValues_Evaluates_Correctly(double heartRate, VitalSignStatus expectedStatus)
    {
        var range = GetRangeByCode("HR");
        var status = _evaluator.Evaluate(range, heartRate);
        Assert.Equal(expectedStatus, status);
    }

    [Fact]
    public void SpO2_BoundaryValue_90_IsMedicalAttention()
    {
        // SpO2 EmergencyBelow is 90. Exactly 90 is NOT strictly below 90, so it evaluates to MedicalAttention.
        var range = GetRangeByCode("SPO2");
        var status = _evaluator.Evaluate(range, 90);
        Assert.Equal(VitalSignStatus.MedicalAttention, status);
    }

    [Fact]
    public void BloodPressure_Diastolic101_WithNormalSystolic_IsEmergency()
    {
        // Systolic 110 is normal [90, 120], but diastolic 101 strictly exceeds SecondaryEmergencyAbove (100).
        var range = GetRangeByCode("BP");
        var status = _evaluator.Evaluate(range, 110, 101);
        Assert.Equal(VitalSignStatus.Emergency, status);
    }

    [Fact]
    public void BloodPressure_PairedRange_NullSecondary_ThrowsArgumentException()
    {
        var range = GetRangeByCode("BP");
        var exception = Assert.Throws<ArgumentException>(() => _evaluator.Evaluate(range, 120, null));
        Assert.Equal("secondaryValue", exception.ParamName);
    }

    [Fact]
    public void Evaluate_NullRange_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _evaluator.Evaluate(null!, 75));
    }
}
