using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Alerts;
using AstronautHealthMonitoring.DTOs.SymptomGuidance;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AstronautHealthMonitoring.Services.Implementations;

public class SymptomGuidanceService : ISymptomGuidanceService
{
    private readonly IConfiguration? _configuration;
    private readonly RiskProbabilityPredictor? _probabilityPredictor;
    private readonly AppDbContext _dbContext;
    private readonly IHealthAlertService _healthAlertService;
    private readonly IMapper _mapper;
    private readonly ILogger<SymptomGuidanceService> _logger;

    public SymptomGuidanceService(
        AppDbContext dbContext,
        IHealthAlertService healthAlertService,
        IMapper mapper,
        ILogger<SymptomGuidanceService> logger, IConfiguration? configuration=null,RiskProbabilityPredictor? probabilityPredictor=null)
    {
        _configuration=configuration;
        _probabilityPredictor=probabilityPredictor;
        _dbContext = dbContext;
        _healthAlertService = healthAlertService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SymptomDto>> GetAllSymptomsAsync(CancellationToken cancellationToken = default)
    {
        var symptoms = await _dbContext.Symptoms
            .AsNoTracking()
            .OrderBy(s => s.BodyArea)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<SymptomDto>>(symptoms);
    }

    public async Task<IReadOnlyList<SpaceConditionDto>> GetAllConditionsAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SpaceConditions
            .AsNoTracking()
            .Include(c => c.ConditionSymptoms)
                .ThenInclude(cs => cs.Symptom)
            .Include(c => c.ConditionVitalSigns)
                .ThenInclude(cv => cv.VitalSignType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(c => c.Name.ToUpper().Contains(term) ||
                                     c.Code.ToUpper().Contains(term) ||
                                     c.Description.ToUpper().Contains(term));
        }

        var conditions = await query
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<SpaceConditionDto>>(conditions);
    }

    public async Task<SymptomAssessmentResponseDto> EvaluateSymptomsAsync(EvaluateSymptomsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var astronaut = await _dbContext.Astronauts.FirstOrDefaultAsync(a => a.Id == request.AstronautId, cancellationToken);
        if (astronaut == null)
        {
            throw new KeyNotFoundException($"Astronaut with ID {request.AstronautId} not found.");
        }

        if (!astronaut.IsActive)
        {
            throw new InvalidOperationException($"Astronaut '{astronaut.AstronautCode}' is deactivated and cannot undergo new symptom assessment.");
        }

        var response = new SymptomAssessmentResponseDto();

        if (request.SymptomCodes == null || request.SymptomCodes.Count == 0)
        {
            return response;
        }

        var normalizedCodes = request.SymptomCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        if (normalizedCodes.Count == 0)
        {
            return response;
        }

        var symptoms = await _dbContext.Symptoms
            .AsNoTracking()
            .Where(s => normalizedCodes.Contains(s.Code.ToUpper()))
            .ToListAsync(cancellationToken);

        var redFlags = symptoms.Where(s => s.IsRedFlag).ToList();

        if (symptoms.Count != normalizedCodes.Count)
            throw new InvalidOperationException("One or more symptom codes are not in the symptom catalog.");
        if (redFlags.Count > 0)
        {
            response.ShouldEscalate = true;
            response.EscalationReason = $"Red flag symptom(s) reported: {string.Join(", ", redFlags.Select(rf => rf.Name))}. Immediate medical evaluation required.";

            try
            {
                var alert = await _healthAlertService.TriggerManualAlertAsync(new TriggerManualAlertRequest
                {
                    AstronautId = astronaut.Id,
                    Severity = AlertSeverity.Emergency,
                    Title = "Red-Flag Symptom Escalation",
                    Message = $"Astronaut '{astronaut.AstronautCode}' reported red-flag symptom(s): {string.Join(", ", redFlags.Select(rf => rf.Name))}.",
                    Reason = "Automated escalation triggered by symptom guidance assessment detecting red-flag clinical signs.",
                    ActionRequired = "Immediate crew check and flight surgeon evaluation required."
                }, cancellationToken);

                response.TriggeredAlertId = alert.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to automatically trigger health alert during symptom assessment for astronaut {AstronautId}", astronaut.Id);
                response.AlertDeliveryError = "Emergency notification could not be saved. Contact the medical officer immediately and retry when connected.";
                response.Reasons.Add(response.AlertDeliveryError);
            }
        }

        var cutoff = DateTime.UtcNow.AddHours(-24);
        var readings = await _dbContext.VitalSignReadings.AsNoTracking()
            .Include(r => r.VitalSignType)
            .Where(r => r.AstronautId == request.AstronautId && r.RecordedAt >= cutoff && r.RecordedAt<=DateTime.UtcNow)
            .ToListAsync(cancellationToken);
        var latestReadings = readings.GroupBy(r => r.VitalSignTypeId)
            .Select(g => g.OrderByDescending(r => r.RecordedAt).First()).ToList();
        var latestSleep = await _dbContext.SleepRecords.AsNoTracking()
            .Where(r => r.AstronautId == request.AstronautId && r.RecordedAt >= cutoff)
            .OrderByDescending(r => r.RecordedAt).FirstOrDefaultAsync(cancellationToken);
        var triage = SymptomAssessmentRules.Evaluate(request, symptoms, latestReadings, latestSleep?.DurationHours);
        triage.TriggeredAlertId=response.TriggeredAlertId;triage.AlertDeliveryError=response.AlertDeliveryError;
        if(response.AlertDeliveryError is not null)triage.Reasons.Add(response.AlertDeliveryError);
        response=triage;
        var conditions = await _dbContext.SpaceConditions.AsNoTracking()
            .Include(c=>c.ConditionSymptoms).ThenInclude(link=>link.Symptom)
            .Include(c=>c.ConditionVitalSigns).ToListAsync(cancellationToken);
        var context=await AssociationContextResolver.ResolveAsync(_dbContext,request.AstronautId,_configuration,DateTime.UtcNow,cancellationToken);
        SymptomAssociationEngine.Apply(response,normalizedCodes,conditions,latestReadings,context);
        SymptomFindingClassifier.Apply(response,request,symptoms,conditions,latestReadings,latestSleep?.DurationHours);
        if(response.Level==3 && !response.TriggeredAlertId.HasValue && redFlags.Count==0)
        {
            var critical=string.Join(", ",response.ConditionMatches.Where(c=>c.FollowUpLevel==3).Select(c=>c.ConditionCode));
            try {
                var alert=await _healthAlertService.TriggerManualAlertAsync(new TriggerManualAlertRequest {
                    AstronautId=astronaut.Id,Severity=AlertSeverity.Emergency,Title="Critical Assessment Finding",
                    Message=$"Critical findings require medical review: {critical}",Reason="A matching symptom pattern has a related recent emergency vital reading.",
                    ActionRequired="Alert crew and the medical officer now and follow the mission emergency protocol."
                },cancellationToken);
                response.TriggeredAlertId=alert.Id;
            } catch(Exception error) {
                _logger.LogError(error,"Critical assessment alert could not be saved for astronaut {AstronautId}",astronaut.Id);
                response.AlertDeliveryError="Emergency notification could not be saved. Contact the medical officer immediately and retry when connected.";
            }
        }
        AssociationGuidance.Apply(response,request.Locale);
        // Alerts have already been handled; an optional remote model never delays their dispatch.
        if(_probabilityPredictor is not null)await _probabilityPredictor.ApplyAsync(response,cancellationToken);
        return response;
    }
}
