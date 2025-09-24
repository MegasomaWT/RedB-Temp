using redb.Core.Models.Attributes;
using System;
using System.Text.Json.Serialization;

namespace redb.ConsoleTest.Models
{
    /// <summary>
    /// Тестовый класс для демонстрации работы [JsonIgnore]
    /// </summary>
    [RedbScheme("Тест JsonIgnore")]
    public class JsonIgnoreTestProps
    {
        // ✅ СОХРАНЯЕМЫЕ поля
        public string Name { get; set; } = "";
        public int Stock { get; set; }
        public double Price { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // ❌ ИГНОРИРУЕМЫЕ поля (НЕ будут сохранены в БД)
        [JsonIgnore]
        public string TempValue { get; set; } = "Временное значение";
        
        [JsonIgnore]
        public DateTime CacheTime { get; set; } = DateTime.Now;
        
        [JsonIgnore]
        public bool IsInMemoryOnly { get; set; } = true;
        
        [JsonIgnore]
        public string ComputedField => $"{Name} - {Stock} шт.";
        
        // ✅ СОХРАНЯЕМЫЕ поля продолжение
        public string Description { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }
}
