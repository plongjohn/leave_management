using System.Text.Json;
using System.Text.Json.Serialization;

namespace LeaveManagement.Api.Tests.Infrastructure;

/// <summary>The JSON settings of the API (web defaults, enums as names) for reading responses.</summary>
internal static class ApiJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
