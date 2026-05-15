using System.Text;
using System.Text.Json;
using DotnetAgents.Api.Dto;
using DotnetAgents.Core.Runtime;
using DotnetAgents.Core.Sessions;

namespace DotnetAgents.Api.Endpoints;

public static class MessagesEndpoints
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static void Map(WebApplication app)
    {
        app.MapPost("/api/users/{userId}/sessions/{sessionId}/messages", async (
            string userId, string sessionId, SendMessageRequest request,
            ISessionStore store, IAgentRuntime runtime, HttpContext ctx,
            CancellationToken ct) =>
        {
            var session = await store.GetAsync(userId, sessionId, ct);
            if (session is null)
            {
                ctx.Response.StatusCode = 404;
                return;
            }

            ctx.Response.Headers["Content-Type"]  = "text/event-stream";
            ctx.Response.Headers["Cache-Control"] = "no-cache";
            ctx.Response.Headers["Connection"]    = "keep-alive";
            ctx.Response.Headers["X-Accel-Buffering"] = "no";
            await ctx.Response.Body.FlushAsync(ct);

            await foreach (var update in runtime.RunStreamingAsync(new AgentRunRequest(userId, sessionId, request.Content), ct))
            {
                StreamEventDto evt = update switch
                {
                    DeltaUpdate d        => new DeltaEvent(d.Content),
                    ToolCallUpdate tc    => new ToolCallEvent(tc.CallId, tc.Name, tc.Arguments),
                    ToolResultUpdate tr  => new ToolResultEvent(tr.CallId, tr.Name, tr.Result),
                    DoneUpdate done      => new DoneEvent(done.MessageId),
                    ErrorUpdate err      => new ErrorEvent(err.Message),
                    _                    => new ErrorEvent("Unknown update type")
                };
                var bytes = Encoding.UTF8.GetBytes($"data: {JsonSerializer.Serialize(evt, JsonOpts)}\n\n");
                await ctx.Response.Body.WriteAsync(bytes, ct);
                await ctx.Response.Body.FlushAsync(ct);
            }
        });
    }
}
