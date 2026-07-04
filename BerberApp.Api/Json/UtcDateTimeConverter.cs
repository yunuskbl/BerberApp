using System.Text.Json;
using System.Text.Json.Serialization;

namespace BerberApp.Api.Json;

/// <summary>
/// Tüm DateTime değerlerini JSON'a UTC olarak 'Z' ekiyle yazar.
/// Npgsql legacy timestamp davranışıyla veritabanından okunan tarihler
/// DateTimeKind.Unspecified gelir; bunlar zaten UTC olarak saklandığı için
/// UTC kabul edilir. Böylece frontend tarihi doğru şekilde yerel saate çevirir.
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDateTime();

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc         => value,
            DateTimeKind.Local       => value.ToUniversalTime(),
            _                        => DateTime.SpecifyKind(value, DateTimeKind.Utc), // Unspecified → UTC kabul
        };
        writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}

/// <summary>Nullable DateTime için UTC 'Z' converter.</summary>
public class UtcNullableDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Null ? null : reader.GetDateTime();

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null) { writer.WriteNullValue(); return; }

        var v = value.Value;
        var utc = v.Kind switch
        {
            DateTimeKind.Utc         => v,
            DateTimeKind.Local       => v.ToUniversalTime(),
            _                        => DateTime.SpecifyKind(v, DateTimeKind.Utc),
        };
        writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}
