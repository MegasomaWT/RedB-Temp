using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using redb.Core;
using redb.Core.Models.Configuration;
using redb.Core.Postgres.Extensions;
using System;

namespace redb.Core.Postgres.Examples
{
    /// <summary>
    /// Примеры использования DI extension методов для RedbService
    /// </summary>
    public static class DIExamples
    {
        /// <summary>
        /// Пример 1: Базовая регистрация с конфигурацией по умолчанию
        /// </summary>
        public static void Example1_BasicRegistration(IServiceCollection services)
        {
            // Простейший способ - конфигурация по умолчанию
            services.AddRedbService();
            
            // Использование:
            // var redb = serviceProvider.GetRequiredService<IRedbService>();
        }

        /// <summary>
        /// Пример 2: Регистрация с конфигурацией из appsettings.json
        /// </summary>
        public static void Example2_FromConfiguration(IServiceCollection services, IConfiguration configuration)
        {
            // Загружает конфигурацию из секции "RedbService" в appsettings.json
            services.AddRedbService(configuration);
            
            // Или из кастомной секции
            services.AddRedbService(configuration, "MyRedbSettings");
        }

        /// <summary>
        /// Пример 3: Программная конфигурация через Action
        /// </summary>
        public static void Example3_ProgrammaticConfiguration(IServiceCollection services)
        {
            services.AddRedbService(config =>
            {
                config.DefaultLoadDepth = 5;
                config.DefaultCheckPermissionsOnSave = true;
                config.IdResetStrategy = ObjectIdResetStrategy.AutoResetOnDelete;
                config.MissingObjectStrategy = MissingObjectStrategy.AutoSwitchToInsert;
                config.EnableMetadataCache = false;
            });
        }

        /// <summary>
        /// Пример 4: Конфигурация через Builder (Fluent API)
        /// </summary>
        public static void Example4_FluentConfiguration(IServiceCollection services)
        {
            services.AddRedbService(builder =>
            {
                builder.WithLoadDepth(3)
                       .WithStrictSecurity()
                       .WithMetadataCache(enabled: true, lifetimeMinutes: 60)
                       .WithPrettyJson();
            });
        }

        /// <summary>
        /// Пример 5: Использование предопределенных профилей
        /// </summary>
        public static void Example5_PredefinedProfiles(IServiceCollection services)
        {
            // Профиль для разработки
            services.AddRedbService("Development");
            
            // Профиль для продакшена
            services.AddRedbService("Production");
            
            // Профиль для высокой производительности
            services.AddRedbService("HighPerformance");
        }

        /// <summary>
        /// Пример 6: Комбинированная конфигурация (профиль + дополнительные настройки)
        /// </summary>
        public static void Example6_CombinedConfiguration(IServiceCollection services)
        {
            services.AddRedbService("Production", builder =>
            {
                builder.WithLoadDepth(2)  // Переопределяем глубину загрузки
                       .WithoutCache();   // Отключаем кеш для этого случая
            });
        }

        /// <summary>
        /// Пример 7: Регистрация с валидацией конфигурации
        /// </summary>
        public static void Example7_WithValidation(IServiceCollection services, IConfiguration configuration)
        {
            // С валидацией, но без исключений при ошибках
            services.AddValidatedRedbService(configuration, throwOnValidationError: false);
            
            // С валидацией и исключениями при критических ошибках
            services.AddValidatedRedbService(configuration, throwOnValidationError: true);
        }

        /// <summary>
        /// Пример 8: Hot-reload конфигурации (экспериментальная функция)
        /// </summary>
        public static void Example8_HotReload(IServiceCollection services, IConfiguration configuration)
        {
            // Автоматическое обновление конфигурации при изменении appsettings.json
            services.AddRedbServiceWithHotReload(configuration);
        }

