using System.Text.Json;
using System.Text.Json.Serialization;

namespace Congreso.Api.Utils;

/// <summary>
/// Utilities for normalizing data across the application
/// </summary>
public static class NormalizationUtils
{
    /// <summary>
    /// Normalizes email addresses to lowercase and trims whitespace
    /// </summary>
    /// <param name="email">The email to normalize</param>
    /// <returns>Normalized email or null if input is null/empty</returns>
    public static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;
            
        return email.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Converts GUID to string representation for consistent serialization
    /// </summary>
    /// <param name="guid">The GUID to convert</param>
    /// <returns>String representation of the GUID</returns>
    public static string GuidToString(Guid guid)
    {
        return guid.ToString();
    }

    /// <summary>
    /// Converts nullable GUID to string representation
    /// </summary>
    /// <param name="guid">The nullable GUID to convert</param>
    /// <returns>String representation of the GUID or null</returns>
    public static string? GuidToString(Guid? guid)
    {
        return guid?.ToString();
    }

    /// <summary>
    /// Safely parses a string to GUID
    /// </summary>
    /// <param name="guidString">The string to parse</param>
    /// <returns>Parsed GUID or null if invalid</returns>
    public static Guid? ParseGuid(string? guidString)
    {
        if (string.IsNullOrWhiteSpace(guidString))
            return null;
            
        return Guid.TryParse(guidString, out var result) ? result : null;
    }

    /// <summary>
    /// Formats DateTime to ISO 8601 string with timezone information
    /// Reference timezone: America/Guatemala (UTC-6)
    /// </summary>
    /// <param name="dateTime">The DateTime to format</param>
    /// <returns>ISO 8601 formatted string</returns>
    public static string FormatDateTimeToIso(DateTime dateTime)
    {
        // Ensure DateTime is treated as UTC
        var utcDateTime = dateTime.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) 
            : dateTime.ToUniversalTime();
            
        return utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }

    /// <summary>
    /// Formats nullable DateTime to ISO 8601 string
    /// </summary>
    /// <param name="dateTime">The nullable DateTime to format</param>
    /// <returns>ISO 8601 formatted string or null</returns>
    public static string? FormatDateTimeToIso(DateTime? dateTime)
    {
        return dateTime.HasValue ? FormatDateTimeToIso(dateTime.Value) : null;
    }

    /// <summary>
    /// Formats DateTimeOffset to ISO 8601 string
    /// </summary>
    /// <param name="dateTimeOffset">The DateTimeOffset to format</param>
    /// <returns>ISO 8601 formatted string</returns>
    public static string FormatDateTimeToIso(DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
    }

    /// <summary>
    /// Formats nullable DateTimeOffset to ISO 8601 string
    /// </summary>
    /// <param name="dateTimeOffset">The nullable DateTimeOffset to format</param>
    /// <returns>ISO 8601 formatted string or null</returns>
    public static string? FormatDateTimeToIso(DateTimeOffset? dateTimeOffset)
    {
        return dateTimeOffset?.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
    }
}

/// <summary>
/// JSON converter for consistent GUID serialization
/// </summary>
public class GuidJsonConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var guidString = reader.GetString();
        return Guid.TryParse(guidString, out var result) ? result : Guid.Empty;
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(NormalizationUtils.GuidToString(value));
    }
}

/// <summary>
/// JSON converter for consistent nullable GUID serialization
/// </summary>
public class NullableGuidJsonConverter : JsonConverter<Guid?>
{
    public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var guidString = reader.GetString();
        return NormalizationUtils.ParseGuid(guidString);
    }

    public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(NormalizationUtils.GuidToString(value.Value));
        else
            writer.WriteNullValue();
    }
}

/// <summary>
/// JSON converter for consistent DateTime serialization to ISO 8601
/// </summary>
public class DateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();
        return DateTime.TryParse(dateString, out var result) ? result : DateTime.MinValue;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(NormalizationUtils.FormatDateTimeToIso(value));
    }
}

/// <summary>
/// JSON converter for consistent nullable DateTime serialization
/// </summary>
public class NullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();
        return DateTime.TryParse(dateString, out var result) ? result : null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(NormalizationUtils.FormatDateTimeToIso(value.Value));
        else
            writer.WriteNullValue();
    }
}