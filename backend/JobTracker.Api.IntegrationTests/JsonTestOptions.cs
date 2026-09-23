using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobTracker.Api.IntegrationTests;

// The API is configured (in Program.cs) to serialize enums as their
// string names, not numbers. System.Net.Http.Json's default options
// don't know that on their own, so every test call that reads a
// response containing an enum (Status) needs this explicit options
// object, or deserialization throws.
public static class JsonTestOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
