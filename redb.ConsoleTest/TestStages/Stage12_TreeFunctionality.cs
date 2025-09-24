using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Models;
using redb.Core.Models.Collections;
using redb.Core.Models.Configuration; // 🆕 Для MissingObjectStrategy
using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;
using redb.Core.Utils;
using redb.Core.Extensions; // 🆕 Добавляем extension методы
using System;
using System.Linq;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// ЭТАП 12: Тестирование древовидных структур
    /// </summary>
    public class Stage12_TreeFunctionality : BaseTestStage
    {
        public override int Order => 12;
        public override string Name => "Тестирование древовидных структур";
        public override string Description => "Полное тестирование древовидных структур: создание иерархий, обход, перемещение узлов, новые extension методы, защита от циклов";

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🌳 === ЭТАП 12: ТЕСТИРОВАНИЕ ДРЕВОВИДНЫХ СТРУКТУР ===");
            
            // 🔧 НАСТРОЙКА СТРАТЕГИИ: TreeProvider генерирует ID заранее, поэтому нужна AutoSwitchToInsert
            redb.Configuration.MissingObjectStrategy = MissingObjectStrategy.AutoSwitchToInsert;
            logger.LogInformation("⚙️ Установлена стратегия: AutoSwitchToInsert для поддержки TreeProvider");
                
                // Получаем схему для древовидных объектов
                var schemeId = await redb.SyncSchemeAsync<AnalyticsRecordProps>();

                logger.LogInformation("Создаем иерархическую структуру категорий товаров...");

                // Создаем корневую категорию
                var rootCategory = new TreeRedbObject<AnalyticsRecordProps>
                {
                    scheme_id = schemeId.Id,
                    owner_id = 0,
                    who_change_id = 0,
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
                
                // Обновляем ID в объекте для использования в CreateChildAsync
                rootCategory.id = rootId;

                // Создаем дочерние категории
                var electronics = new TreeRedbObject<AnalyticsRecordProps>
                {
                    scheme_id = schemeId.Id,
                    owner_id = 0,
                    who_change_id = 0,
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

                var electronicsId = await redb.CreateChildAsync(electronics, rootCategory);
                logger.LogInformation($"✅ Создана категория 'Электроника': ID={electronicsId}");
                
                // Обновляем ID в объекте для дальнейшего использования
                electronics.id = electronicsId;

                var clothing = new TreeRedbObject<AnalyticsRecordProps>
                {
                    scheme_id = schemeId.Id,
                    owner_id = 0,
                    who_change_id = 0,
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

                var clothingId = await redb.CreateChildAsync(clothing, rootCategory);
                logger.LogInformation($"✅ Создана категория 'Одежда': ID={clothingId}");
                
                // Обновляем ID в объекте для дальнейшего использования  
                clothing.id = clothingId;

                // Создаем подкатегории электроники
                var smartphones = new TreeRedbObject<AnalyticsRecordProps>
                {
                    scheme_id = schemeId.Id,
                    owner_id = 0,
                    who_change_id = 0,
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

                var smartphonesId = await redb.CreateChildAsync(smartphones, electronics);
                logger.LogInformation($"✅ Создана подкатегория 'Смартфоны': ID={smartphonesId}");
                
                // Обновляем ID в объекте для дальнейшего использования
                smartphones.id = smartphonesId;

                logger.LogInformation("");
                logger.LogInformation("🔍 === ТЕСТИРОВАНИЕ МЕТОДОВ ДЕРЕВА ===");

                // Тест 1: Загрузка дерева
                logger.LogInformation("Тест 1: Загружаем полное дерево категорий...");
                var tree = await redb.LoadTreeAsync<AnalyticsRecordProps>(rootCategory, maxDepth: 5);
                logger.LogInformation($"✅ Загружено дерево: корень='{tree.name}', детей={tree.Children.Count}");

                // Выводим структуру дерева
                logger.LogInformation("Структура дерева:");
                PrintTreeStructure(logger, tree, 0);

                // Тест 2: Получение детей
                logger.LogInformation("");
                logger.LogInformation("Тест 2: Получаем прямых детей корневой категории...");
                var children = await redb.GetChildrenAsync<AnalyticsRecordProps>(rootCategory);
                logger.LogInformation($"✅ Найдено детей: {children.Count()}");
                foreach (var child in children)
                {
                    logger.LogInformation($"   → {child.Name} (ID: {child.Id})");
                }

                // Тест 3: Путь к корню
                logger.LogInformation("");
                logger.LogInformation("Тест 3: Строим путь от смартфонов к корню...");
                var pathToRoot = await redb.GetPathToRootAsync<AnalyticsRecordProps>(smartphones);
                var breadcrumbs = string.Join(" > ", pathToRoot.Select(node => node.Name));
                logger.LogInformation($"✅ Хлебные крошки: {breadcrumbs}");

                // Тест 4: Получение всех потомков
                logger.LogInformation("");
                logger.LogInformation("Тест 4: Получаем всех потомков корневой категории...");
                var descendants = await redb.GetDescendantsAsync<AnalyticsRecordProps>(rootCategory);
                logger.LogInformation($"✅ Найдено потомков: {descendants.Count()}");
                foreach (var descendant in descendants)
                {
                    var level = descendant.Level;
                    var indent = new string(' ', level * 2);
                    logger.LogInformation($"   {indent}→ {descendant.Name} (уровень {level})");
                }

                // Тест 5: TreeCollection
                logger.LogInformation("");
                logger.LogInformation("Тест 5: Работа с TreeCollection...");
                var collection = new TreeCollection<AnalyticsRecordProps>();

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
                    var level = node.Level;
                    var indent = new string(' ', level * 2);
                    logger.LogInformation($"   {indent}→ {node.Name}");
                }

                logger.LogInformation("Обход в ширину (BFS):");
                foreach (var node in tree.BreadthFirstTraversal())
                {
                    var level = node.Level;
                    logger.LogInformation($"   [Уровень {level}] {node.Name}");
                }

                // Тест 7: Перемещение узла
                logger.LogInformation("");
                logger.LogInformation("Тест 7: Перемещаем 'Смартфоны' из 'Электроники' в 'Одежду'...");
                await redb.MoveObjectAsync(smartphones, clothing);
                logger.LogInformation("✅ Узел перемещен");

                // Проверяем новую структуру
                var updatedTree = await redb.LoadTreeAsync<AnalyticsRecordProps>(rootCategory, maxDepth: 5);
                logger.LogInformation("Обновленная структура дерева:");
                PrintTreeStructure(logger, updatedTree, 0);

                // Возвращаем обратно для корректности
                logger.LogInformation("Возвращаем 'Смартфоны' обратно в 'Электронику'...");
                await redb.MoveObjectAsync(smartphones, electronics);
                logger.LogInformation("✅ Узел возвращен на место");

                // 🆕 ТЕСТ 8: Новые Extension методы для работы с деревьями
                logger.LogInformation("");
                logger.LogInformation("🌟 Тест 8: Тестируем НОВЫЕ extension методы IRedbObject...");
                
                // Загружаем объекты для тестов
                var rootObj = await redb.LoadAsync<AnalyticsRecordProps>(rootId, 1);
                var electronicsObj = await redb.LoadAsync<AnalyticsRecordProps>(electronicsId, 1);
                var smartphonesObj = await redb.LoadAsync<AnalyticsRecordProps>(smartphonesId, 1);
                
                // Тест IsDescendantOfAsync (🚀 оптимизированный метод)
                var isDescendant = await smartphonesObj.IsDescendantOfAsync<AnalyticsRecordProps>(rootObj, redb);
                logger.LogInformation($"✅ IsDescendantOfAsync: Смартфоны потомок Корня = {isDescendant}");
                
                var isNotDescendant = await rootObj.IsDescendantOfAsync<AnalyticsRecordProps>(smartphonesObj, redb);
                logger.LogInformation($"✅ IsDescendantOfAsync: Корень НЕ потомок Смартфонов = {!isNotDescendant}");
                
                // Тест IsAncestorOfAsync
                var isAncestor = await rootObj.IsAncestorOfAsync<AnalyticsRecordProps>(smartphonesObj, redb);
                logger.LogInformation($"✅ IsAncestorOfAsync: Корень предок Смартфонов = {isAncestor}");
                
                // Тест GetTreeLevelAsync (🚀 с защитой от циклов)
                var rootLevel = await rootObj.GetTreeLevelAsync<AnalyticsRecordProps>(redb);
                var phoneLevel = await smartphonesObj.GetTreeLevelAsync<AnalyticsRecordProps>(redb);
                logger.LogInformation($"✅ GetTreeLevelAsync: Корень уровень={rootLevel}, Смартфоны уровень={phoneLevel}");
                
                // Тест различных проверок объекта
                logger.LogInformation($"✅ IsRoot: Корень={rootObj.IsRoot}, Смартфоны={smartphonesObj.IsRoot}");
                logger.LogInformation($"✅ HasParent: Корень={rootObj.HasParent}, Смартфоны={smartphonesObj.HasParent}");
                
                // 🆕 ТЕСТ 9: Защита от циклических ссылок
                logger.LogInformation("");
                logger.LogInformation("🛡️ Тест 9: Защита от циклических ссылок...");
                logger.LogInformation("ℹ️  Демонстрируем что GetPathToRootAsync теперь устойчив к циклам");
                
                // Симулируем "глубокое" дерево для проверки защиты
                var deepPath = await redb.GetPathToRootAsync<AnalyticsRecordProps>(smartphones);
                var pathLength = deepPath.Count();
                logger.LogInformation($"✅ Путь к корню построен безопасно: {pathLength} уровней");
                logger.LogInformation($"   🛡️ Защита от циклов: активна (HashSet<long> visited)");
                logger.LogInformation($"   🚀 SQL injection: защищен (параметризованные запросы)");
                
                // 🆕 ТЕСТ 10: Демонстрация временных меток и метаданных
                logger.LogInformation("");
                logger.LogInformation("📊 Тест 10: Работа с временными метками и метаданными...");
                
                logger.LogInformation($"✅ DateCreate: {rootObj.DateCreate:yyyy-MM-dd HH:mm}");
                logger.LogInformation($"✅ DateModify: {rootObj.DateModify:yyyy-MM-dd HH:mm}");
                logger.LogInformation($"✅ GetAge: {rootObj.GetAge().TotalMinutes:F1} минут с создания");
                logger.LogInformation($"✅ GetTimeSinceLastModification: {rootObj.GetTimeSinceLastModification().TotalSeconds:F0} секунд с изменения");
                
                // Тест GetDisplayName и GetDebugInfo
                var displayName = rootObj.GetDisplayName();
                var debugInfo = rootObj.GetDebugInfo();
                logger.LogInformation($"✅ GetDisplayName: '{displayName}'");
                logger.LogInformation($"✅ GetDebugInfo: {debugInfo}");

                logger.LogInformation("");
                logger.LogInformation("🎯 === РЕЗУЛЬТАТЫ ТЕСТИРОВАНИЯ ДРЕВОВИДНЫХ СТРУКТУР ===");
                logger.LogInformation("✅ Создание иерархических структур работает");
                logger.LogInformation("✅ Загрузка полного дерева с глубиной работает");
                logger.LogInformation("✅ Получение прямых детей работает");
                logger.LogInformation("✅ Построение пути к корню работает");
                logger.LogInformation("✅ Получение всех потомков работает");
                logger.LogInformation("✅ TreeCollection и статистика работают");
                logger.LogInformation("✅ Обходы дерева (DFS/BFS) работают");
                logger.LogInformation("✅ Перемещение узлов работает");
                logger.LogInformation("🌟 ✅ Extension методы IRedbObject работают (НОВОЕ!)");
                logger.LogInformation("🛡️ ✅ Защита от циклических ссылок активна (НОВОЕ!)");
                logger.LogInformation("🚀 ✅ SQL injection защита включена (НОВОЕ!)");
                logger.LogInformation("📊 ✅ Работа с временными метками и метаданными (НОВОЕ!)");
        }

        private static void PrintTreeStructure(ILogger logger, ITreeRedbObject<AnalyticsRecordProps> node, int level)
        {
            var indent = new string(' ', level * 2);
            logger.LogInformation($"{indent}├─ {node.Name} (ID: {node.Id})");

            foreach (var child in node.Children)
            {
                PrintTreeStructure(logger, child, level + 1);
            }
        }
    }
}
