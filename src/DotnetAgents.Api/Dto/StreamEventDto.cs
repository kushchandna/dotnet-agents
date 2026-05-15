using System.Text.Json.Serialization;
namespace DotnetAgents.Api.Dto;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(DeltaEvent),      "delta")]
[JsonDerivedType(typeof(ToolCallEvent),   "tool_call")]
[JsonDerivedType(typeof(ToolResultEvent), "tool_result")]
[JsonDerivedType(typeof(DoneEvent),       "done")]
[JsonDerivedType(typeof(ErrorEvent),      "error")]
public abstract record StreamEventDto;
public sealed record DeltaEvent     (string Content) : StreamEventDto;
public sealed record ToolCallEvent  (string CallId, string Name, string Arguments) : StreamEventDto;
public sealed record ToolResultEvent(string CallId, string Name, string Result) : StreamEventDto;
public sealed record DoneEvent      (string MessageId) : StreamEventDto;
public sealed record ErrorEvent     (string Message) : StreamEventDto;
