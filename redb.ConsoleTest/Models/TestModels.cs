using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using redb.Core.DBModels;
using redb.Core.Models.Entities;
using redb.Core.Models.Attributes; // 🔧 Добавляем атрибуты

namespace redb.ConsoleTest
{
    // Простой кастомный логгер без префиксов
    public class SimpleConsoleLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) => null!;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            var levelText = logLevel switch
            {
                LogLevel.Debug => "debug:",
                LogLevel.Warning => "warn:",
                LogLevel.Error => "error:",
                LogLevel.Critical => "critical:",
                _ => ""
            };

            if (!string.IsNullOrEmpty(levelText))
                Console.WriteLine($"{levelText} {message}");
            else
                Console.WriteLine(message);
        }
    }

    public class SimpleConsoleLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new SimpleConsoleLogger();
        public void Dispose() { }
    }

    // Классы для проверки (properties секция)
    [RedbScheme("Метрики аналитики")]
    public class AnalyticsMetricsProps
    {
        public long AdvertId { get; set; }
        public long? Baskets { get; set; }
        public long? Base { get; set; }
        public long? Association { get; set; }
        public double? Costs { get; set; }
        public long? Rate { get; set; }
    }

    [RedbScheme("Записи аналитики")]
    public class AnalyticsRecordProps
    {
        public DateTime Date { get; set; }
        public string Article { get; set; } = string.Empty;
        public long? Orders { get; set; }
        public long Stock { get; set; }
        public long? TotalCart { get; set; }
        public string? Tag { get; set; }
        public string? TestName { get; set; }
        public string[] stringArr { get; set; }
        public long[] longArr { get; set; }
        public RedbObject<AnalyticsMetricsProps>? AutoMetrics { get; set; }
        public RedbObject<AnalyticsMetricsProps>? AuctionMetrics { get; set; }
        public RedbObject<AnalyticsMetricsProps>[]? AutoMetricsArray { get; set; }
    }

    // ✅ БИЗНЕС КЛАССЫ для тестирования новой парадигмы (Class типы, не RedbObject<>)
    public class Address
    {
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;  
        public Details Details { get; set; } = new Details();
    }

    public class Details
    {
        public int Floor { get; set; }
        public string Building { get; set; } = string.Empty;
        public string[] Tags1 { get; set; } = new string[0];
        public int[] Scores1 { get; set; } = new int[0];

        public string[] Tags2 { get; set; } = new string[0];
        public int[] Scores2 { get; set; } = new int[0];

    }

    public class Contact
    {
        public string Type { get; set; } = string.Empty; // email, phone, etc
        public string Value { get; set; } = string.Empty;
        public bool Verified { get; set; }
    }

    // ✅ СМЕШАННЫЙ ТЕСТОВЫЙ КЛАСС для новой парадигмы  
    public class MixedTestProps
    {
        // Простые типы
        public int Age { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Article { get; set; } = string.Empty;
        public long Stock { get; set; }
        public string Tag { get; set; } = string.Empty;
        public string? TestName { get; set; }  // ✅ ДОБАВЛЯЕМ НЕДОСТАЮЩЕЕ ПОЛЕ!

        // ✅ Простые массивы (новая парадигма: реляционное хранение)
        public string[] Tags1 { get; set; } = new string[0];
        public int[] Scores1 { get; set; } = new int[0];
        public string[] Tags2 { get; set; } = new string[0];
        public int[] Scores2 { get; set; } = new int[0];

        // ✅ Бизнес класс (новая парадигма: UUID хеш + вложенные свойства)
        public Address Address1 { get; set; } = new Address();
        public Address Address2 { get; set; } = new Address();
        public Address? Address3 { get; set; } = null;  // ✅ NULLABLE БИЗНЕС-КЛАСС для тестирования!
        // ✅ Массив бизнес классов (новая парадигма: базовая запись + элементы с ArrayParentId)
        public Contact[] Contacts { get; set; } = new Contact[0];
        
        // ✅ RedbObject ссылки (работает как раньше - ID в Long поле)
        public RedbObject<AnalyticsMetricsProps>? AutoMetrics { get; set; }
        public RedbObject<AnalyticsMetricsProps>[]? RelatedMetrics { get; set; }
    }

    // Класс для тестирования категорий в древовидных LINQ-запросах
    public class CategoryTestProps
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    // Класс для тестирования LINQ-запросов
    public class ProductTestProps
    {
        private int _stock;
        private string _category = string.Empty;
        private double _price;
        private bool _isActive;

        public int Stock 
        { 
            get
            {
                return _stock;
            }
            set
            {
                _stock = value;
            }
        }

        public virtual string Category  // ← ТЕСТИРУЕМ VIRTUAL!
        { 
            get
            {
                return _category;
            }
            set
            {
                _category = value;
            }
        }

        public double Price 
        { 
            get
            {
                return _price;
            }
            set
            {
                _price = value;
            }
        }

        public bool IsActive 
        { 
            get
            {
                return _isActive;
            }
            set
            {
                _isActive = value;
            }
        }

        // Добавлено для тестирования фильтрации DateTime
        public DateTime TestDate { get; set; }
        public int TestValue { get; set; } = 2;
    }

    // Класс для тестирования JSON атрибутов с прокси
    public class JsonTestClass
    {
        [JsonPropertyName("display_name")]
        public virtual string DisplayName { get; set; } = "";
        
        [JsonPropertyName("price_value")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual double? Price { get; set; }
        
        [JsonPropertyName("is_enabled")]
        public virtual bool IsEnabled { get; set; }
    }

    // Классы для тестирования валидации
    public class ValidationTestProps
    {
        public string RequiredString { get; set; } = string.Empty;
        public string? OptionalString { get; set; }
        public List<string> StringArray { get; set; } = new();
        public List<RedbObject<ValidationTestProps>> ObjectArray { get; set; } = new();
        public List<string> RequiredList { get; set; } = new();
    }

    public class ProblematicProps
    {
        public List<string> RequiredList { get; set; } = new();
        public List<RedbObject<ProblematicProps>> ObjectArray { get; set; } = new();
        public List<List<string>> NestedList { get; set; } = new();
        public Dictionary<string, object> CustomData { get; set; } = new();
    }

    // Расширенные типы для тестирования
    public class ExtendedTypesTestProps
    {
        // Базовые типы
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public long BigNumber { get; set; }
        public short SmallNumber { get; set; }
        public byte TinyNumber { get; set; }
        public double Price { get; set; }
        public float SmallPrice { get; set; }
        public decimal PrecisePrice { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid Id { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public char SingleChar { get; set; }
        public TimeSpan Duration { get; set; }
        public ExtendedTestEnum Status { get; set; }

        // Nullable типы
        public string? OptionalName { get; set; }
        public int? OptionalAge { get; set; }
        public double? OptionalPrice { get; set; }
        public bool? OptionalFlag { get; set; }
        public DateTime? OptionalDate { get; set; }
        public Guid? OptionalId { get; set; }
        public char? OptionalChar { get; set; }
        public TimeSpan? OptionalDuration { get; set; }
        public ExtendedTestEnum? OptionalStatus { get; set; }
        public float? OptionalSmallPrice { get; set; }
        public decimal? OptionalPrecisePrice { get; set; }
        public short? OptionalSmallNumber { get; set; }
        public byte? OptionalTinyNumber { get; set; }

        // Массивы
        public List<string> Tags { get; set; } = new();
        public int[] Numbers { get; set; } = Array.Empty<int>();
        public short[] SmallNumbers { get; set; } = Array.Empty<short>();
        public byte[] TinyNumbers { get; set; } = Array.Empty<byte>();
        public List<double> Prices { get; set; } = new();
        public float[] SmallPrices { get; set; } = Array.Empty<float>();
        public decimal[] PrecisePrices { get; set; } = Array.Empty<decimal>();
        public bool[] Flags { get; set; } = Array.Empty<bool>();
        public DateTime[] Dates { get; set; } = Array.Empty<DateTime>();
        public Guid[] Ids { get; set; } = Array.Empty<Guid>();
        public char[] Chars { get; set; } = Array.Empty<char>();
        public TimeSpan[] Durations { get; set; } = Array.Empty<TimeSpan>();

#if NET6_0_OR_GREATER
        // .NET 6+ типы
        public DateOnly? BirthDate { get; set; }
        public TimeOnly? MeetingTime { get; set; }
#endif
    }

    public enum ExtendedTestEnum // Renamed from TestEnum to avoid collision
    {
        None = 0, Active = 1, Inactive = 2, Pending = 3, Archived = 4, Processing = 5, Completed = 6
    }

    // Модель для чтения архивных записей
    public class ArchivedObjectRecord
    {
        public long _id { get; set; }
        public string? _name { get; set; }
        public string? _note { get; set; }
        public DateTime _date_create { get; set; }
        public DateTime _date_modify { get; set; }
        public DateTime _date_delete { get; set; }
        public string? _values { get; set; }
        public Guid? _hash { get; set; }
        public long _id_scheme { get; set; }
        public long _id_owner { get; set; }
        public long _id_who_change { get; set; }
    }
}
