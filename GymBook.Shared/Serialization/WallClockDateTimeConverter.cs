using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GymBook.Serialization;

/// <summary>
/// Workout times are local wall-clock times ("started 7:05 am"), not instants. This writes them without an
/// offset and reads them back unchanged, so they don't shift when they pass through a server in another
/// time zone. Anything that must be compared across devices (UpdatedAt) uses DateTimeOffset instead.
/// </summary>
public sealed class WallClockDateTimeConverter : JsonConverter<DateTime>
{
    // Microseconds: the finest precision PostgreSQL keeps, so every copy of a record serializes identically.
    const string Format = "yyyy-MM-dd'T'HH:mm:ss.FFFFFF";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Older exports wrote an offset; keep the local time as written and drop the offset.
        if (reader.TokenType == JsonTokenType.String && reader.TryGetDateTimeOffset(out var withOffset))
            return DateTime.SpecifyKind(withOffset.DateTime, DateTimeKind.Unspecified);
        throw new JsonException("Invalid date/time.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
}
