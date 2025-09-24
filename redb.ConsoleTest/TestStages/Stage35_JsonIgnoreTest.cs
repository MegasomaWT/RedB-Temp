using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Models.Entities;
using redb.ConsoleTest.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// ЭТАП 35: Тестирование атрибута [JsonIgnore]
    /// </summary>
    public class Stage35_JsonIgnoreTest : BaseTestStage
    {
        public override int Order => 35;
        public override string Name => "Тестирование [JsonIgnore]";
        public override string Description => "Демонстрация игнорирования полей с атрибутом [JsonIgnore] при создании схем и сохранении объектов";

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🚫 === ЭТАП 35: ТЕСТИРОВАНИЕ [JsonIgnore] ===");
            
            logger.LogInformation("📋 Тестируем класс JsonIgnoreTestProps:");
            logger.LogInformation("   ✅ Сохраняемые поля: Name, Stock, Price, CreatedDate, Description, IsActive");
            logger.LogInformation("   ❌ Игнорируемые поля: TempValue, CacheTime, IsInMemoryOnly, ComputedField");
            
            // 🔧 СОЗДАНИЕ СХЕМЫ с учетом [JsonIgnore]
            logger.LogInformation("🔧 Создаем схему с автоматическим исключением [JsonIgnore] полей...");
            var scheme = await redb.SyncSchemeAsync<JsonIgnoreTestProps>();
            logger.LogInformation("✅ Схема создана: {schemeName} (ID: {schemeId})", scheme.Name, scheme.Id);
            
            // 📊 АНАЛИЗ СОЗДАННЫХ СТРУКТУР
            logger.LogInformation("📊 Анализируем созданные структуры схемы...");
            var structures = await redb.GetStructuresAsync(scheme);
            logger.LogInformation("📋 Всего структур создано: {count}", structures.Count);
            
            foreach (var structure in structures)
            {
                logger.LogInformation("   ✅ Структура: {name} - Type ID: {typeId}", structure.Name, structure.IdType);
            }
            
            // 🚫 ПРОВЕРКА что игнорируемые поля НЕ создали структуры
            var ignoredFields = new[] { "TempValue", "CacheTime", "IsInMemoryOnly", "ComputedField" };
            var foundIgnoredFields = structures.Where(s => ignoredFields.Contains(s.Name)).ToList();
            
            if (foundIgnoredFields.Any())
            {
                logger.LogError("❌ ОШИБКА: Найдены структуры для игнорируемых полей:");
                foreach (var field in foundIgnoredFields)
                {
                    logger.LogError("   ❌ {fieldName} - НЕ должно быть создано!", field.Name);
                }
            }
            else
            {
                logger.LogInformation("✅ УСПЕХ: Игнорируемые поля корректно исключены из схемы");
            }
            
            // 💾 СОЗДАНИЕ И СОХРАНЕНИЕ ОБЪЕКТА
            logger.LogInformation("💾 Создаем объект с игнорируемыми полями...");
            var testObj = new RedbObject<JsonIgnoreTestProps>
            {
                name = "Тест JsonIgnore",
                note = "Объект для тестирования игнорирования полей",
                properties = new JsonIgnoreTestProps
                {
                    // ✅ Сохраняемые поля
                    Name = "Тестовый продукт",
                    Stock = 100,
                    Price = 999.99,
                    CreatedDate = DateTime.Now,
                    Description = "Описание продукта",
                    IsActive = true,
                    
                    // ❌ Игнорируемые поля (НЕ должны попасть в БД)
                    TempValue = "Секретная информация",
                    CacheTime = DateTime.Now.AddHours(1),
                    IsInMemoryOnly = true
                }
            };
            
            logger.LogInformation("📋 Значения ПЕРЕД сохранением:");
            logger.LogInformation("   ✅ Name: '{name}'", testObj.properties.Name);
            logger.LogInformation("   ✅ Stock: {stock}", testObj.properties.Stock);
            logger.LogInformation("   ✅ Price: ${price}", testObj.properties.Price);
            logger.LogInformation("   ✅ Description: '{desc}'", testObj.properties.Description);
            logger.LogInformation("   ❌ TempValue: '{temp}' (должно быть проигнорировано)", testObj.properties.TempValue);
            logger.LogInformation("   ❌ IsInMemoryOnly: {inMemory} (должно быть проигнорировано)", testObj.properties.IsInMemoryOnly);
            logger.LogInformation("   ❌ ComputedField: '{computed}' (должно быть проигнорировано)", testObj.properties.ComputedField);
            
            // Сохраняем объект
            var savedId = await redb.SaveAsync(testObj);
            logger.LogInformation("✅ Объект сохранен с ID: {id}", savedId);
            
            // 📖 ЗАГРУЖАЕМ И ПРОВЕРЯЕМ
            logger.LogInformation("📖 Загружаем объект из БД...");
            var loaded = await redb.LoadAsync<JsonIgnoreTestProps>(savedId);
            
            logger.LogInformation("📋 Значения ПОСЛЕ загрузки из БД:");
            logger.LogInformation("   ✅ Name: '{name}' - {status}", 
                loaded.properties.Name, 
                loaded.properties.Name == testObj.properties.Name ? "✅ СОВПАДАЕТ" : "❌ НЕ СОВПАДАЕТ");
            logger.LogInformation("   ✅ Stock: {stock} - {status}", 
                loaded.properties.Stock,
                loaded.properties.Stock == testObj.properties.Stock ? "✅ СОВПАДАЕТ" : "❌ НЕ СОВПАДАЕТ");
            logger.LogInformation("   ✅ Price: ${price} - {status}", 
                loaded.properties.Price,
                Math.Abs(loaded.properties.Price - testObj.properties.Price) < 0.01 ? "✅ СОВПАДАЕТ" : "❌ НЕ СОВПАДАЕТ");
            logger.LogInformation("   ✅ Description: '{desc}' - {status}", 
                loaded.properties.Description,
                loaded.properties.Description == testObj.properties.Description ? "✅ СОВПАДАЕТ" : "❌ НЕ СОВПАДАЕТ");
                
            // Проверяем что игнорируемые поля получили значения по умолчанию
            logger.LogInformation("   ❌ TempValue: '{temp}' (должно быть значение по умолчанию)", loaded.properties.TempValue);
            logger.LogInformation("   ❌ IsInMemoryOnly: {inMemory} (должно быть значение по умолчанию)", loaded.properties.IsInMemoryOnly);
            
            // 🎯 ИТОГОВАЯ ПРОВЕРКА
            bool jsonIgnoreWorking = 
                loaded.properties.TempValue == "Временное значение" &&  // Игнорируемое поле = значение по умолчанию из конструктора
                loaded.properties.Name == testObj.properties.Name &&  // Обычное поле сохранено
                loaded.properties.Stock == testObj.properties.Stock &&  // Обычное поле сохранено
                structures.Count == 6;  // Создано только 6 структур (без игнорируемых)
                
            if (jsonIgnoreWorking)
            {
                logger.LogInformation("✅ УСПЕХ: [JsonIgnore] работает корректно!");
                logger.LogInformation("   ✅ Игнорируемые поля не сохранились в БД");
                logger.LogInformation("   ✅ Обычные поля сохранились корректно");
                logger.LogInformation("   ✅ Схема создалась только для нужных полей");
            }
            else
            {
                logger.LogError("❌ ПРОБЛЕМА: [JsonIgnore] работает некорректно!");
            }
            
            // 🗑️ Очистка тестовых данных
            logger.LogInformation("🗑️ Удаляем тестовый объект...");
            await redb.DeleteAsync(loaded);
            logger.LogInformation("✅ Тестовый объект удален");
        }
    }
}
