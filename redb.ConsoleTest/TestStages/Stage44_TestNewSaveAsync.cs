using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Models.Entities;
using System;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// ЭТАП 44: Тестирование нового SaveAsync
    /// </summary>
    public class Stage44_TestNewSaveAsync : BaseTestStage
    {
        public override int Order => 44;
        public override string Name => "Тестирование нового SaveAsync";
        public override string Description => "Тестируем новый SaveAsync на простом объекте с существующей схемой";

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🚀 === ЭТАП 44: ТЕСТИРОВАНИЕ НОВОГО SaveAsync ===");

            // Используем схему TestPerson (ID=9001) которая уже существует и имеет 14 структур
            var testObj = new RedbObject<TestPersonProps>
            {
                name = "Тест нового SaveAsync",
                note = "Простой объект для тестирования",
                scheme_id = 9001,  // Принудительно используем существующую схему
                properties = new TestPersonProps
                {
                    Name = "John Test",
                    Age = 35,
                    // ❌ УБИРАЕМ ДУБЛИРУЮЩИЕСЯ ПОЛЯ (City, Street уже есть в Address)
                    
                    // ✅ Простые массивы
                    Tags = new string[] { "test", "saveasync", "new" },
                    Scores = new int[] { 90, 95, 85 },
                    
                    // 🏗️ СЛОЖНЫЙ бизнес-класс с рекурсией
                    Address = new TestAddress
                    {
                        City = "Moscow",  
                        Street = "Red Square 1",
                        
                        // 📊 Массив внутри бизнес-класса
                        Districts = new string[] { "Center", "Kremlin" },
                        
                        // 🏗️ Вложенный бизнес-класс
                        Details = new TestAddressDetails 
                        {
                            Building = "Building A",
                            Floor = 5,
                            Type = "Office"
                        },
                        
                        // 📊 Массив бизнес-классов
                        Contacts = new TestContact[] 
                        {
                            new TestContact { Value = "phone:+7123", Verified = true },
                            new TestContact { Value = "email:test@test.com", Verified = false }
                        }
                    }
                }
            };

            logger.LogInformation("📋 Тестовый объект создан:");
            logger.LogInformation("   Name: {name}", testObj.properties.Name);
            logger.LogInformation("   Age: {age}", testObj.properties.Age);
            logger.LogInformation("   Tags: [{tags}] ({tagCount} элементов)", string.Join(", ", testObj.properties.Tags), testObj.properties.Tags.Length);
            logger.LogInformation("   Scores: [{scores}] ({scoreCount} элементов)", string.Join(", ", testObj.properties.Scores), testObj.properties.Scores.Length);
            logger.LogInformation("🏗️ Address: City={city}, Street={street}", testObj.properties.Address.City, testObj.properties.Address.Street);
            logger.LogInformation("   📊 Districts: [{districts}] ({count} элементов)", string.Join(", ", testObj.properties.Address.Districts), testObj.properties.Address.Districts.Length);
            logger.LogInformation("   🏗️ Details: Building={building}, Floor={floor}, Type={type}", testObj.properties.Address.Details.Building, testObj.properties.Address.Details.Floor, testObj.properties.Address.Details.Type);
            logger.LogInformation("   📊 Contacts: {count} элементов", testObj.properties.Address.Contacts.Length);
            foreach (var contact in testObj.properties.Address.Contacts)
            {
                logger.LogInformation("     - {value} (Verified: {verified})", contact.Value, contact.Verified);
            }

            logger.LogInformation("💾 Сохраняем через новый SaveAsync...");
            var savedId = await redb.SaveAsync(testObj);

            logger.LogInformation("✅ Объект сохранен с ID: {savedId}", savedId);

            // Проверяем что сохранилось в БД
            logger.LogInformation("🔍 Загружаем объект из БД...");
            var loaded = await redb.LoadAsync<TestPersonProps>(savedId);

            logger.LogInformation("📋 Загруженный объект:");
            logger.LogInformation("   ID: {id}, Name: {name}", loaded.Id, loaded.Name);
            logger.LogInformation("   Properties Name: {propName}", loaded.properties.Name);
            logger.LogInformation("   Properties Age: {propAge}", loaded.properties.Age);

            logger.LogInformation("✅ === ЭТАП 44 ЗАВЕРШЕН УСПЕШНО ===");
        }
    }

    // Простой класс для тестирования (аналог существующих полей TestPerson)
    public class TestPersonProps
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        // ❌ УБИРАЕМ City и Street - они есть в Address и вызывают дублирование структур!
        
        // ✅ Простые массивы
        public string[] Tags { get; set; } = new string[0];
        public int[] Scores { get; set; } = new int[0];

        // 🏗️ Бизнес-класс с рекурсией
        public TestAddress Address { get; set; } = new TestAddress();
    }

    // 🏗️ Сложный бизнес-класс с массивами и вложенными классами
    public class TestAddress 
    {
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        
        // 📊 МАССИВ внутри бизнес-класса
        public string[] Districts { get; set; } = new string[0];
        
        // 🏗️ ВЛОЖЕННЫЙ бизнес-класс
        public TestAddressDetails Details { get; set; } = new TestAddressDetails();
        
        // 📊 МАССИВ бизнес-классов
        public TestContact[] Contacts { get; set; } = new TestContact[0];
    }

    // 🏗️ Вложенный бизнес-класс
    public class TestAddressDetails
    {
        public string Building { get; set; } = string.Empty;
        public int Floor { get; set; }
        public string Type { get; set; } = string.Empty;
    }

    // 🏗️ Элемент массива бизнес-классов  
    public class TestContact
    {
        public string Value { get; set; } = string.Empty;
        public bool Verified { get; set; }
    }
}
