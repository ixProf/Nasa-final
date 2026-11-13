using System.Net.Sockets;
using System.Text.Json;
using AstronautHealthMonitoring.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AstronautHealthMonitoring.Tests;

public class ApiFailureTests
{
    [Fact]
    public void MissingTableOrColumn_ExplainsMigrationInsteadOfRetry()
    {
        foreach(var state in new[]{"42P01","42703"})
        {
            var error=new DbUpdateException("internal details",new PostgresException("secret table name","ERROR","ERROR",state));
            var failure=ApiFailure.Classify(error);
            Assert.Equal("DATABASE_UPDATE_REQUIRED",failure.Code);
            Assert.Equal(503,failure.Status);
            Assert.Contains("dotnet ef database update",failure.Message);
            Assert.DoesNotContain("secret",failure.Message);
        }
    }
    [Fact]
    public void ConnectionAndAuthenticationFailures_HaveDifferentActions()
    {
        Assert.Equal("DATABASE_AUTH_FAILED",ApiFailure.Classify(new PostgresException("password details","ERROR","ERROR","28P01")).Code);
        Assert.Equal("DATABASE_UNAVAILABLE",ApiFailure.Classify(new NpgsqlException("connection failed",new SocketException())).Code);
        Assert.Equal("DATABASE_BUSY",ApiFailure.Classify(new PostgresException("connections","ERROR","ERROR","53300")).Code);
        Assert.Equal("REQUEST_FAILED",ApiFailure.Classify(new PostgresException("constraint","ERROR","ERROR","23505")).Code);
    }
    [Fact]
    public async Task ErrorResponse_IncludesSafeBilingualExplanationAndLogCorrelation()
    {
        using var services=new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
        var context=new DefaultHttpContext {RequestServices=services,TraceIdentifier="test-request"};
        context.Response.Body=new MemoryStream();
        await ApiExceptionHandler.WriteAsync(context,ApiFailure.Schema,CancellationToken.None);
        context.Response.Body.Position=0;
        var payload=await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(503,context.Response.StatusCode);
        Assert.Equal("test-request",context.Response.Headers["X-Request-ID"]);
        Assert.Equal("test-request",payload.RootElement.GetProperty("traceId").GetString());
        Assert.False(string.IsNullOrEmpty(payload.RootElement.GetProperty("messageAr").GetString()));
    }
}
