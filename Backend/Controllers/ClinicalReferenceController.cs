using System.Globalization;
using System.Text.Json;
using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Emergency;
using AstronautHealthMonitoring.DTOs.ReferenceRanges;
using AstronautHealthMonitoring.Models.Entities;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AstronautHealthMonitoring.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.Authenticated)]
public class ClinicalReferenceController(AppDbContext db, IMapper mapper) : ControllerBase
{
    private static readonly Dictionary<string, Dictionary<string,string>> Translations = LoadTranslations();

    [HttpGet("api/reference-ranges")]
    public async Task<IActionResult> Ranges(CancellationToken ct)
    {
        var rows=await db.ReferenceRanges.AsNoTracking().Include(r=>r.VitalSignType).Where(r=>r.IsActive).ToListAsync(ct);
        return Ok(mapper.Map<List<ReferenceRangeDto>>(rows));
    }

    [HttpGet("api/emergency-procedures")]
    public async Task<IActionResult> Procedures(CancellationToken ct) =>
        Ok(mapper.Map<List<EmergencyProcedureDto>>(await db.EmergencyProcedures.AsNoTracking().OrderBy(p=>p.Code).ToListAsync(ct)));

    [HttpGet("api/guides")]
    public async Task<IActionResult> Guides(CancellationToken ct) => Ok(await BuildGuidesAsync(ct));

    [HttpGet("api/guides/{code}")]
    public async Task<IActionResult> Guide(string code,CancellationToken ct)
    {
        var guide=(await BuildGuidesAsync(ct)).FirstOrDefault(g=>g["code"].Equals(code,StringComparison.OrdinalIgnoreCase));
        return guide is null ? NotFound() : Ok(guide);
    }

    private async Task<List<Dictionary<string,string>>> BuildGuidesAsync(CancellationToken ct)
    {
        var guides=await db.HealthMetricGuides.AsNoTracking().Include(g=>g.VitalSignType).ToListAsync(ct);
        var ranges=await db.ReferenceRanges.AsNoTracking().Where(r=>r.IsActive).ToListAsync(ct);
        var result=new List<Dictionary<string,string>>();
        foreach(var guide in guides)
        {
            var code=guide.VitalSignType.Code;
            var item=Translations.TryGetValue(code,out var translated)?new Dictionary<string,string>(translated):new();
            item["code"]=code; item["nameEn"]=guide.VitalSignType.Name; item["unit"]=guide.VitalSignType.Unit;
            item["whatIsItEn"]=guide.WhatIsIt;item["whyItMattersEn"]=guide.WhyItMatters;
            item["howToMeasureEn"]=guide.HowToMeasure;item["interpretationEn"]=guide.InterpretationGuidance;
            item["actionEn"]=guide.ActionGuidance;
            var range=ranges.Where(r=>r.VitalSignTypeId==guide.VitalSignTypeId).OrderByDescending(r=>r.IsMissionApproved).ThenBy(r=>r.Id).FirstOrDefault();
            item["normalRange"]=range is null?"—":FormatNormal(range);
            item["emergencyRange"]=range is null?"—":FormatEmergency(range);
            item["source"]=range?.Source??"";
            item["isMissionApproved"]=(range?.IsMissionApproved??false).ToString().ToLowerInvariant();
            result.Add(item);
        }
        return result;
    }

    private static string Number(double number)=>number.ToString("0.##",CultureInfo.InvariantCulture);
    public static string FormatNormal(ReferenceRange range) =>
        $"{Number(range.MinNormal)} – {Number(range.MaxNormal)}" +
        (range.MinSecondaryNormal.HasValue && range.MaxSecondaryNormal.HasValue ? $" / {Number(range.MinSecondaryNormal.Value)} – {Number(range.MaxSecondaryNormal.Value)}" : "");
    public static string FormatEmergency(ReferenceRange range)
    {
        var parts=new List<string>();
        if(range.EmergencyBelow.HasValue)parts.Add("< "+Number(range.EmergencyBelow.Value));
        if(range.EmergencyAbove.HasValue)parts.Add("> "+Number(range.EmergencyAbove.Value));
        if(range.SecondaryEmergencyBelow.HasValue)parts.Add("diastolic < "+Number(range.SecondaryEmergencyBelow.Value));
        if(range.SecondaryEmergencyAbove.HasValue)parts.Add("diastolic > "+Number(range.SecondaryEmergencyAbove.Value));
        return parts.Count==0?"—":string.Join(" | ",parts);
    }
    private static Dictionary<string, Dictionary<string,string>> LoadTranslations()
    {
        using var stream=typeof(ClinicalReferenceController).Assembly.GetManifestResourceStream("AstronautHealthMonitoring.Data.ClinicalGuides.json")!;
        return JsonSerializer.Deserialize<List<Dictionary<string,string>>>(stream)!.ToDictionary(g=>g["code"]);
    }
}
