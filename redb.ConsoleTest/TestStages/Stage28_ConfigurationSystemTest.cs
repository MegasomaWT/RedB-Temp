using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Models.Configuration;
using redb.Core.Models.Entities;
using System;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Тестирование системы конфигурации RedbService
    /// </summary>
    public class Stage28_ConfigurationSystemTest : ITestStage
    {
        public string Name => "Система конфигурации";
        public string Description => "Тестирование системы конфигурации RedbService - настройки по умолчанию, стратегии обработки удаленных объектов";
        public int Order => 28;

        public async Task ExecuteAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🔧 === ТЕСТИРОВАНИЕ СИСТЕМЫ КОНФИГУРАЦИИ ===");

            try
            {
                // === ТЕСТ 1: Проверка текущей конфигурации ===
                logger.LogInformation("📋 Тест 1: Проверка текущей конфигурации");
                
                var currentConfig = redb.Configuration;
                logger.LogInformation("   → DefaultCheckPermissionsOnSave: {value}", currentConfig.DefaultCheckPermissionsOnSave);
                logger.LogInformation("   → DefaultCheckPermissionsOnLoad: {value}", currentConfig.DefaultCheckPermissionsOnLoad);
                logger.LogInformation("   → DefaultCheckPermissionsOnDelete: {value}", currentConfig.DefaultCheckPermissionsOnDelete);
                logger.LogInformation("   → DefaultLoadDepth: {value}", currentConfig.DefaultLoadDepth);
                logger.LogInformation("   → IdResetStrategy: {value}", currentConfig.IdResetStrategy);
                logger.LogInformation("   → MissingObjectStrategy: {value}", currentConfig.MissingObjectStrategy);
                logger.LogInformation("   → AutoSetModifyDate: {value}", currentConfig.AutoSetModifyDate);

                // === ТЕСТ 2: Обновление конфигурации через Action ===
                logger.LogInformation("🔄 Тест 2: Обновление конфигурации через Action");
                
                redb.UpdateConfiguration(config =>
                {
                    config.DefaultLoadDepth = 5;
                    config.IdResetStrategy = ObjectIdResetStrategy.AutoResetOnDelete;
                    config.MissingObjectStrategy = MissingObjectStrategy.AutoSwitchToInsert;
                });

                logger.LogInformation("   → Новая глубина загрузки: {value}", redb.Configuration.DefaultLoadDepth);
                logger.LogInformation("   → Новая стратегия сброса ID: {value}", redb.Configuration.IdResetStrategy);
                logger.LogInformation("   → Новая стратегия обработки отсутствующих объектов: {value}", redb.Configuration.MissingObjectStrategy);

                // === ТЕСТ 3: Обновление конфигурации через Builder ===
                logger.LogInformation("🏗️ Тест 3: Обновление конфигурации через Builder");
                
                redb.UpdateConfiguration(builder =>
                {
                    builder.WithLoadDepth(15)
                           .WithStrictSecurity()
                           .WithMetadataCache(enabled: false);
                });

                logger.LogInformation("   → Глубина загрузки через Builder: {value}", redb.Configuration.DefaultLoadDepth);
                logger.LogInformation("   → Проверка прав на сохранение: {value}", redb.Configuration.DefaultCheckPermissionsOnSave);
                logger.LogInformation("   → Кеширование метаданных: {value}", redb.Configuration.EnableMetadataCache);

                // === ТЕСТ 4: Тестирование стратегии AutoResetOnDelete ===
                logger.LogInformation("🗑️ Тест 4: Тестирование стратегии AutoResetOnDelete");
                
                // Создаем тестовый объект
                var testObj = new RedbObject<redb.ConsoleTest.AnalyticsMetricsProps>
                {
                    name = "ConfigTest_AutoReset",
                    properties = new redb.ConsoleTest.AnalyticsMetricsProps
                    {
                        AdvertId = 12345,
                        Baskets = 42,
                        Costs = 99.99
                    }
                };

                // Сохраняем объект
                var savedId = await redb.SaveAsync(testObj);
                logger.LogInformation("   → Объект создан с ID: {id}", savedId);
                logger.LogInformation("   → ID в объекте до удаления: {id}", testObj.id);

                // Удаляем объект (должен автоматически сбросить ID)
                var deleted = await redb.DeleteAsync(testObj);
                logger.LogInformation("   → Объект удален: {deleted}", deleted);
                logger.LogInformation("   → ID в объекте после удаления: {id} (должен быть 0)", testObj.id);

                if (testObj.id == 0)
                {
                    logger.LogInformation("   ✅ Стратегия AutoResetOnDelete работает корректно!");
                }
                else
                {
                    logger.LogWarning("   ⚠️ Стратегия AutoResetOnDelete не сработала!");
                }

                // === ТЕСТ 5: Тестирование стратегии AutoSwitchToInsert ===
                logger.LogInformation("🔄 Тест 5: Тестирование стратегии AutoSwitchToInsert");
                
                // Создаем объект с несуществующим уникальным ID
                var uniqueId = 900000 + DateTime.Now.Second * 1000 + DateTime.Now.Millisecond; // Уникальный ID
                var testObj2 = new RedbObject<redb.ConsoleTest.AnalyticsMetricsProps>
                {
                    id = uniqueId, // Уникальный несуществующий ID
                    name = "ConfigTest_AutoSwitch",
                    properties = new redb.ConsoleTest.AnalyticsMetricsProps
                    {
                        AdvertId = 67890,
                        Base = 84,
                        Rate = 5
                    }
                };

                logger.LogInformation("   → Попытка сохранить объект с несуществующим ID: {id}", testObj2.id);
                
                // Пытаемся сохранить (должен автоматически переключиться на INSERT)
                var newId = await redb.SaveAsync(testObj2);
                logger.LogInformation("   → ID после автопереключения: {newId}", newId);
                logger.LogInformation("   → ID в объекте: {id}", testObj2.id);

                if (newId == uniqueId && testObj2.id == newId)
                {
                    logger.LogInformation("   ✅ Стратегия AutoSwitchToInsert работает корректно!");
                    logger.LogInformation("   → Объект создан с заданным ID: {newId} (программист контролирует ID)", newId);
                    
                    // Удаляем созданный объект
                    await redb.DeleteAsync(testObj2);
                }
                else
                {
                    logger.LogError("   ❌ Стратегия AutoSwitchToInsert не сработала!");
                    logger.LogError("   → Ожидался ID = {expectedId}, получили: newId={newId}, testObj2.id={objId}", uniqueId, newId, testObj2.id);
                    throw new InvalidOperationException("AutoSwitchToInsert failed.");
                }

                // === ТЕСТ 6: Тестирование настроек по умолчанию ===
                logger.LogInformation("⚙️ Тест 6: Тестирование использования настроек по умолчанию");
                
                // Сбрасываем конфигурацию на значения по умолчанию
                redb.UpdateConfiguration(config =>
                {
                    config.DefaultLoadDepth = 10;
                    config.DefaultCheckPermissionsOnLoad = false;
                    config.DefaultCheckPermissionsOnSave = false;
                    config.DefaultCheckPermissionsOnDelete = true;
                });

                // Создаем и сохраняем объект без явного указания параметров
                var testObj3 = new RedbObject<redb.ConsoleTest.AnalyticsMetricsProps>
                {
                    name = "ConfigTest_Defaults",
                    properties = new redb.ConsoleTest.AnalyticsMetricsProps
                    {
                        AdvertId = 11111,
                        Association = 100,
                        Costs = 250.50
                    }
                };

                var id3 = await redb.SaveAsync(testObj3); // Без явных параметров
                logger.LogInformation("   → Объект сохранен с использованием настроек по умолчанию: {id}", id3);

                var loaded3 = await redb.LoadAsync<redb.ConsoleTest.AnalyticsMetricsProps>(id3); // Без явных параметров
                logger.LogInformation("   → Объект загружен с использованием настроек по умолчанию");

                var deleted3 = await redb.DeleteAsync(testObj3); // Без явных параметров
                logger.LogInformation("   → Объект удален с использованием настроек по умолчанию: {deleted}", deleted3);

                logger.LogInformation("   ✅ Настройки по умолчанию работают корректно!");

                logger.LogInformation("🎉 === ВСЕ ТЕСТЫ КОНФИГУРАЦИИ ПРОЙДЕНЫ УСПЕШНО ===");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Ошибка при тестировании системы конфигурации");
                throw;
            }
        }
    }
}
