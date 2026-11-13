using AstronautHealthMonitoring.Models.Common;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Educational and practical guidance for astronauts written in clear, accessible language.
/// Direct implementation of the 5 Core Design Principles for health metrics.
/// </summary>
public class HealthMetricGuide : BaseEntity
{
    public int VitalSignTypeId { get; set; }
    public VitalSignType VitalSignType { get; set; } = null!;

    /// <summary>
    /// 1. What is this? Simple plain-language definition of the metric.
    /// </summary>
    public string WhatIsIt { get; set; } = string.Empty;

    /// <summary>
    /// 2. Why is it important? Explanation of why it matters specifically in space/microgravity.
    /// </summary>
    public string WhyItMatters { get; set; } = string.Empty;

    /// <summary>
    /// 3. How do I measure it? Step-by-step instructions on sensor placement or test execution.
    /// </summary>
    public string HowToMeasure { get; set; } = string.Empty;

    /// <summary>
    /// 4. What does the current result mean? Explanation of normal, elevated, and depressed readings.
    /// </summary>
    public string InterpretationGuidance { get; set; } = string.Empty;

    /// <summary>
    /// 5. What should I do now? Recommended immediate actions for the astronaut.
    /// </summary>
    public string ActionGuidance { get; set; } = string.Empty;
}

