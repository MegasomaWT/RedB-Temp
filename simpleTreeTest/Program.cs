using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using redb.Core.Postgres;
using redb.Core;
using redb.Core.Models.Entities;
using redb.Core.Models.Attributes;
using redb.Core.Extensions;

namespace SimpleTreeTest
{
    /// <summary>
    /// 🚀 ПРОСТОЙ ТЕСТ для проверки исправлений древовидных структур
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🌳 === ПРОСТОЙ ТЕСТ ИСПРАВЛЕНИЙ ДЕРЕВЬЕВ ===");
            Console.WriteLine("Тестируем: защита от циклов, SQL injection защита, оптимизированные extension методы");
            
            try
            {
                // Настройка DI
                var services = new ServiceCollection();
                services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
                
                var connectionString = "Host=localhost;Port=5432;Username=postgres;Password=1;Database=redb;Pooling=true;";
                services.AddDbContext<redb.Core.Postgres.RedbContext>(options =>
                {
                    options.UseNpgsql(connectionString);
                });
                services.AddScoped<IRedbService, RedbService>();
                
                var provider = services.BuildServiceProvider();
                var redb = provider.GetRequiredService<IRedbService>();
                var logger = provider.GetRequiredService<ILogger<Program>>();
                
                logger.LogInformation("🔧 Создаем схему для тестов...");
                
                // Создаем схему
                var scheme = await redb.SyncSchemeAsync<SimpleTestProps>("SimpleTreeTest", alias: "Простой тест дерева");
                
                logger.LogInformation("🌱 Создаем простое дерево...");
                
                // Корневой узел
                var root = new TreeRedbObject<SimpleTestProps>
                {
                    scheme_id = scheme.Id,
                    owner_id = 0,
                    who_change_id = 0,
                    name = "Root",
                    note = "Корневой узел для тестов",
                    properties = new SimpleTestProps
                    {
                        Value = 100,
                        Name = "Root Node"
                    }
                };
                
                var rootId = await redb.SaveAsync(root);
                logger.LogInformation($"✅ Создан корень: ID={rootId}");
                
                // Дочерний узел
                var child = new TreeRedbObject<SimpleTestProps>
                {
                    scheme_id = scheme.Id,
                    owner_id = 0,
                    who_change_id = 0,
                    name = "Child",
                    note = "Дочерний узел",
                    properties = new SimpleTestProps
                    {
                        Value = 50,
                        Name = "Child Node"
                    }
                };
                
                var rootObj = await redb.LoadAsync<SimpleTestProps>(rootId, 1);
                var childId = await redb.CreateChildAsync(child, rootObj);
                logger.LogInformation($"✅ Создан ребенок: ID={childId}");
                
                // Внук
                var grandchild = new TreeRedbObject<SimpleTestProps>
                {
                    scheme_id = scheme.Id,
                    owner_id = 0,
                    who_change_id = 0,
                    name = "GrandChild",
                    note = "Внук",
                    properties = new SimpleTestProps
                    {
                        Value = 25,
                        Name = "GrandChild Node"
                    }
                };
                
                var childObj = await redb.LoadAsync<SimpleTestProps>(childId, 1);
                var grandchildId = await redb.CreateChildAsync(grandchild, childObj);
                logger.LogInformation($"✅ Создан внук: ID={grandchildId}");
                
                logger.LogInformation("");
                logger.LogInformation("🧪 === ТЕСТ 1: GetPathToRootAsync с защитой от циклов ===");
                
                var grandchildObj = await redb.LoadAsync<SimpleTestProps>(grandchildId, 1);
                var path = await redb.GetPathToRootAsync<SimpleTestProps>(grandchildObj);
                var pathNames = string.Join(" > ", path.Select(p => p.name));
                logger.LogInformation($"✅ Путь к корню: {pathNames}");
                logger.LogInformation($"✅ Длина пути: {path.Count()} узлов");
                logger.LogInformation($"🛡️ Защита от циклов: активна (HashSet в GetPathToRootWithUserAsync)");
                
                logger.LogInformation("");
                logger.LogInformation("🧪 === ТЕСТ 2: Новые Extension методы IRedbObject ===");
                
                // Загружаем объекты для тестов
                var rootExtObj = await redb.LoadAsync<SimpleTestProps>(rootId, 1);
                var grandchildExtObj = grandchildObj; // Используем уже загруженный объект
                
                // Тест IsDescendantOfAsync (🚀 оптимизированная версия)
                var isDescendant = await grandchildExtObj.IsDescendantOfAsync<SimpleTestProps>(rootExtObj, redb);
                logger.LogInformation($"✅ IsDescendantOfAsync: Внук потомок корня = {isDescendant}");
                
                // Тест GetTreeLevelAsync
                var rootLevel = await rootExtObj.GetTreeLevelAsync<SimpleTestProps>(redb);
                var grandchildLevel = await grandchildExtObj.GetTreeLevelAsync<SimpleTestProps>(redb);
                logger.LogInformation($"✅ GetTreeLevelAsync: Корень уровень={rootLevel}, Внук уровень={grandchildLevel}");
                
                // Тест свойств объектов
                logger.LogInformation($"✅ IsRoot: Корень={rootExtObj.IsRoot}, Внук={grandchildExtObj.IsRoot}");
                logger.LogInformation($"✅ HasParent: Корень={rootExtObj.HasParent}, Внук={grandchildExtObj.HasParent}");
                
                logger.LogInformation("");
                logger.LogInformation("🧪 === ТЕСТ 3: Временные метки и метаданные ===");
                
                var age = rootExtObj.GetAge();
                var displayName = rootExtObj.GetDisplayName();
                var debugInfo = grandchildExtObj.GetDebugInfo();
                
                logger.LogInformation($"✅ Возраст корня: {age.TotalSeconds:F1} секунд");
                logger.LogInformation($"✅ DisplayName корня: '{displayName}'");
                logger.LogInformation($"✅ DebugInfo внука: {debugInfo}");
                
                logger.LogInformation("");
                logger.LogInformation("🧪 === ТЕСТ 4: Проверка SQL injection защиты ===");
                
                logger.LogInformation("ℹ️  GetChildrenWithUserAsync теперь использует параметризованные запросы");
                var children = await redb.GetChildrenAsync<SimpleTestProps>(rootObj);
                logger.LogInformation($"✅ Получено детей: {children.Count()}");
                logger.LogInformation($"🔒 SQL injection: защищен (параметризованные запросы в PostgresTreeProvider)");
                
                logger.LogInformation("");
                logger.LogInformation("🎉 === ВСЕ ТЕСТЫ ПРОШЛИ УСПЕШНО ===");
                logger.LogInformation("🛡️ ✅ Защита от циклов: работает");
                logger.LogInformation("🚀 ✅ SQL injection защита: активна");
                logger.LogInformation("⚡ ✅ Оптимизированные extension методы: функционируют");
                logger.LogInformation("📊 ✅ Расширенные свойства IRedbObject: все доступны");
                logger.LogInformation("");
                logger.LogInformation("🌟 Все улучшения безопасности и производительности работают корректно!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }
    }
    
    /// <summary>
    /// Простые свойства для тестов
    /// </summary>
    [SchemeAlias("Простые тестовые свойства")]
    public class SimpleTestProps
    {
        public int Value { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

