using redb.Core.Providers;
using redb.Core.DBModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using redb.Core.Postgres.Extensions;
using redb.Core.Models.Entities;
using redb.Core.Models.Contracts;

namespace redb.Core.Postgres.Providers
{
    /// <summary>
    /// PostgreSQL реализация провайдера валидации
    /// </summary>
    public class PostgresValidationProvider : IValidationProvider
    {
        private readonly RedbContext _context;
        private List<SupportedType>? _supportedTypesCache;

        public PostgresValidationProvider(RedbContext context)
        {
            _context = context;
        }

        public async Task<List<SupportedType>> GetSupportedTypesAsync()
        {
            if (_supportedTypesCache != null)
                return _supportedTypesCache;

            var dbTypes = await _context.Set<_RType>().ToListAsync();
            
            _supportedTypesCache = dbTypes.Select(t => new SupportedType
            {
                Id = t.Id,
                Name = t.Name,
                DbType = t.DbType ?? "String",
                DotNetType = t.Type1 ?? "string",
                SupportsArrays = t.Name != "Object" && t.Name != "ListItem", // Объекты и ListItem пока не поддерживают массивы в полной мере
                SupportsNullability = t.Name != "Object", // Объекты всегда nullable через ID
                Description = GetTypeDescription(t.Name)
            }).ToList();

            return _supportedTypesCache;
        }

        public async Task<ValidationIssue?> ValidateTypeAsync(Type csharpType, string propertyName)
        {
            var supportedTypes = await GetSupportedTypesAsync();
            var underlyingType = Nullable.GetUnderlyingType(csharpType) ?? csharpType;
            
            // Проверяем базовые типы
            var mappedTypeName = await MapCSharpTypeToRedbTypeAsync(underlyingType);
            var supportedType = supportedTypes.FirstOrDefault(t => t.Name == mappedTypeName);
            
            if (supportedType == null)
            {
                return new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    PropertyName = propertyName,
                    Message = $"Тип '{csharpType.Name}' не поддерживается в REDB",
                    SuggestedFix = $"Используйте один из поддерживаемых типов: {string.Join(", ", supportedTypes.Where(t => t.SupportsNullability).Select(t => t.DotNetType))}"
                };
            }

            // Проверяем специфичные ограничения
            if (underlyingType == typeof(decimal))
            {
                return new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    PropertyName = propertyName,
                    Message = "Тип 'decimal' будет преобразован в 'double', возможна потеря точности",
                    SuggestedFix = "Рассмотрите использование 'double' напрямую или храните как 'string' для точных вычислений"
                };
            }

            if (underlyingType == typeof(float))
            {
                return new ValidationIssue
                {
                    Severity = ValidationSeverity.Info,
                    PropertyName = propertyName,
                    Message = "Тип 'float' будет преобразован в 'double'",
                    SuggestedFix = "Рассмотрите использование 'double' напрямую"
                };
            }

