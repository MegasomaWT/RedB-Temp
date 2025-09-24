using System.Linq.Expressions;

namespace redb.Core.Query.QueryExpressions;

/// <summary>
/// Интерфейс для обработки Expression Tree
/// </summary>
public interface IExpressionVisitor<out TResult>
{
    /// <summary>
    /// Обработать выражение и вернуть результат
    /// </summary>
    TResult Visit(Expression expression);
}

/// <summary>
/// Информация о поле в выражении
/// </summary>
public record PropertyInfo(string Name, Type Type);

/// <summary>
/// Операторы сравнения
/// ОБНОВЛЕНО: Поддержка новой парадигмы с 25+ операторами массивов
/// </summary>
public enum ComparisonOperator
{
    // 📋 Базовые операторы
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    ContainsIgnoreCase,     // Contains с игнорированием регистра
    StartsWith,
    StartsWithIgnoreCase,   // StartsWith с игнорированием регистра
    EndsWith,
    EndsWithIgnoreCase,     // EndsWith с игнорированием регистра
    
    // 🎯 NULL семантика  
    Exists,             // $exists - явная проверка существования поля
    
    // 🚀 Базовые операторы массивов
    ArrayContains,      // $arrayContains - поиск значения в массиве
    ArrayAny,           // $arrayAny - проверка что массив не пустой
    ArrayEmpty,         // $arrayEmpty - проверка что массив пустой
    ArrayCount,         // $arrayCount - точное количество элементов
    ArrayCountGt,       // $arrayCountGt - количество элементов больше N
    ArrayCountGte,      // $arrayCountGte - количество элементов больше или равно N
    ArrayCountLt,       // $arrayCountLt - количество элементов меньше N
    ArrayCountLte,      // $arrayCountLte - количество элементов меньше или равно N
    
    // 🎯 Позиционные операторы массивов
    ArrayAt,            // $arrayAt - элемент массива по индексу
    ArrayFirst,         // $arrayFirst - первый элемент массива
    ArrayLast,          // $arrayLast - последний элемент массива
    
    // 🔍 Поисковые операторы массивов (для строк)
    ArrayStartsWith,    // $arrayStartsWith - строковые значения начинающиеся с префикса
    ArrayEndsWith,      // $arrayEndsWith - строковые значения заканчивающиеся суффиксом
    ArrayMatches,       // $arrayMatches - поиск по регулярному выражению
    
    // 📈 Агрегационные операторы массивов
    ArraySum,           // $arraySum - сумма числовых элементов
    ArrayAvg,           // $arrayAvg - среднее арифметическое
    ArrayMin,           // $arrayMin - минимальное значение
    ArrayMax            // $arrayMax - максимальное значение
}

/// <summary>
/// Логические операторы
/// </summary>
public enum LogicalOperator
{
    And,
    Or,
    Not
}

/// <summary>
/// Направление сортировки
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}
