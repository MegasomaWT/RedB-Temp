using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using redb.Core.Models.Entities;
using redb.Core.Models.Contracts;

namespace redb.Core.Serialization
{
    // Реализация на System.Text.Json
    // Требование: имена свойств C# совпадают с именами JSON (snake_case), поэтому без кастомной NamingPolicy
    public class SystemTextJsonRedbSerializer : IRedbObjectSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { 
                new JsonStringEnumConverter(), // Поддержка Enum как строк
                new FlexibleTimeSpanConverter(), // Поддержка TimeSpan из строк
                new FlexibleNullableTimeSpanConverter(), // Поддержка nullable TimeSpan
#if NET6_0_OR_GREATER
                new FlexibleDateOnlyConverter(), // Поддержка DateOnly из DateTime строк
                new FlexibleNullableDateOnlyConverter(), // Поддержка nullable DateOnly
                new FlexibleTimeOnlyConverter(), // Поддержка TimeOnly из TimeSpan строк
                new FlexibleNullableTimeOnlyConverter() // Поддержка nullable TimeOnly
#endif
            }
        };

        public RedbObject<TProps> Deserialize<TProps>(string json) where TProps : class, new()
        {
            var obj = JsonSerializer.Deserialize<RedbObject<TProps>>(json, Options);
            if (obj == null)
            {
                throw new InvalidOperationException("Failed to deserialize get_object_json payload to RedbObject<TProps>.");
            }
            return obj;
        }

        public IRedbObject DeserializeDynamic(string json, Type propsType)
        {
            // Создаем generic тип RedbObject<propsType> через рефлексию
            var redbObjectType = typeof(RedbObject<>).MakeGenericType(propsType);
            
            // Десериализуем JSON в этот тип
            var deserializedObj = JsonSerializer.Deserialize(json, redbObjectType, Options);
            
            if (deserializedObj == null)
            {
                throw new InvalidOperationException($"Failed to deserialize get_object_json payload to RedbObject<{propsType.Name}>.");
            }
            
            // Возвращаем как IRedbObject
            return (IRedbObject)deserializedObj;
        }
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Гибкий конвертер для DateOnly - поддерживает DateTime строки
    /// </summary>
    public class FlexibleDateOnlyConverter : JsonConverter<DateOnly>
    {
        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var dateString = reader.GetString();
                if (DateTime.TryParse(dateString, out var dateTime))
                {
                    return DateOnly.FromDateTime(dateTime);
                }
            }
            throw new JsonException($"Unable to convert '{reader.GetString()}' to DateOnly.");
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
        }
    }

    /// <summary>
    /// Гибкий конвертер для nullable DateOnly
    /// </summary>
    public class FlexibleNullableDateOnlyConverter : JsonConverter<DateOnly?>
    {
        public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;
                
            if (reader.TokenType == JsonTokenType.String)
            {
                var dateString = reader.GetString();
                if (string.IsNullOrEmpty(dateString))
                    return null;
                    
                if (DateTime.TryParse(dateString, out var dateTime))
                {
                    return DateOnly.FromDateTime(dateTime);
                }
            }
            throw new JsonException($"Unable to convert '{reader.GetString()}' to DateOnly?.");
        }

        public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
            else
                writer.WriteNullValue();
        }
    }

    /// <summary>
    /// Гибкий конвертер для TimeOnly - поддерживает TimeSpan строки
    /// </summary>
    public class FlexibleTimeOnlyConverter : JsonConverter<TimeOnly>
    {
        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var timeString = reader.GetString();
                if (TimeSpan.TryParse(timeString, out var timeSpan))
                {
                    return TimeOnly.FromTimeSpan(timeSpan);
                }
            }
            throw new JsonException($"Unable to convert '{reader.GetString()}' to TimeOnly.");
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("HH:mm:ss"));
        }
    }

    /// <summary>
    /// Гибкий конвертер для nullable TimeOnly
    /// </summary>
    public class FlexibleNullableTimeOnlyConverter : JsonConverter<TimeOnly?>
    {
        public override TimeOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;
                
            if (reader.TokenType == JsonTokenType.String)
            {
                var timeString = reader.GetString();
                if (string.IsNullOrEmpty(timeString))
                    return null;
                    
                if (TimeSpan.TryParse(timeString, out var timeSpan))
                {
                    return TimeOnly.FromTimeSpan(timeSpan);
                }
            }
            throw new JsonException($"Unable to convert '{reader.GetString()}' to TimeOnly?.");
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString("HH:mm:ss"));
            else
                writer.WriteNullValue();
        }
    }
#endif

    /// <summary>
    /// Гибкий конвертер для TimeSpan - поддерживает строки
    /// </summary>
    public class FlexibleTimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var timeString = reader.GetString();
                if (TimeSpan.TryParse(timeString, out var timeSpan))
                {
                    return timeSpan;
                }
            }
            throw new JsonException($"Unable to convert '{reader.GetString()}' to TimeSpan.");
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(@"hh\:mm\:ss"));
        }
    }

    /// <summary>
    /// Гибкий конвертер для nullable TimeSpan
    /// </summary>
    public class FlexibleNullableTimeSpanConverter : JsonConverter<TimeSpan?>
    {
        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;
                
            if (reader.TokenType == JsonTokenType.String)
            {
                var timeString = reader.GetString();
                if (string.IsNullOrEmpty(timeString))
                    return null;
                    
                if (TimeSpan.TryParse(timeString, out var timeSpan))
                {
                    return timeSpan;
                }
            }
            throw new JsonException($"Unable to convert '{reader.GetString()}' to TimeSpan?.");
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString(@"hh\:mm\:ss"));
            else
                writer.WriteNullValue();
        }
    }
}
