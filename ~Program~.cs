using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using redb.Core.Postgres;
using redb.Core;
using redb.Core.Models;
using redb.Core.Utils;
using System.Linq;

// Простой кастомный логгер без префиксов
public class SimpleConsoleLogger : ILogger
{
    public IDisposable BeginScope<TState>(TState state) => null!;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        var levelText = logLevel switch
        {
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
public class AnalyticsMetricsProps
{
    public long AdvertId { get; set; }
    public long? Baskets { get; set; }
    public long? Base { get; set; }
    public long? Association { get; set; }
    public double? Costs { get; set; }
    public long? Rate { get; set; }
}

public class AnalyticsRecordProps
{
    public DateTime Date { get; set; }
    public string Article { get; set; } = string.Empty;
    public long? Orders { get; set; }
    public long Stock { get; set; }
    public long? TotalCart { get; set; }
    public string? Tag { get; set; }
    public string? TestName { get; set; }
    public redb.Core.Models.RedbObject<AnalyticsMetricsProps>? AutoMetrics { get; set; }
    public redb.Core.Models.RedbObject<AnalyticsMetricsProps>? AuctionMetrics { get; set; }
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

internal class Program
{
    static async Task Main()
    {
        var services = new ServiceCollection();

        // Опция для детального логирования (для отладки)
        bool detailedLogging = Environment.GetEnvironmentVariable("DETAILED_LOGGING") == "true";

        // Логирование
        services.AddLogging(b =>
        {
            b.ClearProviders(); // Убираем все провайдеры по умолчанию
            b.AddProvider(new SimpleConsoleLoggerProvider()) // Наш кастомный логгер без префиксов
             .SetMinimumLevel(LogLevel.Information);

            if (!detailedLogging)
            {
                // Скрываем технические логи EF Core для обычного пользователя
                b.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning); // SQL запросы
                b.AddFilter("Microsoft.EntityFrameworkCore.Query", LogLevel.Warning); // Предупреждения о запросах
                b.AddFilter("Microsoft.EntityFrameworkCore.Update", LogLevel.Warning); // Операции обновления
            }
        });

        // Подключение к PostgreSQL (настройте строку под вашу среду)
        var connectionString = "Host=localhost;Port=5432;Username=postgres;Password=1;Database=redb;Pooling=true;";

        services.AddDbContext<redb.Core.Postgres.RedbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IRedbService, RedbService>();

        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("ConsoleTest");

        try
        {
            var redb = provider.GetRequiredService<IRedbService>();

            logger.LogInformation("🔗 === ЭТАП 1: ПОДКЛЮЧЕНИЕ К БАЗЕ ДАННЫХ ===");
            logger.LogInformation("DB Type: {dbType}", redb.dbType);
            logger.LogInformation("DB Version: {version}", redb.dbVersion);
            logger.LogInformation("DB Size: {size} bytes", redb.dbSize);

            logger.LogInformation("");
            logger.LogInformation("📖 === ЭТАП 2: ЗАГРУЗКА СУЩЕСТВУЮЩЕГО ОБЪЕКТА ===");
            logger.LogInformation("Загружаем объект ID=1021 из базы через get_object_json()...");
            var obj = await ((RedbService)redb).LoadAsync<AnalyticsRecordProps>(1021, depth: 3);
            logger.LogInformation("✅ Объект загружен: id={id}, name='{name}', scheme_id={schemeId}", obj.id, obj.name, obj.scheme_id);
            logger.LogInformation("   Properties: Article='{Article}', Date={Date}, Stock={Stock}", obj.properties.Article, obj.properties.Date, obj.properties.Stock);

            logger.LogInformation("");
            logger.LogInformation("🏗️ === ЭТАП 3: CODE-FIRST СИНХРОНИЗАЦИЯ СХЕМЫ ===");
            var schemeName = "TrueSight.Models.AnalyticsRecord";
            logger.LogInformation("Проверяем/создаем схему и синхронизируем структуры: {scheme}", schemeName);
            var schemeId = await redb.SyncSchemeAsync<AnalyticsRecordProps>(schemeName, alias: "Запись аналитики", strictDeleteExtra: true);
            logger.LogInformation("✅ Scheme ID: {schemeId}, структуры синхронизированы", schemeId);

            logger.LogInformation("");
            logger.LogInformation("🔐 === ЭТАП 4: ДЕМОНСТРАЦИЯ ОПЦИОНАЛЬНЫХ ПРОВЕРОК ПРАВ ===");
            logger.LogInformation("Проверяем права пользователя на редактирование объекта 1021...");
            var canEdit = await ((RedbService)redb).CanUserEditObject(-9223372036854775800, 1021);
            logger.LogInformation("✅ Результат проверки прав: {canEdit}", canEdit ? "РАЗРЕШЕНО" : "ЗАПРЕЩЕНО");

            logger.LogInformation("");
            logger.LogInformation("📋 Демонстрируем опциональную проверку прав при операциях:");

            // Загрузка БЕЗ проверки прав (по умолчанию)
            logger.LogInformation("  → LoadAsync БЕЗ проверки прав (по умолчанию checkPermissions=false)");
            var objWithoutCheck = await redb.LoadAsync<AnalyticsRecordProps>(1021);
            logger.LogInformation($"    ✅ Загружен: {objWithoutCheck.name}");

            // Загрузка С проверкой прав
            logger.LogInformation("  → LoadAsync С проверкой прав (checkPermissions=true)");
            try
            {
                var objWithCheck = await redb.LoadAsync<AnalyticsRecordProps>(1021, userId: -9223372036854775800, checkPermissions: true);
                logger.LogInformation($"    ✅ Загружен с проверкой прав: {objWithCheck.name}");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogInformation($"    ❌ Доступ запрещен: {ex.Message}");
            }

            logger.LogInformation("");
            logger.LogInformation("➕ === ЭТАП 5: СОЗДАНИЕ НОВОГО ОБЪЕКТА ===");
            logger.LogInformation("Создаем новый объект AnalyticsRecord...");
            var newObj = new redb.Core.Models.RedbObject<AnalyticsRecordProps>
            {
                scheme_id = schemeId,
                name = "Новая аналитическая запись",
                note = "Создана в тесте",
                owner_id = -9223372036854775800,
                who_change_id = -9223372036854775800,
                date_create = DateTime.Now,
                date_modify = DateTime.Now,
                properties = new AnalyticsRecordProps
                {
                    Date = DateTime.Today,
                    Article = "Тестовый артикул",
                    Stock = 100,
                    Orders = 5,
                    TotalCart = 10,
                    Tag = "тест",
                    TestName = "Console Test Create"
                }
            };

            logger.LogInformation("Структура создаваемого объекта:");
            logger.LogInformation("   Базовые поля: name='{name}', note='{note}', scheme_id={schemeId}",
                newObj.name, newObj.note, newObj.scheme_id);
            logger.LogInformation("   Properties (будут сохранены в _values):");
            logger.LogInformation("     Article: '{article}'", newObj.properties.Article);
            logger.LogInformation("     Date: {date}", newObj.properties.Date);
            logger.LogInformation("     Stock: {stock}", newObj.properties.Stock);
            logger.LogInformation("     Orders: {orders}", newObj.properties.Orders);
            logger.LogInformation("     TotalCart: {totalCart}", newObj.properties.TotalCart);
            logger.LogInformation("     Tag: '{tag}'", newObj.properties.Tag);
            logger.LogInformation("     TestName: '{testName}'", newObj.properties.TestName);

            logger.LogInformation("Сохраняем объект БЕЗ проверки прав (checkPermissions=false - по умолчанию)...");
            logger.LogInformation("   → INSERT в _objects (базовые поля)");
            logger.LogInformation("   → INSERT в _values (7 записей для properties)");
            logger.LogInformation("   → Автоматический расчет MD5 хеша");
            var newId = await redb.SaveAsync(newObj, checkPermissions: false); // Явно указываем для демонстрации
            logger.LogInformation("✅ Объект создан с ID: {newId}", newId);

            logger.LogInformation("");
            logger.LogInformation("🔍 === ЭТАП 6: ПРОВЕРКА СОЗДАННОГО ОБЪЕКТА ===");
            logger.LogInformation("Загружаем созданный объект {newId} для проверки...", newId);
            var createdObj = await ((RedbService)redb).LoadAsync<AnalyticsRecordProps>(newId);
            logger.LogInformation("✅ Проверка пройдена: name='{name}', TestName='{testName}'", createdObj.name, createdObj.properties.TestName);

            logger.LogInformation("");
            logger.LogInformation("✏️ === ЭТАП 7: ОБНОВЛЕНИЕ ОБЪЕКТА ===");
            logger.LogInformation("Объект ДО изменений:");
            logger.LogInformation("   Name: '{oldName}' → TestName: '{oldTestName}' → Stock: {oldStock}",
                createdObj.name, createdObj.properties.TestName, createdObj.properties.Stock);

            logger.LogInformation("Применяем изменения:");
            var oldName = createdObj.name;
            var oldTestName = createdObj.properties.TestName;
            var oldStock = createdObj.properties.Stock;

            createdObj.name = "Обновленная запись";
            createdObj.properties.TestName = "Console Test Update";
            createdObj.properties.Stock = 150;
            createdObj.date_modify = DateTime.Now;

            logger.LogInformation("   Name: '{oldName}' → '{newName}'", oldName, createdObj.name);
            logger.LogInformation("   TestName: '{oldTestName}' → '{newTestName}'", oldTestName, createdObj.properties.TestName);
            logger.LogInformation("   Stock: {oldStock} → {newStock}", oldStock, createdObj.properties.Stock);
            logger.LogInformation("   date_modify: обновлено до текущего времени");

            logger.LogInformation("Сохраняем изменения (UPDATE в _objects и _values)...");
            var updatedId = await redb.SaveAsync(createdObj);
            logger.LogInformation("✅ Объект обновлен, ID: {updatedId}", updatedId);

            logger.LogInformation("");
            logger.LogInformation("🎯 === ЭТАП 8: ФИНАЛЬНАЯ ПРОВЕРКА ===");
            logger.LogInformation("Загружаем финальное состояние объекта {updatedId}...", updatedId);
            var updatedObj = await ((RedbService)redb).LoadAsync<AnalyticsRecordProps>(updatedId);
            logger.LogInformation("✅ Финальный объект: name='{name}', TestName='{testName}', Stock={stock}",
                updatedObj.name, updatedObj.properties.TestName, updatedObj.properties.Stock);

            logger.LogInformation("Что изменилось в результате обновления:");
            logger.LogInformation("   В _objects: обновлены поля _name, _date_modify, _hash");
            logger.LogInformation("   В _values: обновлены значения Stock и TestName");
            logger.LogInformation("   MD5 хеш пересчитан автоматически на основе новых properties");

            logger.LogInformation("");
            logger.LogInformation("🔍 === ЭТАП 9: АНАЛИЗ ДАННЫХ В БАЗЕ ===");
            logger.LogInformation("Проверяем как данные сохранены в таблицах _objects и _values...");
            await CheckObjectInDatabase(redb, updatedId, logger);

            logger.LogInformation("");
            logger.LogInformation("📊 === ЭТАП 10: СРАВНИТЕЛЬНЫЙ АНАЛИЗ ===");
            logger.LogInformation("Сравниваем старый и новый объекты...");
            await CompareObjectsInDatabase(redb, new[] { 1021, updatedId }, logger);

            logger.LogInformation("");
            // ========================================
            // ЭТАП 11: ТЕСТИРОВАНИЕ УДАЛЕНИЯ ОБЪЕКТОВ
            // ========================================

            logger.LogInformation("🗑️ === ЭТАП 11: УДАЛЕНИЕ ОБЪЕКТОВ ===");

            // Создаем дополнительный объект для тестирования удаления
            logger.LogInformation("Создаем дополнительный объект для тестирования удаления...");
            var objectToDelete = new RedbObject<AnalyticsRecordProps>
            {
                name = "Объект для удаления",
                note = "Будет удален в тесте",
                scheme_id = schemeId,
                owner_id = -9223372036854775800,
                who_change_id = -9223372036854775800,
                properties = new AnalyticsRecordProps
                {
                    Article = "TEST_DELETE",
                    Date = DateTime.Now,
                    Stock = 999,
                    TestName = "ToBeDeleted"
                }
            };

            var deleteObjectId = await redb.SaveAsync(objectToDelete);
            logger.LogInformation($"✅ Создан объект для удаления: ID={deleteObjectId}");

            // Получаем контекст для прямых проверок БД
            var redbService = (RedbService)redb;
            var context = redbService.GetType().GetField("_redbContext",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(redbService)
                as redb.Core.Postgres.RedbContext;

            // Проверяем что объект существует до удаления
            var beforeDelete = await CheckObjectExists(context!, deleteObjectId);
            logger.LogInformation($"До удаления: объект {deleteObjectId} существует = {beforeDelete}");

            // Пытаемся удалить с неправильными правами (должен выбросить исключение)
            logger.LogInformation("Тест 1: Проверяем защиту от несанкционированного удаления (checkPermissions=true)...");
            try
            {
                await redb.DeleteAsync(deleteObjectId, 12345, checkPermissions: true); // Несуществующий пользователь с проверкой прав
                logger.LogInformation("❌ ОШИБКА: удаление должно было быть запрещено!");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogInformation($"✅ Защита работает: {ex.Message}");
            }

            // Удаляем объект БЕЗ проверки прав (системный режим)
            logger.LogInformation($"Тест 2: Удаляем объект {deleteObjectId} БЕЗ проверки прав (checkPermissions=false)...");
            try
            {
                var deleted = await redb.DeleteAsync(deleteObjectId, -9223372036854775800, checkPermissions: false);
                logger.LogInformation($"✅ Объект удален в системном режиме: {deleted}");

                // Проверяем что объект удален
                var afterDelete = await CheckObjectExists(context!, deleteObjectId);
                logger.LogInformation($"После удаления: объект {deleteObjectId} существует = {afterDelete}");

                // Проверяем что объект попал в архив
                var inArchive = await CheckObjectInArchive(context!, deleteObjectId);
                logger.LogInformation($"В архиве _deleted_objects: объект {deleteObjectId} найден = {inArchive}");

                if (inArchive)
                {
                    // Показываем содержимое архивной записи
                    await ShowArchivedObjectDetails(context!, deleteObjectId, logger);
                }
            }
            catch (Exception deleteEx)
            {
                logger.LogError(deleteEx, $"Ошибка при удалении объекта {deleteObjectId}: {deleteEx.Message}");
                if (deleteEx.InnerException != null)
                {
                    logger.LogError($"Внутренняя ошибка: {deleteEx.InnerException.Message}");
                }
                logger.LogError($"StackTrace: {deleteEx.StackTrace}");
                throw;
            }

            logger.LogInformation("");
            logger.LogInformation("🌳 === ЭТАП 12: ТЕСТИРОВАНИЕ ДРЕВОВИДНЫХ СТРУКТУР ===");
            await TestTreeFunctionality(logger, (RedbService)redb);

            logger.LogInformation("");
            logger.LogInformation("🎉 === ТЕСТ ЗАВЕРШЕН УСПЕШНО ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in console test");
        }
    }

    // Проверка объекта в базе данных: поля _objects и связанные _values
    // Вспомогательные методы для проверки удаления
    static async Task<bool> CheckObjectExists(redb.Core.Postgres.RedbContext redbContext, long objectId)
    {
        return await redbContext.Objects.AnyAsync(o => o.Id == objectId);
    }

    static async Task<bool> CheckObjectInArchive(redb.Core.Postgres.RedbContext redbContext, long objectId)
    {
        return await redbContext.Database
            .SqlQueryRaw<long>("SELECT _id FROM _deleted_objects WHERE _id = {0}", objectId)
            .AnyAsync();
    }

    static async Task ShowArchivedObjectDetails(redb.Core.Postgres.RedbContext redbContext, long objectId, ILogger logger)
    {
        logger.LogInformation("");
        logger.LogInformation("📋 === ДЕТАЛИ АРХИВНОЙ ЗАПИСИ ===");

        // Получаем архивную запись
        var archivedRecord = await redbContext.Database
            .SqlQueryRaw<ArchivedObjectRecord>(@"
                SELECT _id, _name, _note, _date_create, _date_modify, _date_delete, 
                       _values, _hash, _id_scheme, _id_owner, _id_who_change
                FROM _deleted_objects 
                WHERE _id = {0}", objectId)
            .FirstOrDefaultAsync();

        if (archivedRecord == null)
        {
            logger.LogWarning("Архивная запись для объекта {objectId} не найдена", objectId);
            return;
        }

        logger.LogInformation("Архивная запись объекта {id}:", archivedRecord._id);
        logger.LogInformation("  Name: {name}", archivedRecord._name);
        logger.LogInformation("  Note: {note}", archivedRecord._note);
        logger.LogInformation("  Scheme: {scheme}, Owner: {owner}, WhoChange: {whoChange}",
            archivedRecord._id_scheme, archivedRecord._id_owner, archivedRecord._id_who_change);
        logger.LogInformation("  Created: {created}, Modified: {modified}, Deleted: {deleted}",
            archivedRecord._date_create, archivedRecord._date_modify, archivedRecord._date_delete);
        logger.LogInformation("  Hash: {hash}", archivedRecord._hash);

        logger.LogInformation("");
        logger.LogInformation("📄 Архивированные _values (JSON):");
        if (string.IsNullOrEmpty(archivedRecord._values))
        {
            logger.LogInformation("  (нет значений)");
        }
        else
        {
            try
            {
                // Форматируем JSON для лучшей читаемости
                var jsonObj = System.Text.Json.JsonSerializer.Deserialize<object>(archivedRecord._values);
                var formattedJson = System.Text.Json.JsonSerializer.Serialize(jsonObj, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Показываем первые 500 символов JSON для обзора
                var preview = formattedJson.Length > 500 ? formattedJson.Substring(0, 500) + "..." : formattedJson;
                logger.LogInformation("  JSON Preview ({length} chars):", formattedJson.Length);
                logger.LogInformation("{preview}", preview);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Ошибка парсинга JSON: {error}", ex.Message);
                logger.LogInformation("  Raw Values: {values}", archivedRecord._values);
            }
        }
    }

    // === ТЕСТИРОВАНИЕ ДРЕВОВИДНЫХ СТРУКТУР ===

    static async Task TestTreeFunctionality(ILogger logger, RedbService redb)
    {
        try
        {
            logger.LogInformation("Создаем иерархическую структуру категорий товаров...");

            // Создаем корневую категорию
            var rootCategory = new redb.Core.Models.TreeRedbObject<AnalyticsRecordProps>
            {
                scheme_id = 1002,
                owner_id = -9223372036854775800,
                who_change_id = -9223372036854775800,
                name = "Все товары",
                note = "Корневая категория",
                properties = new AnalyticsRecordProps
                {
                    Article = "ROOT",
                    Date = DateTime.Now,
                    Stock = 0,
                    TestName = "Root Category"
                }
            };

            var rootId = await redb.SaveAsync(rootCategory);
            logger.LogInformation($"✅ Создана корневая категория: ID={rootId}");

            // Создаем дочерние категории
            var electronics = new redb.Core.Models.TreeRedbObject<AnalyticsRecordProps>
            {
                scheme_id = 1002,
                owner_id = -9223372036854775800,
                who_change_id = -9223372036854775800,
                name = "Электроника",
                note = "Категория электроники",
                properties = new AnalyticsRecordProps
                {
                    Article = "ELEC",
                    Date = DateTime.Now,
                    Stock = 50,
                    TestName = "Electronics Category"
                }
            };

            var electronicsId = await redb.CreateChildAsync(electronics, rootId);
            logger.LogInformation($"✅ Создана категория 'Электроника': ID={electronicsId}");

            var clothing = new redb.Core.Models.TreeRedbObject<AnalyticsRecordProps>
            {
                scheme_id = 1002,
                owner_id = -9223372036854775800,
                who_change_id = -9223372036854775800,
                name = "Одежда",
                note = "Категория одежды",
                properties = new AnalyticsRecordProps
                {
                    Article = "CLOTH",
                    Date = DateTime.Now,
                    Stock = 30,
                    TestName = "Clothing Category"
                }
            };

            var clothingId = await redb.CreateChildAsync(clothing, rootId);
            logger.LogInformation($"✅ Создана категория 'Одежда': ID={clothingId}");

            // Создаем подкатегории электроники
            var smartphones = new redb.Core.Models.TreeRedbObject<AnalyticsRecordProps>
            {
                scheme_id = 1002,
                owner_id = -9223372036854775800,
                who_change_id = -9223372036854775800,
                name = "Смартфоны",
                note = "Подкатегория смартфонов",
                properties = new AnalyticsRecordProps
                {
                    Article = "PHONE",
                    Date = DateTime.Now,
                    Stock = 15,
                    TestName = "Smartphones Subcategory"
                }
            };

            var smartphonesId = await redb.CreateChildAsync(smartphones, electronicsId);
            logger.LogInformation($"✅ Создана подкатегория 'Смартфоны': ID={smartphonesId}");

            logger.LogInformation("");
            logger.LogInformation("🔍 === ТЕСТИРОВАНИЕ МЕТОДОВ ДЕРЕВА ===");

            // Тест 1: Загрузка дерева
            logger.LogInformation("Тест 1: Загружаем полное дерево категорий...");
            var tree = await redb.LoadTreeAsync<AnalyticsRecordProps>(rootId, maxDepth: 5);
            logger.LogInformation($"✅ Загружено дерево: корень='{tree.name}', детей={tree.Children.Count}");

            // Выводим структуру дерева
            logger.LogInformation("Структура дерева:");
            PrintTreeStructure(logger, tree, 0);

            // Тест 2: Получение детей
            logger.LogInformation("");
            logger.LogInformation("Тест 2: Получаем прямых детей корневой категории...");
            var children = await redb.GetChildrenAsync<AnalyticsRecordProps>(rootId);
            logger.LogInformation($"✅ Найдено детей: {children.Count()}");
            foreach (var child in children)
            {
                logger.LogInformation($"   → {child.name} (ID: {child.id})");
            }

            // Тест 3: Путь к корню
            logger.LogInformation("");
            logger.LogInformation("Тест 3: Строим путь от смартфонов к корню...");
            var pathToRoot = await redb.GetPathToRootAsync<AnalyticsRecordProps>(smartphonesId);
            var breadcrumbs = string.Join(" > ", pathToRoot.Select(node => node.name));
            logger.LogInformation($"✅ Хлебные крошки: {breadcrumbs}");

            // Тест 4: Получение всех потомков
            logger.LogInformation("");
            logger.LogInformation("Тест 4: Получаем всех потомков корневой категории...");
            var descendants = await redb.GetDescendantsAsync<AnalyticsRecordProps>(rootId);
            logger.LogInformation($"✅ Найдено потомков: {descendants.Count()}");
            foreach (var descendant in descendants)
            {
                var level = ((redb.Core.Models.ITreeNode<redb.Core.Models.TreeRedbObject<AnalyticsRecordProps>>)descendant).Level;
                var indent = new string(' ', level * 2);
                logger.LogInformation($"   {indent}→ {descendant.name} (уровень {level})");
            }

            // Тест 5: TreeCollection
            logger.LogInformation("");
            logger.LogInformation("Тест 5: Работа с TreeCollection...");
            var collection = new redb.Core.Models.TreeCollection<AnalyticsRecordProps>();

            // Добавляем узлы в коллекцию
            collection.Add(tree);
            foreach (var child in tree.Children)
            {
                collection.Add(child);
                foreach (var grandchild in child.Children)
                {
                    collection.Add(grandchild);
                }
            }

            var stats = collection.GetStats();
            logger.LogInformation($"✅ Статистика TreeCollection: {stats}");

            // Тест 6: Расширения для обхода дерева
            logger.LogInformation("");
            logger.LogInformation("Тест 6: Обход дерева различными способами...");

            logger.LogInformation("Обход в глубину (DFS):");
            foreach (var node in tree.DepthFirstTraversal())
            {
                var level = ((redb.Core.Models.ITreeNode<redb.Core.Models.TreeRedbObject<AnalyticsRecordProps>>)node).Level;
                var indent = new string(' ', level * 2);
                logger.LogInformation($"   {indent}→ {node.name}");
            }

            logger.LogInformation("Обход в ширину (BFS):");
            foreach (var node in tree.BreadthFirstTraversal())
            {
                var level = ((redb.Core.Models.ITreeNode<redb.Core.Models.TreeRedbObject<AnalyticsRecordProps>>)node).Level;
                logger.LogInformation($"   [Уровень {level}] {node.name}");
            }

            // Тест 7: Перемещение узла
            logger.LogInformation("");
            logger.LogInformation("Тест 7: Перемещаем 'Смартфоны' из 'Электроники' в 'Одежду'...");
            await redb.MoveObjectAsync(smartphonesId, clothingId, -9223372036854775800, checkPermissions: false);
            logger.LogInformation("✅ Узел перемещен");

            // Проверяем новую структуру
            var updatedTree = await redb.LoadTreeAsync<AnalyticsRecordProps>(rootId, maxDepth: 5);
            logger.LogInformation("Обновленная структура дерева:");
            PrintTreeStructure(logger, updatedTree, 0);

            // Возвращаем обратно для корректности
            logger.LogInformation("Возвращаем 'Смартфоны' обратно в 'Электронику'...");
            await redb.MoveObjectAsync(smartphonesId, electronicsId, -9223372036854775800, checkPermissions: false);
            logger.LogInformation("✅ Узел возвращен на место");

            logger.LogInformation("");
            logger.LogInformation("🎯 === ДРЕВОВИДНЫЕ СТРУКТУРЫ ПРОТЕСТИРОВАНЫ УСПЕШНО ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при тестировании древовидных структур");
            throw;
        }
    }

    static void PrintTreeStructure(ILogger logger, redb.Core.Models.TreeRedbObject<AnalyticsRecordProps> node, int level)
    {
        var indent = new string(' ', level * 2);
        logger.LogInformation($"{indent}├─ {node.name} (ID: {node.id})");

        foreach (var child in node.Children)
        {
            PrintTreeStructure(logger, child, level + 1);
        }
    }


    static async Task CheckObjectInDatabase(IRedbService redb, long objectId, ILogger logger)
    {
        var redbService = (RedbService)redb;
        var context = redbService.GetType().GetField("_redbContext",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(redbService)
            as redb.Core.Postgres.RedbContext;

        // Получаем базовые поля объекта из _objects
        var objData = await context.Objects
            .Where(o => o.Id == objectId)
            .Select(o => new
            {
                o.Id,
                o.Name,
                o.Note,
                o.IdScheme,
                o.IdOwner,
                o.IdWhoChange,
                o.DateCreate,
                o.DateModify,
                o.Hash,
                o.Bool,
                o.Key,
                o.CodeInt,
                o.CodeString,
                o.CodeGuid
            })
            .FirstOrDefaultAsync();

        if (objData == null)
        {
            logger.LogWarning("Объект {objectId} не найден в _objects", objectId);
            return;
        }

        logger.LogInformation("Объект {id} в _objects:", objData.Id);
        logger.LogInformation("  Name: {name}", objData.Name);
        logger.LogInformation("  Note: {note}", objData.Note);
        logger.LogInformation("  Scheme: {scheme}, Owner: {owner}, WhoChange: {whoChange}",
            objData.IdScheme, objData.IdOwner, objData.IdWhoChange);
        logger.LogInformation("  Created: {created}, Modified: {modified}",
            objData.DateCreate, objData.DateModify);
        logger.LogInformation("  Hash: {hash}", objData.Hash);
        logger.LogInformation("  Bool: {bool}, Key: {key}", objData.Bool, objData.Key);
        logger.LogInformation("  CodeInt: {codeInt}, CodeString: {codeString}, CodeGuid: {codeGuid}",
            objData.CodeInt, objData.CodeString, objData.CodeGuid);

        // Получаем все значения из _values для этого объекта
        var values = await context.Values
            .Where(v => v.IdObject == objectId)
            .Join(context.Structures, v => v.IdStructure, s => s.Id, (v, s) => new
            {
                StructureName = s.Name,
                StructureType = s.TypeNavigation.DbType,
                IsArray = s.IsArray,
                StoreNull = s.StoreNull,
                v.String,
                v.Long,
                v.Guid,
                v.Double,
                v.DateTime,
                v.Boolean,
                v.ByteArray,
                v.Text,
                v.Array
            })
            .ToListAsync();

        logger.LogInformation("Значения в _values ({count} записей):", values.Count);
        foreach (var val in values)
        {
            var actualValue = GetActualValue(val);
            logger.LogInformation("  {name} ({type}{array}): {value}",
                val.StructureName,
                val.StructureType,
                val.IsArray == true ? "[]" : "",
                actualValue ?? "<NULL>");
        }
    }

    // Сравнение нескольких объектов
    static async Task CompareObjectsInDatabase(IRedbService redb, long[] objectIds, ILogger logger)
    {
        var redbService = (RedbService)redb;
        var context = redbService.GetType().GetField("_redbContext",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(redbService)
            as redb.Core.Postgres.RedbContext;

        foreach (var objectId in objectIds)
        {
            logger.LogInformation("--- Объект {id} ---", objectId);

            // Базовые поля
            var obj = await context.Objects.FindAsync(objectId);
            if (obj == null)
            {
                logger.LogWarning("Объект {id} не найден", objectId);
                continue;
            }

            logger.LogInformation("Базовые поля: name='{name}', scheme={scheme}, hash={hash}",
                obj.Name, obj.IdScheme, obj.Hash);

            // Свойства (generic fields)
            var valueCount = await context.Values.CountAsync(v => v.IdObject == objectId);
            var propertyNames = await context.Values
                .Where(v => v.IdObject == objectId)
                .Join(context.Structures, v => v.IdStructure, s => s.Id, (v, s) => s.Name)
                .ToListAsync();

            logger.LogInformation("Дженерик свойства ({count}): {names}",
                valueCount, string.Join(", ", propertyNames));
        }
    }

    // Извлечение актуального значения из записи _values
    static object? GetActualValue(dynamic valueRecord)
    {
        // Проверяем все возможные столбцы и возвращаем не-null значение
        if (valueRecord.String != null) return valueRecord.String;
        if (valueRecord.Long != null) return valueRecord.Long;
        if (valueRecord.Guid != null) return valueRecord.Guid;
        if (valueRecord.Double != null) return valueRecord.Double;
        if (valueRecord.DateTime != null) return valueRecord.DateTime;
        if (valueRecord.Boolean != null) return valueRecord.Boolean;
        if (valueRecord.ByteArray != null) return $"<ByteArray[{((byte[])valueRecord.ByteArray).Length}]>";
        if (valueRecord.Text != null) return valueRecord.Text;
        if (valueRecord.Array != null) return $"<Array: {valueRecord.Array}>";

        return null;
    }
}