            return null;
        }

        public async Task<SchemaValidationResult> ValidateSchemaAsync<TProps>(string schemeName, bool strictDeleteExtra = true) where TProps : class
        {
            var result = new SchemaValidationResult { IsValid = true };
            var properties = typeof(TProps).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !p.ShouldIgnoreForRedb())
                .ToArray();
            var nullabilityContext = new NullabilityInfoContext();

            // Валидация каждого свойства
            foreach (var property in properties)
            {
                // Проверка типа
                var typeIssue = await ValidateTypeAsync(property.PropertyType, property.Name);
                if (typeIssue != null)
                {
                    result.Issues.Add(typeIssue);
                    if (typeIssue.Severity == ValidationSeverity.Error)
                        result.IsValid = false;
                }

                // Проверка ограничений
                var nullabilityInfo = nullabilityContext.Create(property);
                var isArray = IsArrayType(property.PropertyType);
                var baseType = isArray ? GetArrayElementType(property.PropertyType) : property.PropertyType;
                var isRequired = nullabilityInfo.WriteState != NullabilityState.Nullable && 
                                Nullable.GetUnderlyingType(baseType) == null;

                var constraintIssue = ValidatePropertyConstraints(property.PropertyType, property.Name, isRequired, isArray);
                if (constraintIssue != null)
                {
                    result.Issues.Add(constraintIssue);
                    if (constraintIssue.Severity == ValidationSeverity.Error)
                        result.IsValid = false;
                }
            }

            // Анализ изменений схемы если она уже существует
            var existingScheme = await _context.Schemes.FirstOrDefaultAsync(s => s.Name == schemeName);
            if (existingScheme != null)
            {
                result.ChangeReport = await AnalyzeSchemaChangesAsync<TProps>(existingScheme.Id);
                if (result.ChangeReport.HasBreakingChanges && strictDeleteExtra)
                {
                    result.Issues.Add(new ValidationIssue
                    {
                        Severity = ValidationSeverity.Warning,
                        PropertyName = "Schema",
                        Message = "Обнаружены критические изменения схемы при strictDeleteExtra=true",
                        SuggestedFix = "Проверьте отчет об изменениях или установите strictDeleteExtra=false"
                    });
                }
            }

            return result;
        }

        public async Task<SchemaChangeReport> AnalyzeSchemaChangesAsync<TProps>(long schemeId) where TProps : class
        {
            var report = new SchemaChangeReport();
            var properties = typeof(TProps).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !p.ShouldIgnoreForRedb())
                .ToArray();
            var nullabilityContext = new NullabilityInfoContext();

            // Получаем существующие структуры
            var existingStructures = await _context.Structures
                .Include(s => s.TypeNavigation)
                .Where(s => s.IdScheme == schemeId)
                .ToListAsync();

            var existingNames = existingStructures.Select(s => s.Name).ToHashSet();
            var newNames = properties.Select(p => p.Name).ToHashSet();

            // Анализируем добавленные свойства
            foreach (var property in properties.Where(p => !existingNames.Contains(p.Name)))
            {
                report.Changes.Add(new StructureChange
                {
                    Type = ChangeType.Added,
                    PropertyName = property.Name,
                    NewValue = $"{property.PropertyType.Name} ({await MapCSharpTypeToRedbTypeAsync(property.PropertyType)})",
                    IsBreaking = false
                });
            }

            // Анализируем удаленные свойства
            foreach (var structure in existingStructures.Where(s => !newNames.Contains(s.Name)))
            {
                var change = new StructureChange
                {
                    Type = ChangeType.Removed,
                    PropertyName = structure.Name,
                    OldValue = $"{structure.TypeNavigation.Name}",
                    IsBreaking = true
                };
                report.Changes.Add(change);
                report.HasBreakingChanges = true;
            }

            // Анализируем измененные свойства
            foreach (var property in properties.Where(p => existingNames.Contains(p.Name)))
            {
                var existingStructure = existingStructures.First(s => s.Name == property.Name);
                var nullabilityInfo = nullabilityContext.Create(property);
                var isArray = IsArrayType(property.PropertyType);
                var baseType = isArray ? GetArrayElementType(property.PropertyType) : property.PropertyType;
                var isRequired = nullabilityInfo.WriteState != NullabilityState.Nullable && 
                                Nullable.GetUnderlyingType(baseType) == null;
                var newTypeName = await MapCSharpTypeToRedbTypeAsync(baseType);

                // Проверка изменения типа
                if (existingStructure.TypeNavigation.Name != newTypeName)
                {
                    var change = new StructureChange
                    {
                        Type = ChangeType.TypeChanged,
                        PropertyName = property.Name,
                        OldValue = existingStructure.TypeNavigation.Name,
                        NewValue = newTypeName,
                        IsBreaking = !AreTypesCompatible(existingStructure.TypeNavigation.Name, newTypeName)
                    };
                    report.Changes.Add(change);
                    if (change.IsBreaking)
                        report.HasBreakingChanges = true;
                }

                // Проверка изменения обязательности
                if (existingStructure.AllowNotNull != isRequired)
                {
                    var change = new StructureChange
                    {
                        Type = ChangeType.NullabilityChanged,
                        PropertyName = property.Name,
                        OldValue = existingStructure.AllowNotNull == true ? "required" : "optional",
                        NewValue = isRequired ? "required" : "optional",
                        IsBreaking = isRequired && existingStructure.AllowNotNull != true // Делаем поле обязательным
                    };
                    report.Changes.Add(change);
                    if (change.IsBreaking)
                        report.HasBreakingChanges = true;
                }

                // Проверка изменения массива
                if (existingStructure.IsArray != isArray)
                {
                    var change = new StructureChange
                    {
                        Type = ChangeType.ArrayChanged,
                        PropertyName = property.Name,
                        OldValue = existingStructure.IsArray == true ? "array" : "single",
                        NewValue = isArray ? "array" : "single",
                        IsBreaking = true // Изменение массива всегда критично
                    };
                    report.Changes.Add(change);
                    report.HasBreakingChanges = true;
                }
            }

            return report;
        }

        public ValidationIssue? ValidatePropertyConstraints(Type propertyType, string propertyName, bool isRequired, bool isArray)
        {
            // Проверка массивов
            if (isArray)
            {
                var elementType = GetArrayElementType(propertyType);
                // Для синхронного метода используем упрощенную проверку
                if (typeof(RedbObject<>).IsAssignableFrom(elementType))
                {
                    return new ValidationIssue
                    {
                        Severity = ValidationSeverity.Warning,
                        PropertyName = propertyName,
                        Message = "Массивы объектов требуют особого внимания при сериализации",
                        SuggestedFix = "Убедитесь что объекты в массиве имеют корректные ID"
                    };
                }
            }

            // Проверка обязательных полей
            if (isRequired && propertyType.IsClass && propertyType != typeof(string))
            {
                return new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    PropertyName = propertyName,
                    Message = "Обязательные ссылочные типы (кроме string) могут вызвать проблемы при десериализации",
                    SuggestedFix = "Рассмотрите возможность сделать поле nullable или предоставить значение по умолчанию"
                };
            }

            return null;
        }

        #region Helper Methods

        private Dictionary<Type, string>? _csharpToRedbTypeCache;
        
        private async Task<string> MapCSharpTypeToRedbTypeAsync(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            
            // Инициализируем кеш маппинга из БД если еще не загружен
            if (_csharpToRedbTypeCache == null)
            {
                await InitializeCSharpToRedbTypeMappingAsync();
            }

            // Ищем точное соответствие
            if (_csharpToRedbTypeCache!.TryGetValue(underlyingType, out var exactMatch))
                return exactMatch;

            // 🚀 ИСПРАВЛЕНИЕ: Правильная проверка дженериков RedbObject<>
            if (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(RedbObject<>))
                return "Object";

            // Если тип не найден, возвращаем String как fallback
            return "String";
        }

        private async Task InitializeCSharpToRedbTypeMappingAsync()
        {
            var allTypes = await _context.Set<_RType>().ToListAsync();
            _csharpToRedbTypeCache = new Dictionary<Type, string>();

            foreach (var dbType in allTypes)
            {
                var dotNetTypeName = dbType.Type1; // Используем Type1 из модели
                if (string.IsNullOrEmpty(dotNetTypeName))
                    continue;

                // Маппинг строковых представлений типов на реальные C# типы
                var csharpType = MapStringToType(dotNetTypeName);
                if (csharpType != null)
                {
                    _csharpToRedbTypeCache[csharpType] = dbType.Name;
                }
            }
        }

        // ===== НЕДОСТАЮЩИЕ МЕТОДЫ ИЗ КОНТРАКТА IValidationProvider =====
        
        public async Task<SchemaValidationResult> ValidateSchemaAsync<TProps>(IRedbScheme scheme, bool strictDeleteExtra = true) where TProps : class
        {
            return await ValidateSchemaAsync<TProps>(scheme.Name, strictDeleteExtra);
        }
        
        public async Task<SchemaChangeReport> AnalyzeSchemaChangesAsync<TProps>(IRedbScheme scheme) where TProps : class
        {
            return await AnalyzeSchemaChangesAsync<TProps>(scheme.Id);
        }

        private static Type? MapStringToType(string typeName)
        {
            return typeName switch
            {
                "string" => typeof(string),
                "int" => typeof(int),
                "long" => typeof(long),
                "short" => typeof(short),
                "byte" => typeof(byte),
                "double" => typeof(double),
                "float" => typeof(float),
                "decimal" => typeof(decimal),
                "boolean" => typeof(bool),
                "DateTime" => typeof(DateTime),
                "Guid" => typeof(Guid),
                "byte[]" => typeof(byte[]),
                "char" => typeof(char),
                "TimeSpan" => typeof(TimeSpan),
#if NET6_0_OR_GREATER
                "DateOnly" => typeof(DateOnly),
                "TimeOnly" => typeof(TimeOnly),
#endif
                "_RObject" => typeof(RedbObject<>),
                "_RListItem" => null, // Специальный тип, не маппится напрямую
                "Enum" => typeof(Enum),
                _ => null // Неизвестный тип
            };
        }

        private static bool IsArrayType(Type type)
        {
            return type.IsArray || 
                   (type.IsGenericType && 
                    (type.GetGenericTypeDefinition() == typeof(List<>) ||
                     type.GetGenericTypeDefinition() == typeof(IList<>) ||
                     type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                     type.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
        }

        private static Type GetArrayElementType(Type arrayType)
        {
            if (arrayType.IsArray)
                return arrayType.GetElementType()!;
            
            if (arrayType.IsGenericType)
                return arrayType.GetGenericArguments()[0];
            
            return typeof(object);
        }

        private static bool AreTypesCompatible(string oldType, string newType)
        {
            // Совместимые преобразования типов
            var compatibleMappings = new Dictionary<string, HashSet<string>>
            {
                ["String"] = new() { "String" },
                ["Long"] = new() { "Long", "Double" }, // Long можно преобразовать в Double
                ["Double"] = new() { "Double" },
                ["Boolean"] = new() { "Boolean" },
                ["DateTime"] = new() { "DateTime", "String" }, // DateTime можно сохранить как String
                ["Guid"] = new() { "Guid", "String" }, // Guid можно сохранить как String
                ["ByteArray"] = new() { "ByteArray", "String" }, // ByteArray можно сохранить как String (base64)
                ["Object"] = new() { "Object", "Long" } // Object хранится как Long (ID)
            };

            return compatibleMappings.TryGetValue(oldType, out var compatible) && 
                   compatible.Contains(newType);
        }

        private static string GetTypeDescription(string typeName)
        {
            return typeName switch
            {
                "String" => "Строковые значения (текст до 850 символов в _String)",
                "Long" => "Целые числа (int, long)",
                "Double" => "Числа с плавающей точкой (double, float, decimal)",
                "Boolean" => "Логические значения (true/false)",
                "DateTime" => "Дата и время",
                "Guid" => "Уникальные идентификаторы",
                "ByteArray" => "Бинарные данные",
                "Object" => "Ссылки на другие объекты REDB",
                "ListItem" => "Элементы списков",
                "Text" => "Длинные текстовые значения (устарел, используйте String)",
                _ => "Неизвестный тип"
            };
        }

        #endregion
    }
}
