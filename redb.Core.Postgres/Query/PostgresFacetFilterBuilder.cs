using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using redb.Core.Query.FacetFilters;
using redb.Core.Query.QueryExpressions;

namespace redb.Core.Postgres.Query;

/// <summary>
/// Построитель JSON фильтров для функции search_objects_with_facets
/// ОБНОВЛЕНО: Поддержка новой парадигмы - 25+ операторов, nullable поля, Class поля, массивы
/// </summary>
public class PostgresFacetFilterBuilder : IFacetFilterBuilder
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger? _logger;

    public PostgresFacetFilterBuilder(ILogger? logger = null)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    public string BuildFacetFilters(FilterExpression? filter)
    {
        if (filter == null)
        {
            _logger?.LogDebug("LINQ Filter: No filter provided, returning empty filter: {{}}");
            return "{}";
        }

        var filterObject = BuildFilterObject(filter);
        var filterJson = JsonSerializer.Serialize(filterObject, _jsonOptions);
        
        _logger?.LogDebug("LINQ Filter Generated: {FilterJson}", filterJson);
        return filterJson;
    }

    public string BuildOrderBy(IReadOnlyList<OrderingExpression> orderings)
    {
        if (!orderings.Any())
        {
            _logger?.LogDebug("LINQ OrderBy: No ordering provided, returning empty array: []");
            return "[]";
        }

        var orderArray = orderings.Select(o => new Dictionary<string, string>
        {
            [o.Property.Name] = o.Direction == SortDirection.Ascending ? "asc" : "desc"
        }).ToArray();

        var orderJson = JsonSerializer.Serialize(orderArray, _jsonOptions);
        _logger?.LogDebug("LINQ OrderBy Generated: {OrderJson}", orderJson);
        return orderJson;
    }

    public QueryParameters BuildQueryParameters(int? limit = null, int? offset = null)
    {
        return new QueryParameters(limit, offset);
    }

    private object BuildFilterObject(FilterExpression filter)
    {
        return filter switch
        {
            ComparisonExpression comparison => BuildComparisonFilter(comparison),
            LogicalExpression logical => BuildLogicalFilter(logical),
            NullCheckExpression nullCheck => BuildNullCheckFilter(nullCheck),
            InExpression inExpr => BuildInFilter(inExpr),
            _ => throw new NotSupportedException($"Filter expression type {filter.GetType().Name} is not supported")
        };
    }

    private object BuildComparisonFilter(ComparisonExpression comparison)
    {
        var fieldName = BuildFieldPath(comparison.Property);
        
        // ✅ ИСПРАВЛЕНИЕ: Преобразуем значение в правильный тип на основе схемы поля!
        var originalValue = comparison.Value;
        var value = ConvertValueToFieldType(comparison.Value, comparison.Property.Type);
        
        // 🔍 ДЕТАЛЬНОЕ ЛОГИРОВАНИЕ ПРЕОБРАЗОВАНИЯ ТИПОВ
        _logger?.LogInformation($"🔍 TYPE CONVERSION: Field '{comparison.Property.Name}' (Type: {comparison.Property.Type.Name})");
        _logger?.LogInformation($"   📥 Original value: {originalValue} ({originalValue?.GetType().Name ?? "null"})");
        _logger?.LogInformation($"   📤 Converted value: {value} ({value?.GetType().Name ?? "null"})");
        _logger?.LogInformation($"   🎯 Operator: {comparison.Operator}");

        // 🔍 СПЕЦИАЛЬНОЕ ЛОГИРОВАНИЕ ДЛЯ EQUALITY
        if (comparison.Operator == ComparisonOperator.Equal)
        {
            // ✅ ИСПРАВЛЕНИЕ JSON СЕРИАЛИЗАЦИИ: Для Double типов принудительно создаем дробное число  
            var finalValue = value;
            if (comparison.Property.Type == typeof(double) || comparison.Property.Type == typeof(double?))
            {
                if (value is double doubleVal && doubleVal == Math.Floor(doubleVal))
                {
                    // Преобразуем 2000.0 в "2000.0" чтобы SQL понимал что это Double
                    finalValue = doubleVal.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);  // 2000 → "2000.0"
                    _logger?.LogInformation($"   🔧 FIXED: Принудительно создаем double string: {finalValue}");
                }
            }
            
            var result = new Dictionary<string, object>
            {
                [fieldName] = new Dictionary<string, object?> { ["$eq"] = finalValue }
            };
            
            // 🔍 ЛОГИРУЕМ ИТОГОВЫЙ JSON ФИЛЬТР  
            _logger?.LogInformation($"   📋 Generated filter: {{{fieldName}: {{\"$eq\": {finalValue}}}}}");
            return result;
        }
        
        return comparison.Operator switch
        {
            ComparisonOperator.NotEqual => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$ne"] = value } 
            },
            ComparisonOperator.GreaterThan => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$gt"] = value } 
            },
            ComparisonOperator.GreaterThanOrEqual => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$gte"] = value } 
            },
            ComparisonOperator.LessThan => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$lt"] = value } 
            },
            ComparisonOperator.LessThanOrEqual => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$lte"] = value } 
            },
            ComparisonOperator.Contains => BuildContainsFilter(fieldName, value, false),
            ComparisonOperator.ContainsIgnoreCase => BuildContainsFilter(fieldName, value, true),
            ComparisonOperator.StartsWith => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$startsWith"] = value } 
            },
            ComparisonOperator.StartsWithIgnoreCase => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$startsWithIgnoreCase"] = value } 
            },
            ComparisonOperator.EndsWith => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$endsWith"] = value } 
            },
            ComparisonOperator.EndsWithIgnoreCase => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$endsWithIgnoreCase"] = value } 
            },
            
            // 🎯 NULL СЕМАНТИКА
            ComparisonOperator.Exists => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object> { ["$exists"] = value } 
            },
            // 🚀 БАЗОВЫЕ ОПЕРАТОРЫ МАССИВОВ
            ComparisonOperator.ArrayContains => BuildArrayFilter(fieldName, "$arrayContains", value),
            ComparisonOperator.ArrayAny => BuildArrayFilter(fieldName, "$arrayAny", true),
            ComparisonOperator.ArrayEmpty => BuildArrayFilter(fieldName, "$arrayEmpty", true),
            ComparisonOperator.ArrayCount => BuildArrayFilter(fieldName, "$arrayCount", value),
            ComparisonOperator.ArrayCountGt => BuildArrayFilter(fieldName, "$arrayCountGt", value),
            ComparisonOperator.ArrayCountGte => BuildArrayFilter(fieldName, "$arrayCountGte", value),
            ComparisonOperator.ArrayCountLt => BuildArrayFilter(fieldName, "$arrayCountLt", value),
            ComparisonOperator.ArrayCountLte => BuildArrayFilter(fieldName, "$arrayCountLte", value),
            
            // 🎯 ПОЗИЦИОННЫЕ ОПЕРАТОРЫ МАССИВОВ
            ComparisonOperator.ArrayAt => BuildArrayFilter(fieldName, "$arrayAt", value),
            ComparisonOperator.ArrayFirst => BuildArrayFilter(fieldName, "$arrayFirst", value),
            ComparisonOperator.ArrayLast => BuildArrayFilter(fieldName, "$arrayLast", value),
            
            // 🔍 ПОИСКОВЫЕ ОПЕРАТОРЫ МАССИВОВ
            ComparisonOperator.ArrayStartsWith => BuildArrayFilter(fieldName, "$arrayStartsWith", value),
            ComparisonOperator.ArrayEndsWith => BuildArrayFilter(fieldName, "$arrayEndsWith", value),
            ComparisonOperator.ArrayMatches => BuildArrayFilter(fieldName, "$arrayMatches", value),
            
            // 📈 АГРЕГАЦИОННЫЕ ОПЕРАТОРЫ МАССИВОВ
            ComparisonOperator.ArraySum => BuildArrayFilter(fieldName, "$arraySum", value),
            ComparisonOperator.ArrayAvg => BuildArrayFilter(fieldName, "$arrayAvg", value),
            ComparisonOperator.ArrayMin => BuildArrayFilter(fieldName, "$arrayMin", value),
            ComparisonOperator.ArrayMax => BuildArrayFilter(fieldName, "$arrayMax", value),
            _ => throw new NotSupportedException($"Comparison operator {comparison.Operator} is not supported")
        };
    }

    private object BuildLogicalFilter(LogicalExpression logical)
    {
        return logical.Operator switch
        {
            LogicalOperator.And => BuildAndFilter(logical.Operands),
            LogicalOperator.Or => new Dictionary<string, object> 
            { 
                ["$or"] = logical.Operands.Select(BuildFilterObject).ToArray() 
            },
            LogicalOperator.Not => new Dictionary<string, object> 
            { 
                ["$not"] = BuildFilterObject(logical.Operands.First()) 
            },
            _ => throw new NotSupportedException($"Logical operator {logical.Operator} is not supported")
        };
    }

    private object BuildAndFilter(IReadOnlyList<FilterExpression> operands)
    {
        // Для AND мы можем объединить условия в один объект, если они не конфликтуют
        // В противном случае используем $and
        var result = new Dictionary<string, object>();

        foreach (var operand in operands)
        {
            var filterObj = BuildFilterObject(operand);
            
            if (filterObj is Dictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    if (result.ContainsKey(kvp.Key))
                    {
                        // Конфликт ключей - используем $and
                        return new Dictionary<string, object>
                        {
                            ["$and"] = operands.Select(BuildFilterObject).ToArray()
                        };
                    }
                    result[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                // Сложный объект - используем $and
                return new Dictionary<string, object>
                {
                    ["$and"] = operands.Select(BuildFilterObject).ToArray()
                };
            }
        }

        return result;
    }

    private object BuildNullCheckFilter(NullCheckExpression nullCheck)
    {
        var fieldName = BuildFieldPath(nullCheck.Property);
        
        if (nullCheck.IsNull)
        {
            // 🎯 ОПТИМАЛЬНАЯ NULL СЕМАНТИКА - поле НЕ существует в _values
            // Используем $exists: false для явности
            return new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object> { ["$exists"] = false } 
            };
        }
        else
        {
            // 🎯 ОПТИМАЛЬНАЯ NULL СЕМАНТИКА - поле существует с реальным не-NULL значением
            // Используем улучшенный $ne null, который проверяет наличие конкретных значений
            return new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$ne"] = null } 
            };
        }
    }

    private object BuildInFilter(InExpression inExpr)
    {
        var fieldName = BuildFieldPath(inExpr.Property);
        
        return new Dictionary<string, object> 
        { 
            [fieldName] = new Dictionary<string, object> { ["$in"] = inExpr.Values.ToArray() } 
        };
    }

    // ===== 🚀 НОВЫЕ МЕТОДЫ ДЛЯ НОВОЙ ПАРАДИГМЫ =====

    /// <summary>
    /// Построение пути поля с поддержкой Class полей (Contact.Name, Contacts[].Email)
    /// </summary>
    private string BuildFieldPath(redb.Core.Query.QueryExpressions.PropertyInfo property)
    {
        var fieldPath = property.Name;
        
        // 🎯 ОПРЕДЕЛЯЕМ ТИП ПОЛЯ ДЛЯ CLASS ПОЛЕЙ
        if (IsClassField(property))
        {
            // Class поле: Contact.Name, Address.City
            return fieldPath; // Поле уже содержит полный путь от парсера
        }
        
        if (IsClassArrayField(property))
        {
            // Class массив: Contacts[].Email, Addresses[].Street
            return fieldPath; // Поле уже содержит полный путь от парсера  
        }
        
        if (IsCollectionType(property.Type))
        {
            // Обычный массив: Tags[], Scores[], Categories[]
            if (!fieldPath.EndsWith("[]"))
            {
                return fieldPath + "[]";
            }
        }
        
        // Обычное поле: Name, Age, Status
        return fieldPath;
    }

    /// <summary>
    /// 🎯 ЗАКАЗЧИКОВА СЕМАНТИКА: Построение фильтра Contains с поддержкой регистронезависимого поиска
    /// Поддерживает: r.Article.Contains(filter, StringComparison.OrdinalIgnoreCase)
    /// </summary>
    private object BuildContainsFilter(string fieldName, object? value, bool ignoreCase)
    {
        if (ignoreCase)
        {
            // 🚀 РЕГИСТРОНЕЗАВИСИМЫЙ ПОИСК
            return new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$containsIgnoreCase"] = value } 
            };
        }
        else
        {
            // 📝 ОБЫЧНЫЙ ПОИСК С УЧЕТОМ РЕГИСТРА
            return new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$contains"] = value } 
            };
        }
    }

    /// <summary>
    /// Построение фильтров для массивов с поддержкой nullable
    /// </summary>
    private object BuildArrayFilter(string fieldName, string operatorName, object? value, bool isNullable = false)
    {
        // 🔧 ИСПРАВЛЯЕМ ДВОЙНЫЕ СКОБКИ - НЕ добавляем "[]" если уже есть
        var arrayFieldName = fieldName.EndsWith("[]") ? fieldName : fieldName + "[]";
        
        if (isNullable && value == null)
        {
            // Nullable массив - ищем отсутствие массива
            return new Dictionary<string, object> 
            { 
                [arrayFieldName] = new Dictionary<string, object> { ["$exists"] = false }
            };
        }
        
        return new Dictionary<string, object> 
        { 
            [arrayFieldName] = new Dictionary<string, object?> { [operatorName] = value } 
        };
    }

    /// <summary>
    /// 🎯 ЗАКАЗЧИКОВАЯ СЕМАНТИКА: Построение фильтров для nullable полей
    /// Поддерживает: r.Auction != null && r.Auction.Costs > 100
    /// </summary>
    private object BuildNullableFieldFilter(string fieldName, object? value, ComparisonOperator op)
    {
        if (value == null)
        {
            // Nullable поле с null значением
            switch (op)
            {
                case ComparisonOperator.Equal:
                    // field == null → поле отсутствует
                    return new Dictionary<string, object> 
                    { 
                        [fieldName] = new Dictionary<string, object> { ["$exists"] = false } 
                    };
                    
                case ComparisonOperator.NotEqual:
                    // field != null → поле существует с любым значением  
                    return new Dictionary<string, object> 
                    { 
                        [fieldName] = new Dictionary<string, object> { ["$exists"] = true } 
                    };
                    
                default:
                    throw new NotSupportedException($"Operator {op} не поддерживается для nullable поля с null значением");
            }
        }
        
        // Nullable поле с реальным значением - обычная логика
        var operatorName = op switch
        {
            ComparisonOperator.Equal => "=",
            ComparisonOperator.NotEqual => "$ne", 
            ComparisonOperator.GreaterThan => "$gt",
            ComparisonOperator.GreaterThanOrEqual => "$gte",
            ComparisonOperator.LessThan => "$lt", 
            ComparisonOperator.LessThanOrEqual => "$lte",
            ComparisonOperator.Contains => "$contains",
            ComparisonOperator.StartsWith => "$startsWith",
            ComparisonOperator.EndsWith => "$endsWith",
            _ => throw new NotSupportedException($"Operator {op} не поддерживается для nullable поля")
        };
        
        if (operatorName == "=")
        {
            return new Dictionary<string, object?> { [fieldName] = value };
        }
        
        return new Dictionary<string, object> 
        { 
            [fieldName] = new Dictionary<string, object?> { [operatorName] = value } 
        };
    }

    /// <summary>
    /// Проверка на nullable тип поля
    /// </summary>
    private bool IsNullableType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    /// <summary>
    /// Проверка на массив/коллекцию
    /// </summary>
    private bool IsCollectionType(Type type)
    {
        if (type == typeof(string)) return false; // string не коллекция для наших целей
        
        return type.IsArray || 
               (type.IsGenericType && (
                   type.GetGenericTypeDefinition() == typeof(List<>) ||
                   type.GetGenericTypeDefinition() == typeof(IList<>) ||
                   type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                   type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
               ));
    }

    /// <summary>
    /// Проверка на Class поле (Contact.Name, Address.City)
    /// </summary>
    private bool IsClassField(redb.Core.Query.QueryExpressions.PropertyInfo property)
    {
        // Class поле определяется по наличию точки в имени и отсутствию [] 
        return property.Name.Contains('.') && !property.Name.Contains("[]");
    }

    /// <summary>
    /// Проверка на Class массив (Contacts[].Email, Addresses[].Street)
    /// </summary>
    private bool IsClassArrayField(redb.Core.Query.QueryExpressions.PropertyInfo property)
    {
        // Class массив определяется по наличию и точки и [] в имени
        return property.Name.Contains('.') && property.Name.Contains("[]");
    }

    /// <summary>
    /// Проверка на бизнес-класс (не примитив и не коллекция)
    /// </summary>
    private bool IsBusinessClass(Type type)
    {
        // Бизнес-класс: не примитив, не строка, не коллекция, не nullable примитив
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || 
            type == typeof(DateTime) || type == typeof(Guid))
            return false;
            
        if (IsNullableType(type))
        {
            var underlyingType = Nullable.GetUnderlyingType(type)!;
            return IsBusinessClass(underlyingType);
        }
        
        if (IsCollectionType(type))
            return false;
            
        // Это бизнес-класс (Address, Contact, etc.)
        return type.IsClass;
    }
    
    /// <summary>
    /// ✅ ИСПРАВЛЕНИЕ ПРОБЛЕМЫ №4: Преобразует значение в правильный тип поля
    /// Решает проблему когда Price (double) ищется как integer значение
    /// </summary>
    private object? ConvertValueToFieldType(object? value, Type fieldType)
    {
        if (value == null) return null;
        
        // Убираем Nullable wrapper если есть
        var targetType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
        
        // Если типы уже совпадают - возвращаем как есть
        if (value.GetType() == targetType)
            return value;
            
        try
        {
            // ✅ ЧИСЛОВЫЕ ТИПЫ - основная причина проблемы!
            if (targetType == typeof(double))
            {
                return Convert.ToDouble(value);  // 2000 → 2000.0
            }
            else if (targetType == typeof(float))
            {
                return Convert.ToSingle(value);
            }
            else if (targetType == typeof(decimal))
            {
                return Convert.ToDecimal(value);
            }
            else if (targetType == typeof(long))
            {
                return Convert.ToInt64(value);
            }
            else if (targetType == typeof(int))
            {
                return Convert.ToInt32(value);
            }
            else if (targetType == typeof(short))
            {
                return Convert.ToInt16(value);
            }
            else if (targetType == typeof(byte))
            {
                return Convert.ToByte(value);
            }
            
            // ✅ БУЛЕВСКИЕ ТИПЫ
            else if (targetType == typeof(bool))
            {
                return Convert.ToBoolean(value);
            }
            
            // ✅ ДАТА-ВРЕМЯ
            else if (targetType == typeof(DateTime))
            {
                return Convert.ToDateTime(value);
            }
            
            // ✅ GUID
            else if (targetType == typeof(Guid))
            {
                if (value is string guidStr)
                    return Guid.Parse(guidStr);
                return (Guid)value;
            }
            
            // ✅ СТРОКИ
            else if (targetType == typeof(string))
            {
                return value.ToString();
            }
            
            // Для других типов возвращаем как есть
            return value;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"⚠️ Не удалось преобразовать значение {value} ({value.GetType().Name}) в тип {targetType.Name}: {ex.Message}");
            return value; // Fallback - возвращаем оригинальное значение
        }
    }
}
