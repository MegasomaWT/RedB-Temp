using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Models.Entities;
using System;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// ЭТАП 5: Создание нового объекта
    /// </summary>
    public class Stage05_CreateObject : BaseTestStage
    {
        public override int Order => 5;
        public override string Name => "Создание нового объекта";
        public override string Description => "Создаем новый объект AnalyticsRecord с демонстрацией структуры и сохранением";

        public long CreatedObjectId { get; private set; }

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("➕ === ЭТАП 5: СОЗДАНИЕ НОВОГО ОБЪЕКТА ===");

            // Получаем схему из предыдущего этапа
            //var schMetName = "TrueSight.DBModels.AnalyticsMetrics";
            //var schemeName = "TrueSight.DBModels.AnalyticsRecord";

            // 🚀 ПОЛНОСТЬЮ АВТОМАТИЧЕСКАЯ СХЕМА: Никаких ручных вызовов SyncSchemeAsync!
            // При сохранении объекта схема создается автоматически:
            // - Имя схемы = имя класса (AnalyticsRecordProps, AnalyticsMetricsProps)
            // - Алиас берется из атрибута [RedbScheme("...")]
            // - Структуры синхронизируются по свойствам класса
            logger.LogInformation("Схемы создаются ПОЛНОСТЬЮ АВТОМАТИЧЕСКИ при сохранении объектов!");

            var metObj = new RedbObject<AnalyticsMetricsProps>
            {
                //scheme_id = metId,
                name = "Вложенный объект AutoMetrics",
                note = "Полностью заполненный тестовый объект для демонстрации",
                owner_id = 0,
                who_change_id = 0,
                date_create = DateTime.Now,
                date_modify = DateTime.Now,
                properties = new AnalyticsMetricsProps
                {
                    AdvertId = 12312,
                    Base = 1500,
                    Baskets = 45,
                    Association = 12,
                    Costs = 2500.75,
                    Rate = 95
                }
            };

            // Создаем массив объектов для тестирования AutoMetricsArray
            logger.LogInformation("Создаем массив объектов для AutoMetricsArray...");
            var metricsArray = new RedbObject<AnalyticsMetricsProps>[]
            {
                new RedbObject<AnalyticsMetricsProps>
                {
                    name = "Метрика 1 - Реклама ULTRA",
                    note = "Данные по рекламной кампании #1",
                    owner_id = 0,
                    who_change_id = 0,
                    date_create = DateTime.Now,
                    date_modify = DateTime.Now,
                    properties = new AnalyticsMetricsProps
                    {
                        AdvertId = 10001,
                        Base = 150,
                        Baskets = 25,
                        Association = 5,
                        Costs = 1250.50,
                        Rate = 85
                    }
                },
                new RedbObject<AnalyticsMetricsProps>
                {
                    name = "Метрика 2 - Органика",
                    note = "Органический трафик",
                    owner_id = 0,
                    who_change_id = 0,
                    date_create = DateTime.Now,
                    date_modify = DateTime.Now,
                    properties = new AnalyticsMetricsProps
                    {
                        AdvertId = 10002,
                        Base = 300,
                        Baskets = 45,
                        Association = 12,
                        Costs = 0.0, // Органика без затрат
                        Rate = 92
                    }
                },
                new RedbObject<AnalyticsMetricsProps>
                {
                    name = "Метрика 3 - Социальные сети",
                    note = "Трафик из социальных сетей",
                    owner_id = 0,
                    who_change_id = 0,
                    date_create = DateTime.Now,
                    date_modify = DateTime.Now,
                    properties = new AnalyticsMetricsProps
                    {
                        AdvertId = 10003,
                        Base = 75,
                        Baskets = 8,
                        Association = 2,
                        Costs = 450.25,
                        Rate = 65
                    }
                }
            };

            logger.LogInformation("Создано {count} объектов в массиве AutoMetricsArray:", metricsArray.Length);
            for (int i = 0; i < metricsArray.Length; i++)
            {
                var metric = metricsArray[i];
                logger.LogInformation("   [{index}] {name} - AdvertId: {advertId}, Base: {baseValue}, Costs: {costs}",
                    i, metric.name, metric.properties.AdvertId, metric.properties.Base, metric.properties.Costs);
            }

            logger.LogInformation("🔧 === СОЗДАНИЕ СМЕШАННОГО ТЕСТОВОГО ОБЪЕКТА ===");
            // ✅ СМЕШАННЫЙ ТЕСТОВЫЙ ОБЪЕКТ для новой парадигмы
            var newObj = new RedbObject<MixedTestProps>
            {
                // scheme_id = 0 по умолчанию → автоматически определится как "MixedTestProps"
                name = "Смешанный тестовый объект",
                note = "Тест новой парадигмы с бизнес-классами",
                owner_id = 0,
                who_change_id = 0,
                date_create = DateTime.Now,
                date_modify = DateTime.Now,
                properties = new MixedTestProps
                {
                    // ✅ Простые типы
                    Age = 30,
                    Name = "John Doe",
                    Date = DateTime.Today,
                    Article = "Тестовый артикул",
                    Stock = 100,
                    Tag = "mixed-test",
                    TestName = "МАКСИМАЛЬНЫЙ ТЕСТ STAGE 5", // ✅ ЗАПОЛНЯЕМ TestName!

                    // ✅ Простые массивы (новая парадигма: реляционное хранение) - МАКСИМАЛЬНО ЗАПОЛНЕННЫЕ!
                    Tags1 = new string[] { "developer", "senior", "fullstack", "expert", "architect", "lead" },
                    Scores1 = new int[] { 85, 92, 78, 96, 88, 94 },
                    Tags2 = new string[] { "JJJdeveloper", "!!!senior", "_____fullstack", "###expert", "@@@architect" },
                    Scores2 = new int[] { 33, 22, 11, 44, 55 },

                    // ✅ Бизнес класс (новая парадигма: UUID хеш + вложенные свойства)
                    Address1 = new Address
                    {
                        City = "Moscow",
                        Street = "Main Street 123",
                        Details = new Details
                        {
                            Floor = 5,
                            Building = "Building A",
                            // ✅ ЗАПОЛНЯЕМ МАКСИМАЛЬНО - массивы в Details!
                            Tags1 = new string[] { "moscow", "main-street", "building-a" },
                            Scores1 = new int[] { 95, 87, 92 },
                            Tags2 = new string[] { "addr1", "premium", "center" },
                            Scores2 = new int[] { 88, 91, 89 }
                        }
                    },
                    Address2 = new Address
                    {
                        City = "Moscow",
                        Street = "Main Street 123",
                        Details = new Details
                        {
                            Floor = 15,
                            Building = "Building B Advanced",
                            // ✅ МАКСИМАЛЬНО ЗАПОЛНЕННЫЕ массивы для Address2!
                            Tags1 = new string[] { "address2", "advanced", "building-b", "premium", "moscow-center" },
                            Scores1 = new int[] { 98, 97, 96, 95, 94 },
                            Tags2 = new string[] { "ultra", "mega", "super", "advanced", "final" },
                            Scores2 = new int[] { 100, 99, 98, 97, 96 },
                        }
                    },
                    // ✅ Массив бизнес классов (новая парадигма: базовая запись + элементы с ArrayParentId)
                    Contacts = new Contact[]
                    {
                            new Contact { Type = "email", Value = "john@example.com", Verified = true },
                            new Contact { Type = "phone", Value = "+7-999-123-45-67", Verified = false },
                            new Contact { Type = "telegram", Value = "@john_doe_test", Verified = true },
                            new Contact { Type = "skype", Value = "john.doe.business", Verified = false },
                            new Contact { Type = "whatsapp", Value = "+7-999-555-77-88", Verified = true }
                    },

                    // ✅ RedbObject ссылки (работает как раньше - ID в Long поле)
                    AutoMetrics = metObj,
                    RelatedMetrics = metricsArray
                }
            };

            logger.LogInformation("✅ === СТРУКТУРА СМЕШАННОГО ОБЪЕКТА ===");
            logger.LogInformation("   Базовые поля: name='{name}', note='{note}', scheme_id={schemeId} (автоопределение)",
                newObj.name, newObj.note, newObj.scheme_id);
            logger.LogInformation("   Properties (будут сохранены в _values):");

            // ✅ Простые типы
            logger.LogInformation("     🔢 Age: {age}", newObj.properties.Age);
            logger.LogInformation("     📝 Name: '{name}'", newObj.properties.Name);
            logger.LogInformation("     📅 Date: {date}", newObj.properties.Date);
            logger.LogInformation("     📦 Article: '{article}'", newObj.properties.Article);
            logger.LogInformation("     📊 Stock: {stock}", newObj.properties.Stock);
            logger.LogInformation("     🏷️ Tag: '{tag}'", newObj.properties.Tag);

            // ✅ Простые массивы (новая парадигма)
            logger.LogInformation("     📋 Tags[]: [{tags}] ({tagCount} элементов)",
                string.Join(", ", newObj.properties.Tags1 ?? Array.Empty<string>()),
                newObj.properties.Tags1?.Length ?? 0);
            logger.LogInformation("     🎯 Scores[]: [{scores}] ({scoreCount} элементов)",
                string.Join(", ", newObj.properties.Scores2 ?? Array.Empty<int>()),
                newObj.properties.Scores2?.Length ?? 0);

            // ✅ Бизнес класс (новая парадигма)
            logger.LogInformation("     🏠 Address: {city}, {street} (Floor: {floor}, Building: {building})",
                newObj.properties.Address1.City, newObj.properties.Address2.Street,
                newObj.properties.Address1.Details.Floor, newObj.properties.Address2.Details.Building);

            // ✅ Массив бизнес классов (новая парадигма)
            logger.LogInformation("     📞 Contacts[]: {contactCount} контактов", newObj.properties.Contacts?.Length ?? 0);
            if (newObj.properties.Contacts != null)
            {
                for (int i = 0; i < newObj.properties.Contacts.Length; i++)
                {
                    var contact = newObj.properties.Contacts[i];
                    logger.LogInformation("       [{index}] {type}: {value} (Verified: {verified})",
                        i, contact.Type, contact.Value, contact.Verified);
                }
            }
            
            // ✅ ДЕТАЛИЗАЦИЯ Address1.Details и Address2.Details
            logger.LogInformation("     🏠 Address1.Details: Floor={floor}, Building={building}",
                newObj.properties.Address1.Details.Floor, newObj.properties.Address1.Details.Building);
            logger.LogInformation("       📋 Address1.Details.Tags1: [{tags}] ({count} элементов)",
                string.Join(", ", newObj.properties.Address1.Details.Tags1), newObj.properties.Address1.Details.Tags1.Length);
            logger.LogInformation("       🎯 Address1.Details.Scores1: [{scores}] ({count} элементов)",
                string.Join(", ", newObj.properties.Address1.Details.Scores1), newObj.properties.Address1.Details.Scores1.Length);
                
            logger.LogInformation("     🏠 Address2.Details: Floor={floor}, Building={building}",
                newObj.properties.Address2.Details.Floor, newObj.properties.Address2.Details.Building);
            logger.LogInformation("       📋 Address2.Details.Tags1: [{tags}] ({count} элементов)",
                string.Join(", ", newObj.properties.Address2.Details.Tags1), newObj.properties.Address2.Details.Tags1.Length);
            logger.LogInformation("       🎯 Address2.Details.Scores1: [{scores}] ({count} элементов)",
                string.Join(", ", newObj.properties.Address2.Details.Scores1), newObj.properties.Address2.Details.Scores1.Length);

            // ✅ RedbObject ссылки
            logger.LogInformation("     🔗 AutoMetrics: '{autoMetricsName}' (RedbObject ссылка)", newObj.properties.AutoMetrics?.name);
            logger.LogInformation("     🔗 RelatedMetrics[]: {arrayCount} RedbObject ссылок", newObj.properties.RelatedMetrics?.Length ?? 0);

            logger.LogInformation("🚀 === СОХРАНЕНИЕ СМЕШАННОГО ОБЪЕКТА ===");
            logger.LogInformation("Сохраняем объект БЕЗ проверки прав (checkPermissions=false - по умолчанию)...");
            logger.LogInformation("   → Автоматическое создание схем 'MixedTestProps' и 'AnalyticsMetricsProps'");
            logger.LogInformation("   → INSERT в _objects (базовые поля)");
            logger.LogInformation("   → INSERT в _values (новая парадигма):");
            logger.LogInformation("     • Простые типы: Age, Name, Date, Article, Stock, Tag");
            logger.LogInformation("     • Простые массивы: Tags[], Scores[] (реляционное хранение)");
            logger.LogInformation("     • Бизнес класс: Address (UUID хеш + вложенные свойства)");
            logger.LogInformation("     • Массив бизнес классов: Contacts[] (базовая запись + элементы)");
            logger.LogInformation("     • RedbObject ссылки: AutoMetrics, RelatedMetrics[]");
            logger.LogInformation("   → Автоматическое сохранение 4 вложенных объектов (1 + 3 в массиве)");
            logger.LogInformation("   → Автоматический расчет MD5 хеша");

            // 🔬 ДИАГНОСТИКА NULL → DEFAULT ПЕРЕД СОХРАНЕНИЕМ
            logger.LogInformation("🔬 === АНАЛИЗ NULL ПОЛЕЙ ПЕРЕД СОХРАНЕНИЕМ ===");
            logger.LogInformation($"   📊 Stock (non-nullable): {newObj.properties.Stock}");
            logger.LogInformation($"   📊 Tag (nullable): {newObj.properties.Tag ?? "NULL"}");
            logger.LogInformation($"   📊 TestName (nullable): {newObj.properties.TestName ?? "NULL"}");
            if (newObj.properties.AutoMetrics?.properties != null)
            {
                logger.LogInformation($"   📊 AutoMetrics.Baskets (nullable): {newObj.properties.AutoMetrics.properties.Baskets?.ToString() ?? "NULL"}");
                logger.LogInformation($"   📊 AutoMetrics.Costs (nullable): {newObj.properties.AutoMetrics.properties.Costs?.ToString() ?? "NULL"}");
            }
            
            // 🔬 АНАЛИЗ БИЗНЕС-КЛАССОВ
            logger.LogInformation("🔬 АНАЛИЗ БИЗНЕС-КЛАССОВ (Address):");
            logger.LogInformation($"   📊 Address1 (не nullable): {(newObj.properties.Address1 == null ? "NULL" : $"Filled: {newObj.properties.Address1.City}")}");
            logger.LogInformation($"   📊 Address2 (не nullable): {(newObj.properties.Address2 == null ? "NULL" : $"Filled: {newObj.properties.Address2.City}")}");
            logger.LogInformation($"   📊 Address3 (nullable): {(newObj.properties.Address3 == null ? "NULL" : $"Filled: {newObj.properties.Address3.City}")}");

            CreatedObjectId = await redb.SaveAsync(newObj); // Сохраняем объект
            
            logger.LogInformation("✅ Объект создан с ID: {newId}", CreatedObjectId);

            // 🧪 ТЕСТ ИСПРАВЛЕННОЙ ЛОГИКИ ИЗМЕНЕНИЯ МАССИВА
            logger.LogInformation("🧪 === ТЕСТИРУЕМ ИСПРАВЛЕННУЮ ArrayParentId ЛОГИКУ ===");
            var testArrayMod = await redb.LoadAsync<MixedTestProps>(CreatedObjectId);
            logger.LogInformation("✅ Объект загружен, изменяем Contacts[0].Type");
            testArrayMod.properties.Contacts[0].Type = "test_fixed";
            logger.LogInformation("🚀 Сохраняем с изменением массива...");
            await redb.SaveAsync(testArrayMod);
            logger.LogInformation("✅ ИЗМЕНЕНИЕ МАССИВА УСПЕШНО!");
            
            // 🔍 ПРОВЕРЯЕМ ЧТО ИЗМЕНИЛОСЬ В БД
            logger.LogInformation("🔍 === ПРОВЕРЯЕМ ИЗМЕНЕНИЯ В БАЗЕ ДАННЫХ ===");
            var changedObj = await redb.LoadAsync<MixedTestProps>(CreatedObjectId);
            logger.LogInformation("📞 Измененные контакты:");
            for (int i = 0; i < changedObj.properties.Contacts.Length; i++)
            {
                var contact = changedObj.properties.Contacts[i];
                string indicator = i == 0 ? "🔥 [ИЗМЕНЕН]" : "   [без изменений]";
                logger.LogInformation("  {indicator} [{index}] Type: '{type}', Value: '{value}', Verified: {verified}",
                    indicator, i, contact.Type, contact.Value, contact.Verified);
            }
            // 🔬 ДОПОЛНИТЕЛЬНЫЙ ТЕСТ: ОБЪЕКТ С ЯВНЫМИ NULL ПОЛЯМИ
            logger.LogInformation("🧪 === СОЗДАЕМ ТЕСТОВЫЙ ОБЪЕКТ С NULL ПОЛЯМИ ===");
            var nullTestObj = new RedbObject<AnalyticsRecordProps>
            {
                name = "NULL Test Object",
                properties = new AnalyticsRecordProps
                {
                    Date = DateTime.Today,
                    Article = "NULL-TEST-001",
                    Stock = 100,              // ✅ Non-nullable: заполнено
                    Orders = null,            // ❌ Nullable: остается null
                    TotalCart = null,         // ❌ Nullable: остается null  
                    Tag = null,               // ❌ Nullable: остается null
                    TestName = "Filled Name", // ✅ Nullable но заполнено
                    stringArr = new string[] { "test" },
                    longArr = new long[] { 1, 2, 3 },
                    AuctionMetrics = null  // ✅ NULL RedbObject ссылка!
                }
            };
            
            logger.LogInformation("📝 СОЗДАЕМ ОБЪЕКТ С NULL:");
            logger.LogInformation($"   📊 Orders: {nullTestObj.properties.Orders?.ToString() ?? "NULL"}");
            logger.LogInformation($"   📊 TotalCart: {nullTestObj.properties.TotalCart?.ToString() ?? "NULL"}");
            logger.LogInformation($"   📊 Tag: {nullTestObj.properties.Tag ?? "NULL"}");
            logger.LogInformation($"   📊 TestName: {nullTestObj.properties.TestName ?? "NULL"}");
            logger.LogInformation($"   📊 Stock: {nullTestObj.properties.Stock}");
            logger.LogInformation($"   📊 AuctionMetrics (RedbObject ссылка): {(nullTestObj.properties.AuctionMetrics == null ? "NULL" : "NOT NULL")}");
            
            var nullTestId = await redb.SaveAsync(nullTestObj);
            SetStageData("NullTestObjectId", nullTestId);
            logger.LogInformation("✅ NULL Test объект создан с ID: {nullTestId}", nullTestId);

            // ✅ Сохраняем ID для использования в следующих стадиях
            SetStageData("CreatedObjectId", CreatedObjectId);
            SetStageData("UpdatedObjectId", CreatedObjectId);  // Для совместимости со стадией 9

            // 🔍 ПРОВЕРКА УСТАНОВКИ ParentId для вложенных объектов
            logger.LogInformation("🔍 === ПРОВЕРКА ParentId ДЛЯ ВЛОЖЕННЫХ ОБЪЕКТОВ ===");

            // Проверяем основной объект
            logger.LogInformation("📋 Основной объект:");
            logger.LogInformation("   ID: {mainId}, ParentId: {mainParentId}", newObj.Id, newObj.ParentId);

            // Проверяем одиночный вложенный объект
            logger.LogInformation("📋 Одиночный вложенный объект (AutoMetrics):");
            logger.LogInformation("   ID: {nestedId}, ParentId: {nestedParentId}, Name: '{nestedName}'",
                newObj.properties.AutoMetrics.Id, newObj.properties.AutoMetrics.ParentId, newObj.properties.AutoMetrics.name);

            // Проверяем массив вложенных объектов
            logger.LogInformation("📋 Массив вложенных объектов (RelatedMetrics):");
            if (newObj.properties.RelatedMetrics != null)
            {
                for (int i = 0; i < newObj.properties.RelatedMetrics.Length; i++)
                {
                    var arrayItem = newObj.properties.RelatedMetrics[i];
                    logger.LogInformation("   [{index}] ID: {arrayId}, ParentId: {arrayParentId}, Name: '{arrayName}'",
                        i, arrayItem.Id, arrayItem.ParentId, arrayItem.name);
                }
            }

            // 📖 ЗАГРУЖАЕМ И ПРОВЕРЯЕМ ИЗ БАЗЫ ДАННЫХ
            logger.LogInformation("📖 === ПРОВЕРКА ЗАГРУЗКИ ИЗ БД ===");
            var loaded = await redb.LoadAsync<MixedTestProps>(CreatedObjectId);

            logger.LogInformation("📋 Загруженный основной объект:");
            logger.LogInformation("   ID: {loadedId}, ParentId: {loadedParentId}, Name: '{loadedName}'",
                loaded.Id, loaded.ParentId, loaded.name);

            logger.LogInformation("📋 Загруженный вложенный объект (AutoMetrics):");
            if (loaded.properties.AutoMetrics != null)
            {
                logger.LogInformation("   ID: {loadedNestedId}, ParentId: {loadedNestedParentId}, Name: '{loadedNestedName}'",
                    loaded.properties.AutoMetrics.Id, loaded.properties.AutoMetrics.ParentId, loaded.properties.AutoMetrics.name);
            }

            logger.LogInformation("📋 Загруженный массив вложенных объектов (RelatedMetrics):");
            if (loaded.properties.RelatedMetrics != null)
            {
                for (int i = 0; i < loaded.properties.RelatedMetrics.Length; i++)
                {
                    var loadedArrayItem = loaded.properties.RelatedMetrics[i];
                    logger.LogInformation("   [{index}] ID: {loadedArrayId}, ParentId: {loadedArrayParentId}, Name: '{loadedArrayName}'",
                        i, loadedArrayItem.Id, loadedArrayItem.ParentId, loadedArrayItem.name);
                }
            }

            // 🎯 ПРОВЕРКА РЕЗУЛЬТАТА
            bool parentIdSetCorrectly = true;

            // Проверяем одиночный вложенный объект
            if (loaded.properties.AutoMetrics != null && loaded.properties.AutoMetrics.ParentId != loaded.Id)
            {
                logger.LogWarning("⚠️  ПРОБЛЕМА: У вложенного объекта AutoMetrics ParentId = {actualParent}, ожидался {expectedParent}",
                    loaded.properties.AutoMetrics.ParentId, loaded.Id);
                parentIdSetCorrectly = false;
            }

            // Проверяем массив вложенных объектов
            if (loaded.properties.RelatedMetrics != null)
            {
                foreach (var arrayItem in loaded.properties.RelatedMetrics)
                {
                    if (arrayItem.ParentId != loaded.Id)
                    {
                        logger.LogWarning("⚠️  ПРОБЛЕМА: У объекта в массиве '{arrayName}' ParentId = {actualParent}, ожидался {expectedParent}",
                            arrayItem.name, arrayItem.ParentId, loaded.Id);
                        parentIdSetCorrectly = false;
                    }
                }
            }

            if (parentIdSetCorrectly)
            {
                logger.LogInformation("✅ УСПЕХ: ParentId установлен корректно для всех вложенных объектов!");
            }
            else
            {
                logger.LogError("❌ ПРОБЛЕМА: ParentId установлен некорректно для некоторых вложенных объектов!");
            }
        }
    }
}