        /// <summary>
        /// Пример 9: Полная настройка для ASP.NET Core приложения
        /// </summary>
        public static void Example9_AspNetCoreSetup()
        {
            // Пример для Program.cs в ASP.NET Core приложении:
            
            /*
            var builder = WebApplication.CreateBuilder();

            // Вариант 1: Простая настройка
            builder.Services.AddRedbService(builder.Configuration);

            // Вариант 2: С выбором профиля в зависимости от окружения
            var profileName = builder.Environment.IsDevelopment() ? "Development" : "Production";
            builder.Services.AddRedbService(profileName);

            // Вариант 3: Комбинированная настройка
            builder.Services.AddRedbService(config =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    config.DefaultLoadDepth = 15;
                    config.EnableMetadataCache = false;
                }
                else
                {
                    config.DefaultLoadDepth = 5;
                    config.EnableMetadataCache = true;
                }
            });

            var app = builder.Build();

            // Использование в контроллере или сервисе:
            // public class MyController : ControllerBase
            // {
            //     private readonly IRedbService _redb;
            //     
            //     public MyController(IRedbService redb)
            //     {
            //         _redb = redb;
            //     }
            // }
            */
        }

        /// <summary>
        /// Пример 10: Настройка для консольного приложения
        /// </summary>
        public static void Example10_ConsoleAppSetup()
        {
            // Для консольного приложения используем ServiceCollection напрямую
            var services = new ServiceCollection();
            
            // Добавляем конфигурацию (требует Microsoft.Extensions.Configuration.Json)
            // var configuration = new ConfigurationBuilder()
            //     .AddJsonFile("appsettings.json", optional: true)
            //     .Build();
            
            // Регистрируем RedbService (если есть конфигурация)
            // services.AddRedbService(configuration);
            
            // Или используем программную конфигурацию
            services.AddRedbService(config =>
            {
                config.DefaultLoadDepth = 15;
                config.IdResetStrategy = ObjectIdResetStrategy.AutoResetOnDelete;
                config.EnableMetadataCache = true;
            });

            var serviceProvider = services.BuildServiceProvider();

            // Использование:
            // var redb = serviceProvider.GetRequiredService<IRedbService>();
            // await redb.SaveAsync(myObject);
        }

        /// <summary>
        /// Пример appsettings.json для конфигурации
        /// </summary>
        public static string ExampleAppSettingsJson => @"
{
  ""RedbService"": {
    ""IdResetStrategy"": ""AutoResetOnDelete"",
    ""MissingObjectStrategy"": ""AutoSwitchToInsert"",
    ""DefaultCheckPermissionsOnLoad"": false,
    ""DefaultCheckPermissionsOnSave"": true,
    ""DefaultCheckPermissionsOnDelete"": true,
    ""DefaultLoadDepth"": 10,
    ""DefaultMaxTreeDepth"": 50,
    ""EnableMetadataCache"": true,
    ""MetadataCacheLifetimeMinutes"": 30,
    ""EnableSchemaValidation"": true,
    ""EnableDataValidation"": true,
    ""AutoSetModifyDate"": true,
    ""AutoRecomputeHash"": true,
    ""DefaultSecurityPriority"": ""SecurityContext"",
    ""SystemUserId"": 0,
    ""JsonOptions"": {
      ""WriteIndented"": false,
      ""UseUnsafeRelaxedJsonEscaping"": true
    }
  }
}";

        /// <summary>
        /// Пример переменных окружения для конфигурации
        /// </summary>
        public static string ExampleEnvironmentVariables => @"
# Переопределение через environment variables
REDBSERVICE__DEFAULTCHECKPERMISSIONSONLOAD=true
REDBSERVICE__DEFAULTLOADDEPTH=5
REDBSERVICE__ENABLESCHEMAMETADATACACHE=false
REDBSERVICE__JSONOPTIONS__WRITEINDENTED=true
REDBSERVICE__IDRESETSTRATEGY=AutoResetOnDelete
REDBSERVICE__MISSINGOBJECTSTRATEGY=AutoSwitchToInsert
";
    }
}
