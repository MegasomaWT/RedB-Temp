using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using redb.Core.Models.Contracts;

namespace redb.Core.Providers
{
    /// <summary>
    /// Результат валидации схемы
    /// </summary>
    public class SchemaValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationIssue> Issues { get; set; } = new();
        public SchemaChangeReport? ChangeReport { get; set; }
    }

    /// <summary>
    /// Проблема валидации
    /// </summary>
    public class ValidationIssue
    {
        public ValidationSeverity Severity { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? SuggestedFix { get; set; }
    }

    /// <summary>
    /// Уровень серьезности проблемы
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Отчет об изменениях схемы
    /// </summary>
    public class SchemaChangeReport
    {
        public List<StructureChange> Changes { get; set; } = new();
        public bool HasBreakingChanges { get; set; }
    }

    /// <summary>
    /// Изменение структуры
    /// </summary>
    public class StructureChange
    {
        public ChangeType Type { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public bool IsBreaking { get; set; }
    }

    /// <summary>
    /// Тип изменения
    /// </summary>
    public enum ChangeType
    {
        Added,
        Removed,
        Modified,
        TypeChanged,
        NullabilityChanged,
        ArrayChanged
    }

    /// <summary>
    /// Информация о поддерживаемом типе
    /// </summary>
    public class SupportedType
    {
        public string Name { get; set; } = string.Empty;
        public string DbType { get; set; } = string.Empty;
        public string DotNetType { get; set; } = string.Empty;
        public long Id { get; set; }
        public bool SupportsArrays { get; set; } = true;
        public bool SupportsNullability { get; set; } = true;
        public string? Description { get; set; }
    }

    /// <summary>
    /// Провайдер валидации схем и типов
    /// </summary>
    public interface IValidationProvider
    {
        /// <summary>
        /// Получить все поддерживаемые типы
        /// </summary>
        Task<List<SupportedType>> GetSupportedTypesAsync();

        /// <summary>
        /// Проверить соответствие C# типа поддерживаемым типам REDB
        /// </summary>
        Task<ValidationIssue?> ValidateTypeAsync(Type csharpType, string propertyName);

        /// <summary>
        /// Валидация схемы перед синхронизацией
        /// </summary>
        Task<SchemaValidationResult> ValidateSchemaAsync<TProps>(string schemeName, bool strictDeleteExtra = true) where TProps : class;
        
        /// <summary>
        /// Валидация схемы перед синхронизацией (с контрактом)
        /// </summary>
        Task<SchemaValidationResult> ValidateSchemaAsync<TProps>(IRedbScheme scheme, bool strictDeleteExtra = true) where TProps : class;

        /// <summary>
        /// Проверить совместимость изменений схемы
        /// </summary>
        Task<SchemaChangeReport> AnalyzeSchemaChangesAsync<TProps>(IRedbScheme scheme) where TProps : class;

        /// <summary>
        /// Проверить обязательность полей и массивы
        /// </summary>
        ValidationIssue? ValidatePropertyConstraints(Type propertyType, string propertyName, bool isRequired, bool isArray);
    }
}
