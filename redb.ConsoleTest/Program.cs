using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using redb.Core.Postgres;
using redb.Core;
using redb.Core.Models.Entities;
using redb.Core.Models.Configuration;
using redb.Core.Providers;
using redb.Core.Postgres.Providers;
using redb.Core.Models.Security;
using redb.Core.Serialization;
using redb.ConsoleTest.TestStages;
using redb.ConsoleTest.Models;
using redb.ConsoleTest;

internal class Program
    {
        static async Task Main(string[] args)
        {
            // Настройка DI контейнера
            var services = new ServiceCollection();
            ConfigureServices(services);
            
            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("ConsoleTest");

            try
            {
                // ✅ НАСТРАИВАЕМ ГЛОБАЛЬНЫЕ НАСТРОЙКИ REDB
                ConfigureRedbGlobalSettings(provider, logger);

                var redb = provider.GetRequiredService<IRedbService>();
                var stageManager = new TestStageManager();

                // Парсинг аргументов командной строки
                if (args.Length == 0)
                {
                    // Выполняем все этапы
                    await stageManager.ExecuteAllStagesAsync(logger, redb);
                }
                else if (args.Contains("--help") || args.Contains("-h"))
                {
                    ShowHelp(logger);
                }
                else if (args.Contains("--list") || args.Contains("-l"))
                {
                    stageManager.ShowAvailableStages(logger);
                }
                else if (args.Contains("--demo"))
                {
                    // Демонстрация механизма Reflection Change Tracking
                    // TestReflectionMechanism.DemoReflectionTracking(); // TODO: Реализовать класс TestReflectionMechanism
                    logger.LogWarning("⚠️ Функция демонстрации Reflection пока не реализована");
                }
                else if (args.Contains("--stages") || args.Contains("-s"))
                {
                    // Выполняем выбранные этапы
                    var stagesArg = GetArgumentValue(args, "--stages") ?? GetArgumentValue(args, "-s");
                    if (string.IsNullOrEmpty(stagesArg))
                    {
                        logger.LogError("❌ Не указаны номера этапов. Используйте: --stages 1,3,13");
                        return;
                    }

                    var stageNumbers = ParseStageNumbers(stagesArg);
                    if (stageNumbers.Any())
                    {
                        await stageManager.ExecuteStagesAsync(logger, redb, stageNumbers);
                    }
                    else
                    {
                        logger.LogError("❌ Неверный формат номеров этапов: {stages}", stagesArg);
                    }
                }
                else
                {
                    logger.LogError("❌ Неизвестные аргументы. Используйте --help для справки");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "💥 Критическая ошибка в консольном тесте");
                Console.WriteLine(ex);
            }
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            // Опция для детального логирования (для отладки)
            bool detailedLogging = false; // 🔍 ВКЛЮЧАЕМ детальное логирование EF для диагностики ChangeTracking!

            // Логирование
            services.AddLogging(b =>
            {
                b.ClearProviders(); // Убираем все провайдеры по умолчанию
                b.AddProvider(new SimpleConsoleLoggerProvider()) // Наш кастомный логгер без префиксов
                 .SetMinimumLevel(LogLevel.Information);

                if (!detailedLogging)
                {
                    // Полностью отключаем все логи EF Core для обычного пользователя
                    b.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None); // Все EF Core логи
                    b.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None); // SQL запросы
                    b.AddFilter("Microsoft.EntityFrameworkCore.Query", LogLevel.None); // Предупреждения о запросах
                    b.AddFilter("Microsoft.EntityFrameworkCore.Update", LogLevel.None); // Операции обновления
                    b.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.None); // Инфраструктурные логи
                    b.AddFilter("Microsoft.EntityFrameworkCore.Model", LogLevel.None); // Модель
                    b.AddFilter("Microsoft.EntityFrameworkCore.Migrations", LogLevel.None); // Миграции
                }
            });

            // Подключение к PostgreSQL (настройте строку под вашу среду)
            var connectionString = "Host=localhost;Port=5432;Username=postgres;Password=rjkmwjnmvs;Database=redb;Pooling=true;";

            services.AddDbContext<redb.Core.Postgres.RedbContext>(options =>
            {
                options.UseNpgsql(connectionString);
                
                // Полностью отключаем логирование EF Core на уровне DbContext
                if (!detailedLogging)
                {
                    options.UseLoggerFactory(LoggerFactory.Create(builder => { })); // Пустой логгер
                    options.EnableSensitiveDataLogging(false); // Отключаем чувствительные данные
                    options.EnableDetailedErrors(false); // Отключаем детальные ошибки
                }
                else
                {
                    options.EnableSensitiveDataLogging(true); // Включаем для отладки
                    options.EnableDetailedErrors(true); // Включаем детальные ошибки для отладки
                    
                    // 🔍 ЛОГИРОВАНИЕ SQL КОМАНД В КОНСОЛЬ для диагностики
                    options.LogTo(Console.WriteLine, LogLevel.Information);
                }
            });

            services.AddScoped<IRedbService, RedbService>();

            // ✅ Регистрируем провайдеры для REDB
            services.AddScoped<ISchemeSyncProvider, PostgresSchemeSyncProvider>();
            services.AddScoped<IObjectStorageProvider, PostgresObjectStorageProvider>();
            services.AddScoped<IUserProvider, PostgresUserProvider>();
            services.AddScoped<IPermissionProvider, PostgresPermissionProvider>();
            services.AddScoped<IRedbObjectSerializer, SystemTextJsonRedbSerializer>();
            
            // Добавляем конфигурацию
                    services.AddSingleton(new RedbServiceConfiguration
        {
            EavSaveStrategy = EavSaveStrategy.DeleteInsert  // 🔄 ChangeTracking для продакшна (медленнее, но точнее)
        });
        }

        private static void ConfigureRedbGlobalSettings(ServiceProvider provider, ILogger logger)
        {
            logger.LogInformation("🔧 Настраиваем REDB с новой парадигмой сохранения...");

            // 🚀 Получаем конфигурацию из DI контейнера
            var configuration = provider.GetRequiredService<RedbServiceConfiguration>();
            RedbObject<ProductTestProps>.SetConfiguration(configuration);

            // Устанавливаем схемный провайдер для метаданных
            var schemeProvider = provider.GetRequiredService<ISchemeSyncProvider>();
            RedbObject.SetSchemeSyncProvider(schemeProvider);

            logger.LogInformation("✅ НОВАЯ ПАРАДИГМА ВКЛЮЧЕНА!");
            logger.LogInformation($"✅ EAV Strategy: {configuration.EavSaveStrategy} (реляционные массивы, UUID хеши, NULL семантика)");
            logger.LogInformation("✅ Функции: SaveArrayFieldAsync, SaveClassFieldAsync, SaveSimpleFieldAsync");
        }

        private static string? GetArgumentValue(string[] args, string argumentName)
        {
            var index = Array.IndexOf(args, argumentName);
            if (index >= 0 && index + 1 < args.Length)
            {
                return args[index + 1];
            }
            return null;
        }

        private static int[] ParseStageNumbers(string stagesArg)
        {
            try
            {
                return stagesArg.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.Parse(s.Trim()))
                    .ToArray();
            }
            catch
            {
                return Array.Empty<int>();
            }
        }

        private static void ShowHelp(ILogger logger)
        {
            logger.LogInformation("🚀 === REDB CONSOLE TEST - СПРАВКА ===");
            logger.LogInformation("");
            logger.LogInformation("Консольное приложение для тестирования функциональности REDB");
            logger.LogInformation("Полная документация: TESTING_GUIDE.md");
            logger.LogInformation("");
            logger.LogInformation("📋 Использование:");
            logger.LogInformation("  dotnet run                    - выполнить все этапы тестирования");
            logger.LogInformation("  dotnet run --stages 1,3,13    - выполнить только этапы 1, 3 и 13");
            logger.LogInformation("  dotnet run --list             - показать список доступных этапов");
            logger.LogInformation("  dotnet run --help             - показать эту справку");
            logger.LogInformation("");
            logger.LogInformation("🔧 Сокращенные формы:");
            logger.LogInformation("  -s вместо --stages");
            logger.LogInformation("  -l вместо --list");
            logger.LogInformation("  -h вместо --help");
            logger.LogInformation("");
            logger.LogInformation("🎯 Основные этапы:");
            logger.LogInformation("  1  - Подключение к базе данных");
            logger.LogInformation("  2  - Загрузка существующего объекта");
            logger.LogInformation("  3  - Code-First синхронизация схемы");
            logger.LogInformation("  4  - Демонстрация опциональных проверок прав");
            logger.LogInformation("  5  - Создание нового объекта");
            logger.LogInformation("  6  - Проверка созданного объекта");
            logger.LogInformation("  7  - Обновление объекта");
            logger.LogInformation("  8  - Финальная проверка");
            logger.LogInformation("  9  - Анализ данных в базе");
            logger.LogInformation("  10 - Сравнительный анализ");
            logger.LogInformation("  11 - Удаление объектов");
            logger.LogInformation("  12 - Тестирование древовидных структур");
            logger.LogInformation("  13 - Основные LINQ-запросы (Where, Count)");
            logger.LogInformation("  16 - Расширенные LINQ (Any, WhereIn)");
            logger.LogInformation("  17 - Дополнительные LINQ (All, Select, Distinct)");
            logger.LogInformation("  18 - Сортировка и пагинация");
            logger.LogInformation("  19 - Сортировка по датам");
            logger.LogInformation("  30 - 🆕 Улучшения безопасности деревьев");
            logger.LogInformation("  41 - 🚀 LINQ Новая Парадигма (nullable, тернарные, StringComparison)");
            logger.LogInformation("");
            logger.LogInformation("🔬 Переменные окружения:");
            logger.LogInformation("  DETAILED_LOGGING=true         - включить детальное логирование EF Core");
            logger.LogInformation("");
            logger.LogInformation("💡 Примеры:");
            logger.LogInformation("  dotnet run --stages 1,2,3     - базовые функции");
            logger.LogInformation("  dotnet run --stages 4,5,6,7,8 - CRUD операции");
            logger.LogInformation("  dotnet run --stages 9,10,11   - анализ БД и удаление");
            logger.LogInformation("  dotnet run --stages 12        - древовидные структуры");
            logger.LogInformation("  dotnet run --stages 13,16,17  - все LINQ тесты");
            logger.LogInformation("  dotnet run --stages 18,19     - сортировка и пагинация");
            logger.LogInformation("  dotnet run --stages 12,30     - полное тестирование деревьев");
            logger.LogInformation("  dotnet run --stages 30        - только новые улучшения");
            logger.LogInformation("");
            logger.LogInformation("⚠️  Требования:");
            logger.LogInformation("  - PostgreSQL база данных запущена");
            logger.LogInformation("  - Схема REDB создана (redbPostgre.sql)");
            logger.LogInformation("  - Строка подключения настроена");
        }
    }
