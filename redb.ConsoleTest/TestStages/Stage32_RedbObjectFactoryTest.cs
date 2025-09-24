using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Models.Entities;
using redb.Core.Models;
using redb.Core.Providers;
using redb.ConsoleTest.TestStages;
using redb.ConsoleTest;
using System.Linq;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// ЭТАП 32: Тест RedbObjectFactory - новая фабрика для создания объектов
    /// Сравнение старого способа создания с новым через фабрику
    /// </summary>
    public class Stage32_RedbObjectFactoryTest : BaseTestStage
    {
        public override string Name => "🏭 Тест RedbObjectFactory";
        public override string Description => "Демонстрация создания объектов через новую фабрику с автоматической инициализацией метаданных";
        public override int Order => 32;

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🏭 === ТЕСТ REDBOBJECTFACTORY ===");
            logger.LogInformation("Сравниваем старый способ создания с новой фабрикой");

            // === ИНИЦИАЛИЗАЦИЯ ФАБРИКИ ===
            logger.LogInformation("🔧 Инициализируем RedbObjectFactory...");
            try
            {
                // RedbService сам реализует ISchemeSyncProvider, передаем его напрямую
                var schemeSyncProvider = redb as ISchemeSyncProvider;
                if (schemeSyncProvider != null)
                {
                    RedbObjectFactory.Initialize(schemeSyncProvider);
                    logger.LogInformation("✅ RedbObjectFactory успешно инициализирован!");
                    
                    var settings = RedbObjectFactory.GetSettings();
                    logger.LogInformation($"📊 Настройки фабрики: Initialized={settings.IsInitialized}, User={settings.CurrentUserName} (ID:{settings.CurrentUserId})");
                }
                else
                {
                    logger.LogWarning("⚠️ RedbService не реализует ISchemeSyncProvider - CreateAsync будет недоступен");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"⚠️ Ошибка инициализации фабрики: {ex.Message}");
            }
            logger.LogInformation("");

            // === ТЕСТ 1: СТАРЫЙ СПОСОБ СОЗДАНИЯ (как было раньше) ===
            logger.LogInformation("📊 ТЕСТ 1: Создание объекта СТАРЫМ способом");
            
            var oldWayObject = new RedbObject<AnalyticsMetricsProps>
            {
                name = "Старый способ создания",
                note = "Ручная инициализация всех полей",
                owner_id = 0,  // Вручную указываем
                who_change_id = 0,  // Вручную указываем
                date_create = DateTime.Now,  // Вручную указываем
                date_modify = DateTime.Now,  // Вручную указываем
                properties = new AnalyticsMetricsProps
                {
                    AdvertId = 1001,
                    Base = 100
                }
            };

            logger.LogInformation("   ✅ Старый объект создан: {Name}", oldWayObject.name);
            logger.LogInformation("   📅 date_create: {DateCreate}", oldWayObject.date_create);
            logger.LogInformation("   👤 owner_id: {OwnerId}", oldWayObject.owner_id);

            // === ТЕСТ 2: НОВЫЙ СПОСОБ ЧЕРЕЗ ФАБРИКУ (БЫСТРОЕ СОЗДАНИЕ) ===
            logger.LogInformation("🏭 ТЕСТ 2: Создание объекта через RedbObjectFactory (быстрое)");

            try
            {
                var factoryObject = RedbObjectFactory.CreateFast<AnalyticsMetricsProps>(
                    properties: new AnalyticsMetricsProps
                    {
                        AdvertId = 2002,
                        Base = 200
                    }
                );

                // Приводим к конкретному типу для полного доступа
                var concreteFactoryObject = (RedbObject<AnalyticsMetricsProps>)factoryObject;
                
                // Дополнительная настройка объекта
                concreteFactoryObject.name = "Объект из фабрики";
                concreteFactoryObject.note = "Автоматическая инициализация через фабрику";

                logger.LogInformation("   ✅ Объект из фабрики создан: {Name}", concreteFactoryObject.name);
                logger.LogInformation("   📅 date_create: {DateCreate}", concreteFactoryObject.date_create);
                logger.LogInformation("   👤 owner_id: {OwnerId} (автоматически из AmbientSecurityContext)", concreteFactoryObject.owner_id);
                logger.LogInformation("   🔧 who_change_id: {WhoChangeId} (автоматически)", concreteFactoryObject.who_change_id);
                logger.LogInformation("   📊 Properties.AdvertId: {AdvertId}", factoryObject.properties.AdvertId);

                SetStageData("FactoryObject", concreteFactoryObject);
            }
            catch (Exception ex)
            {
                logger.LogWarning("⚠️ Быстрое создание недоступно: {Error}", ex.Message);
                logger.LogInformation("   ℹ️ Это нормально, если провайдеры не инициализированы");
            }

            // === ТЕСТ 3: СОЗДАНИЕ С ПОЛНОЙ ИНИЦИАЛИЗАЦИЕЙ ===
            logger.LogInformation("⚡ ТЕСТ 3: Создание с автоматическим поиском схемы");

            try
            {
                if (RedbObjectFactory.IsInitialized)
                {
                    var fullObject = await RedbObjectFactory.CreateAsync<AnalyticsRecordProps>(
                        properties: new AnalyticsRecordProps
                        {
                            Article = "Полная инициализация",
                            Stock = 42,
                            Date = DateTime.Now,
                            stringArr = new[] { "test1", "test2" },
                            longArr = new long[] { 1, 2, 3 }
                        }
                    );

                    // Приводим к конкретному типу для доступа к свойствам
                    var concreteObject = (RedbObject<AnalyticsRecordProps>)fullObject;
                    concreteObject.name = "Объект с полной инициализацией";
                    concreteObject.note = "Создан через CreateAsync с поиском схемы";

                    logger.LogInformation("   ✅ Объект с полной инициализацией создан: {Name}", concreteObject.name);
                    logger.LogInformation("   📋 scheme_id: {SchemeId} (автоматически найден)", concreteObject.scheme_id);
                    logger.LogInformation("   📊 Properties.Article: {Article}", concreteObject.properties.Article);
                }
                else
                {
                    logger.LogInformation("   ℹ️ Фабрика не инициализирована - пропускаем тест CreateAsync");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("⚠️ Создание с полной инициализацией недоступно: {Error}", ex.Message);
            }

            // === ТЕСТ 4: СРАВНЕНИЕ ПРОИЗВОДИТЕЛЬНОСТИ ===
            logger.LogInformation("⏱️ ТЕСТ 4: Сравнение производительности");

            var startTime = DateTime.Now;
            
            // Старый способ - 100 объектов
            var oldWayObjects = new RedbObject<AnalyticsMetricsProps>[100];
            for (int i = 0; i < 100; i++)
            {
                oldWayObjects[i] = new RedbObject<AnalyticsMetricsProps>
                {
                    name = $"Объект {i}",
                    owner_id = 0,
                    who_change_id = 0,
                    date_create = DateTime.Now,
                    date_modify = DateTime.Now,
                    properties = new AnalyticsMetricsProps { AdvertId = i, Base = i * 10 }
                };
            }
            var oldWayTime = DateTime.Now - startTime;

            startTime = DateTime.Now;
            
            // Новый способ - 100 объектов через фабрику
            var factoryObjects = new RedbObject<AnalyticsMetricsProps>[100];
            for (int i = 0; i < 100; i++)
            {
                var factoryObj = RedbObjectFactory.CreateFast<AnalyticsMetricsProps>(
                    properties: new AnalyticsMetricsProps { AdvertId = i + 1000, Base = (i + 1000) * 10 }
                );
                factoryObjects[i] = (RedbObject<AnalyticsMetricsProps>)factoryObj;
                factoryObjects[i].name = $"Фабричный объект {i}";
            }
            var factoryTime = DateTime.Now - startTime;

            logger.LogInformation("   📊 Производительность создания 100 объектов:");
            logger.LogInformation("     🔸 Старый способ: {OldTime:F2} мс", oldWayTime.TotalMilliseconds);
            logger.LogInformation("     🏭 Через фабрику: {FactoryTime:F2} мс", factoryTime.TotalMilliseconds);
            
            if (factoryTime < oldWayTime)
            {
                logger.LogInformation("   🚀 Фабрика быстрее на {Diff:F2} мс!", (oldWayTime - factoryTime).TotalMilliseconds);
            }
            else
            {
                logger.LogInformation("   ⏱️ Фабрика медленнее на {Diff:F2} мс (но дает больше возможностей)", (factoryTime - oldWayTime).TotalMilliseconds);
            }

            // === ТЕСТ 5: ДЕМОНСТРАЦИЯ ТИПОБЕЗОПАСНОСТИ ===
            logger.LogInformation("🔒 ТЕСТ 5: Демонстрация типобезопасности");

            try
            {
                var typeSafeObject = RedbObjectFactory.CreateFast<AnalyticsMetricsProps>(
                    properties: new AnalyticsMetricsProps { AdvertId = 5000, Base = 500 }
                );

                logger.LogInformation("   ✅ Типобезопасный объект создан");
                logger.LogInformation("   📊 Типобезопасный доступ к Properties.AdvertId: {AdvertId}", typeSafeObject.properties.AdvertId);
                logger.LogInformation("   📊 Типобезопасный доступ к Properties.Base: {Base}", typeSafeObject.properties.Base);
                logger.LogInformation("   🔧 ID объекта: {Id}", typeSafeObject.Id);
                
                // Для доступа к базовым полям нужно привести к конкретному типу
                var concreteTypeSafeObject = (RedbObject<AnalyticsMetricsProps>)typeSafeObject;
                logger.LogInformation("   🔧 date_create: {DateCreate}", concreteTypeSafeObject.date_create);
            }
            catch (Exception ex)
            {
                logger.LogWarning("⚠️ Демонстрация типобезопасности недоступна: {Error}", ex.Message);
            }

            // === ВЫВОДЫ ===
            logger.LogInformation("");
            logger.LogInformation("📋 === ВЫВОДЫ ТЕСТИРОВАНИЯ ФАБРИКИ ===");
            logger.LogInformation("✅ Старый способ: Работает, но требует ручной инициализации");
            logger.LogInformation("🏭 Новая фабрика: Автоматизирует создание объектов:");
            logger.LogInformation("   • Автоматически устанавливает owner_id из контекста безопасности");
            logger.LogInformation("   • Автоматически устанавливает who_change_id");
            logger.LogInformation("   • Автоматически находит scheme_id по типу (в CreateAsync)");
            logger.LogInformation("   • Устанавливает корректные даты создания");
            logger.LogInformation("   • Поддерживает быстрое создание без схемы (CreateFast)");
            logger.LogInformation("   • Интегрируется с системой безопасности");
            logger.LogInformation("   • Обеспечивает типобезопасность через IRedbObject<TProps>");
            
            logger.LogInformation("🚀 Рекомендация: Используйте RedbObjectFactory для всех новых объектов!");
            logger.LogInformation("   💡 CreateFast() - для простых случаев без схемы");
            logger.LogInformation("   💡 CreateAsync() - для полной инициализации со схемой");
            logger.LogInformation("   💡 CreateChildAsync() - для создания дочерних объектов");
        }
    }
}