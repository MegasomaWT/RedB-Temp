using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using redb.Core.Models.Attributes;
using redb.Core.Providers;

namespace redb.Core.Utils
{
    /// <summary>
    /// Автоматический реестр типов для полиморфной работы с объектами REDB
    /// Сканирует загруженные assembly и строит маппинг scheme_id/имя_схемы -> C# тип
    /// </summary>
    public class AutomaticTypeRegistry
    {
        private static readonly Dictionary<string, Type> _schemeToType = new();
        private static readonly Dictionary<long, Type> _schemeIdToType = new();
        private static bool _isInitialized = false;
        private static readonly object _lock = new();

        /// <summary>
        /// Инициализировать реестр типов при старте приложения
        /// Сканирует все assembly с RedbSchemeAttribute и создает маппинг
        /// </summary>
        /// <param name="schemeProvider">Провайдер для получения метаданных схем</param>
        public static async Task InitializeAsync(ISchemeSyncProvider schemeProvider)
        {
            lock (_lock)
            {
                if (_isInitialized)
                    return;
            }

            // Сканируем все загруженные assembly
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    var typesWithAttribute = assembly.GetTypes()
                        .Where(t => t.GetCustomAttribute<RedbSchemeAttribute>() != null)
                        .ToArray();
                        
                    foreach (var type in typesWithAttribute)
                    {
                        var attr = type.GetCustomAttribute<RedbSchemeAttribute>()!;
                        
                        // Имя схемы всегда = имя класса
                        var schemeName = attr.GetSchemeName(type);
                        
                        // Регистрируем по имени схемы
                        _schemeToType[schemeName] = type;
                        
                        // Также регистрируем по алиасу, если есть
                        if (!string.IsNullOrEmpty(attr.Alias))
                        {
                            _schemeToType[attr.Alias] = type;
                        }
                        
                        // Получаем scheme_id из БД и кешируем
                        try
                        {
                            var scheme = await schemeProvider.GetSchemeByNameAsync(schemeName);
                            if (scheme != null)
                            {
                                _schemeIdToType[scheme.Id] = type;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Логируем ошибку, но продолжаем инициализацию
                            Console.WriteLine($"Предупреждение: не удалось найти схему '{schemeName}' для типа {type.Name}: {ex.Message}");
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Игнорируем assembly с проблемами загрузки типов
                    Console.WriteLine($"Предупреждение: не удалось загрузить типы из assembly {assembly.FullName}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Игнорируем assembly с другими проблемами
                    Console.WriteLine($"Предупреждение: ошибка при сканировании assembly {assembly.FullName}: {ex.Message}");
                }
            }

            lock (_lock)
            {
                _isInitialized = true;
            }
            
            Console.WriteLine($"AutomaticTypeRegistry: зарегистрировано {_schemeToType.Count} типов, {_schemeIdToType.Count} scheme_id маппингов");
        }

        /// <summary>
        /// Получить C# тип по ID схемы
        /// </summary>
        /// <param name="schemeId">ID схемы</param>
        /// <returns>C# тип или null если не найден</returns>
        public static Type? GetTypeBySchemeId(long schemeId)
        {
            return _schemeIdToType.TryGetValue(schemeId, out var type) ? type : null;
        }

        /// <summary>
        /// Получить C# тип по имени схемы
        /// </summary>
        /// <param name="schemeName">Имя схемы</param>
        /// <returns>C# тип или null если не найден</returns>
        public static Type? GetTypeBySchemeName(string schemeName)
        {
            return _schemeToType.TryGetValue(schemeName, out var type) ? type : null;
        }

        /// <summary>
        /// Регистрировать тип вручную (для случаев когда автоматическое сканирование не работает)
        /// </summary>
        /// <param name="schemeName">Имя схемы</param>
        /// <param name="schemeId">ID схемы</param>
        /// <param name="type">C# тип</param>
        public static void RegisterType(string schemeName, long schemeId, Type type)
        {
            _schemeToType[schemeName] = type;
            _schemeIdToType[schemeId] = type;
        }

        /// <summary>
        /// Очистить реестр (для тестирования)
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _schemeToType.Clear();
                _schemeIdToType.Clear();
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Получить статистику реестра
        /// </summary>
        /// <returns>Информация о количестве зарегистрированных типов</returns>
        public static (int SchemeNames, int SchemeIds) GetStatistics()
        {
            return (_schemeToType.Count, _schemeIdToType.Count);
        }

        /// <summary>
        /// Проверить, инициализирован ли реестр
        /// </summary>
        public static bool IsInitialized => _isInitialized;
    }
}
