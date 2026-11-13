using Microsoft.AspNetCore.Diagnostics;

namespace AstronautHealthMonitoring.Services.Implementations;

public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context,Exception exception,CancellationToken ct)
    {
        var failure=ApiFailure.Classify(exception);
        logger.LogError(exception,"API failure {Code}; request {TraceId}; {Method} {Path}",failure.Code,context.TraceIdentifier,context.Request.Method,context.Request.Path);
        await WriteAsync(context,failure,ct);
        return true;
    }
    public static async Task WriteAsync(HttpContext context,ApiFailure failure,CancellationToken ct)
    {
        context.Response.StatusCode=failure.Status;
        context.Response.Headers["X-Request-ID"]=context.TraceIdentifier;
        await context.Response.WriteAsJsonAsync(new {type="about:blank",title=failure.Code,status=failure.Status,detail=failure.Message,code=failure.Code,message=failure.Message,messageAr=failure.MessageAr,traceId=context.TraceIdentifier},options:(System.Text.Json.JsonSerializerOptions?)null,contentType:"application/problem+json",cancellationToken:ct);
    }
}
