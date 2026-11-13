using System.Reflection;
using AstronautHealthMonitoring.Controllers;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class ControllerAuthorizationReflectionTests
{
    [Theory]
    [InlineData(typeof(VitalSignsController), nameof(VitalSignsController.RecordReading), AuthPolicies.AstronautOnly)]
    [InlineData(typeof(VitalSignsController), nameof(VitalSignsController.GetById), AuthPolicies.Authenticated)]
    [InlineData(typeof(VitalSignsController), nameof(VitalSignsController.GetLatest), AuthPolicies.Authenticated)]
    [InlineData(typeof(VitalSignsController), nameof(VitalSignsController.GetHistory), AuthPolicies.Authenticated)]
    [InlineData(typeof(VitalSignsController), nameof(VitalSignsController.GetTypes), AuthPolicies.Authenticated)]
    [InlineData(typeof(HealthAlertsController), nameof(HealthAlertsController.GetAlerts), AuthPolicies.Authenticated)]
    [InlineData(typeof(HealthAlertsController), nameof(HealthAlertsController.GetById), AuthPolicies.Authenticated)]
    [InlineData(typeof(HealthAlertsController), nameof(HealthAlertsController.TriggerManualAlert), AuthPolicies.Authenticated)]
    [InlineData(typeof(HealthAlertsController), nameof(HealthAlertsController.AcknowledgeAlert), AuthPolicies.MonitorOnly)]
    [InlineData(typeof(HealthAlertsController), nameof(HealthAlertsController.ResolveAlert), AuthPolicies.MonitorOnly)]
    [InlineData(typeof(SleepController), nameof(SleepController.RecordSleep), AuthPolicies.AstronautOnly)]
    [InlineData(typeof(SleepController), nameof(SleepController.GetById), AuthPolicies.Authenticated)]
    [InlineData(typeof(SleepController), nameof(SleepController.GetLatest), AuthPolicies.Authenticated)]
    [InlineData(typeof(SleepController), nameof(SleepController.GetHistory), AuthPolicies.Authenticated)]
    [InlineData(typeof(MoodController), nameof(MoodController.RecordMood), AuthPolicies.AstronautOnly)]
    [InlineData(typeof(MoodController), nameof(MoodController.GetById), AuthPolicies.Authenticated)]
    [InlineData(typeof(MoodController), nameof(MoodController.GetLatest), AuthPolicies.Authenticated)]
    [InlineData(typeof(MoodController), nameof(MoodController.GetHistory), AuthPolicies.Authenticated)]
    [InlineData(typeof(AstronautsController), nameof(AstronautsController.CreateAstronaut), AuthPolicies.MonitorOnly)]
    [InlineData(typeof(AstronautsController), nameof(AstronautsController.GetAstronauts), AuthPolicies.MonitorOnly)]
    [InlineData(typeof(AstronautsController), nameof(AstronautsController.GetMe), AuthPolicies.AstronautOnly)]
    [InlineData(typeof(AstronautsController), nameof(AstronautsController.GetById), AuthPolicies.Authenticated)]
    [InlineData(typeof(AstronautsController), nameof(AstronautsController.UpdateAstronaut), AuthPolicies.MonitorOnly)]
    [InlineData(typeof(AstronautsController), nameof(AstronautsController.DeactivateAstronaut), AuthPolicies.MonitorOnly)]
    [InlineData(typeof(MissionsController), nameof(MissionsController.GetMissions), AuthPolicies.Authenticated)]
    [InlineData(typeof(MissionsController), nameof(MissionsController.GetById), AuthPolicies.Authenticated)]
    [InlineData(typeof(MissionsController), nameof(MissionsController.CreateMission), AuthPolicies.MonitorOnly)]
    [InlineData(typeof(MissionsController), nameof(MissionsController.UpdateMission), AuthPolicies.MonitorOnly)]
    [InlineData(typeof(MissionsController), nameof(MissionsController.DeleteMission), AuthPolicies.MonitorOnly)]
    [InlineData(typeof(MissionsController), nameof(MissionsController.AssignAstronaut), AuthPolicies.MonitorOnly)]
    [InlineData(typeof(MissionsController), nameof(MissionsController.UnassignAstronaut), AuthPolicies.MonitorOnly)]
    [InlineData(typeof(DashboardController), nameof(DashboardController.GetMonitorDashboard), AuthPolicies.MonitorOnly)]
    [InlineData(typeof(DashboardController), nameof(DashboardController.GetAstronautSnapshot), AuthPolicies.MonitorOnly)]
    [InlineData(typeof(DashboardController), nameof(DashboardController.GetMyDashboard), AuthPolicies.AstronautOnly)]
    [InlineData(typeof(RecommendationsController), nameof(RecommendationsController.GetRecommendations), AuthPolicies.Authenticated)]
    [InlineData(typeof(RecommendationsController), nameof(RecommendationsController.GetById), AuthPolicies.Authenticated)]
    [InlineData(typeof(RecommendationsController), nameof(RecommendationsController.CreateRecommendation), AuthPolicies.MonitorOnly)]
    [InlineData(typeof(RecommendationsController), nameof(RecommendationsController.UpdateStatus), AuthPolicies.Authenticated)]
    [InlineData(typeof(SymptomGuidanceController), nameof(SymptomGuidanceController.GetAllSymptoms), AuthPolicies.Authenticated)]
    [InlineData(typeof(SymptomGuidanceController), nameof(SymptomGuidanceController.GetGuidanceCatalog), AuthPolicies.Authenticated)]
    [InlineData(typeof(SymptomGuidanceController), nameof(SymptomGuidanceController.AssessSymptoms), AuthPolicies.Authenticated)]
    [InlineData(typeof(ChatController), nameof(ChatController.GetMessages), AuthPolicies.Authenticated)]
    [InlineData(typeof(ChatController), nameof(ChatController.SendMessage), AuthPolicies.Authenticated)]
    [InlineData(typeof(ChatController), nameof(ChatController.MarkMessageAsRead), AuthPolicies.Authenticated)]
    [InlineData(typeof(ChatController), nameof(ChatController.GetUnreadCount), AuthPolicies.Authenticated)]
    [InlineData(typeof(SyncController), nameof(SyncController.SyncBatch), AuthPolicies.AstronautOnly)]
    public void Endpoint_Has_Expected_AuthorizePolicy(Type controllerType, string methodName, string expectedPolicy)
    {
        var method = controllerType.GetMethod(methodName);
        Assert.NotNull(method);

        var authorizeAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
        Assert.Equal(expectedPolicy, authorizeAttr.Policy);
    }

    [Fact]
    public void AlertsHub_Class_Has_AuthorizeAttribute()
    {
        var authorizeAttr = typeof(AlertsHub).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
    }

    [Fact]
    public void ChatHub_Class_Has_AuthorizeAttribute()
    {
        var authorizeAttr = typeof(ChatHub).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
    }

    [Fact]
    public void All_Controller_Actions_Have_Explicit_Authorization_Or_AllowAnonymous()
    {
        var controllerTypes = typeof(AuthController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        Assert.NotEmpty(controllerTypes);

        var allowedAnonymousEndpoints = new HashSet<string>
        {
            $"{nameof(AuthController)}.{nameof(AuthController.Login)}",
            $"{nameof(AuthController)}.{nameof(AuthController.Refresh)}"
        };

        var validPolicies = new HashSet<string>
        {
            AuthPolicies.AstronautOnly,
            AuthPolicies.MonitorOnly,
            AuthPolicies.Authenticated
        };

        var violations = new List<string>();

        foreach (var controller in controllerTypes)
        {
            var actions = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => typeof(IActionResult).IsAssignableFrom(m.ReturnType) ||
                            (m.ReturnType.IsGenericType && typeof(Task).IsAssignableFrom(m.ReturnType) &&
                             typeof(IActionResult).IsAssignableFrom(m.ReturnType.GetGenericArguments()[0])))
                .ToList();

            foreach (var action in actions)
            {
                var identifier = $"{controller.Name}.{action.Name}";

                var isAnon = action.GetCustomAttribute<AllowAnonymousAttribute>() != null;
                if (isAnon)
                {
                    if (!allowedAnonymousEndpoints.Contains(identifier))
                    {
                        violations.Add($"{identifier} has [AllowAnonymous] but is not in whitelisted anonymous endpoints.");
                    }
                    continue;
                }

                var authorizeAttr = action.GetCustomAttribute<AuthorizeAttribute>() ??
                                    controller.GetCustomAttribute<AuthorizeAttribute>();

                if (authorizeAttr == null)
                {
                    violations.Add($"{identifier} is missing [Authorize] attribute.");
                }
                else if (string.IsNullOrWhiteSpace(authorizeAttr.Policy) || !validPolicies.Contains(authorizeAttr.Policy))
                {
                    violations.Add($"{identifier} has [Authorize] with missing or invalid policy: '{authorizeAttr.Policy}'.");
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void HealthAlertsController_TriggerManualAlert_Has_EnableRateLimitingAttribute()
    {
        var method = typeof(HealthAlertsController).GetMethod(nameof(HealthAlertsController.TriggerManualAlert));
        Assert.NotNull(method);

        var rateLimitingAttr = method.GetCustomAttribute<Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute>();
        Assert.NotNull(rateLimitingAttr);
        Assert.Equal("ManualAlertLimiter", rateLimitingAttr.PolicyName);
    }

    [Fact]
    public void HealthAlertsController_Has_Only_Alerts_Route()
    {
        var routes = typeof(HealthAlertsController).GetCustomAttributes<RouteAttribute>().Select(r => r.Template).ToList();
        Assert.Single(routes);
        Assert.Equal("api/alerts", routes[0]);
    }

    [Theory]
    [InlineData(typeof(VitalSignsController))]
    [InlineData(typeof(HealthAlertsController))]
    [InlineData(typeof(SleepController))]
    [InlineData(typeof(MoodController))]
    [InlineData(typeof(AstronautsController))]
    [InlineData(typeof(MissionsController))]
    [InlineData(typeof(DashboardController))]
    [InlineData(typeof(RecommendationsController))]
    [InlineData(typeof(SymptomGuidanceController))]
    [InlineData(typeof(ChatController))]
    [InlineData(typeof(SyncController))]
    public void Controllers_Have_ApiController_And_Route_Attributes(Type controllerType)
    {
        var apiControllerAttr = controllerType.GetCustomAttribute<ApiControllerAttribute>();
        Assert.NotNull(apiControllerAttr);

        var routeAttrs = controllerType.GetCustomAttributes<RouteAttribute>().ToList();
        Assert.NotEmpty(routeAttrs);
        Assert.Contains(routeAttrs, r => r.Template == "api/[controller]" || r.Template == "api/symptoms" || r.Template == "api/alerts");
    }

    [Fact]
    public void ForwardedHeadersOptions_AreConfiguredForReverseProxyBehindIIS()
    {
        var services = new ServiceCollection();
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedFor));
        Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedProto));
        Assert.Empty(options.KnownNetworks);
        Assert.Empty(options.KnownProxies);
    }

    [Fact]
    public async Task OpenApiDocumentTransformer_SetsServerListToRelativeUrl()
    {
        var document = new OpenApiDocument();
        document.Servers = new List<OpenApiServer>
        {
            new() { Url = "/" }
        };

        Assert.Single(document.Servers);
        Assert.Equal("/", document.Servers[0].Url);
    }
}

