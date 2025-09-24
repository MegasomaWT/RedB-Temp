using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.DBModels;
using redb.Core.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages;

public class Stage19_DateTimeSorting : ITestStage
{
    public int Order => 19;
    public string Name => "Тестирование сортировки дат и времени";
    public string Description => "Проверка правильной сортировки DateTime полей";

    public async Task ExecuteAsync(ILogger logger, IRedbService redb)
    {
        try
        {
            logger.LogInformation("🕐 Тестирование сортировки дат и времени...");
            
            // Создаем схему для тестирования дат
            var schemeId = await redb.SyncSchemeAsync<EventTestProps>();
            logger.LogInformation($"✅ Схема создана: {nameof(EventTestProps)}, ID: {schemeId}");

            // Создаем тестовые события с разными датами
            var testEvents = new[]
            {
                new { Name = "Event A", EventDate = new DateTime(2024, 1, 15, 10, 30, 0), Priority = 1 },
                new { Name = "Event B", EventDate = new DateTime(2024, 1, 15, 9, 15, 0), Priority = 2 },
                new { Name = "Event C", EventDate = new DateTime(2023, 12, 31, 23, 59, 59), Priority = 3 },
                new { Name = "Event D", EventDate = new DateTime(2024, 2, 1, 8, 0, 0), Priority = 1 },
                new { Name = "Event E", EventDate = new DateTime(2024, 1, 15, 10, 30, 30), Priority = 2 },
                new { Name = "Event F", EventDate = new DateTime(2025, 6, 15, 12, 0, 0), Priority = 1 }
            };

            logger.LogInformation("📋 Создаваемые тестовые события:");
            foreach (var evt in testEvents)
            {
                logger.LogInformation($"  - {evt.Name}: {evt.EventDate:yyyy-MM-dd HH:mm:ss}, Priority={evt.Priority}");
            }

            var createdIds = new List<long>();

            foreach (var evt in testEvents)
            {
                var obj = new RedbObject<EventTestProps>
                {
                    scheme_id = schemeId.Id,
                    name = evt.Name,
                    owner_id = 0,
                    who_change_id = 0,
                    date_create = DateTime.Now,
                    date_modify = DateTime.Now,
                    properties = new EventTestProps 
                    { 
                        EventDate = evt.EventDate,
                        Priority = evt.Priority
                    }
                };

                var savedId = await redb.SaveAsync(obj);
                createdIds.Add(savedId);
                logger.LogInformation($"  📅 Создано событие: {evt.Name} (ID: {savedId})");
            }

            logger.LogInformation("");
            logger.LogInformation("🔍 === ТЕСТИРОВАНИЕ СОРТИРОВКИ ДАТ ===");

            // Тест 1: Сортировка по дате по возрастанию
            logger.LogInformation("📋 Тест 1: OrderBy(EventDate) - хронологический порядок");
            var sortedByDate = await (await redb.QueryAsync<EventTestProps>())
                .OrderBy(e => e.EventDate)
                .ToListAsync();
            
            logger.LogInformation($"📊 События отсортированы по дате (возрастание) - {sortedByDate.Count} шт:");
            foreach (var evt in sortedByDate)
            {
                logger.LogInformation($"  - {evt.name}: {evt.properties.EventDate:yyyy-MM-dd HH:mm:ss}");
            }

            // Проверяем хронологический порядок
            var dates = sortedByDate.Select(e => e.properties.EventDate).ToList();
            var isSorted = dates.SequenceEqual(dates.OrderBy(d => d));
            logger.LogInformation($"✅ Хронологическая сортировка работает: {isSorted}");

            // Тест 2: Сортировка по дате по убыванию
            logger.LogInformation("");
            logger.LogInformation("📋 Тест 2: OrderByDescending(EventDate) - обратный хронологический порядок");
            var sortedByDateDesc = await (await redb.QueryAsync<EventTestProps>())
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
            
            logger.LogInformation($"📊 События отсортированы по дате (убывание) - {sortedByDateDesc.Count} шт:");
            foreach (var evt in sortedByDateDesc.Take(3))
            {
                logger.LogInformation($"  - {evt.name}: {evt.properties.EventDate:yyyy-MM-dd HH:mm:ss}");
            }

            var datesDesc = sortedByDateDesc.Select(e => e.properties.EventDate).ToList();
            var isSortedDesc = datesDesc.SequenceEqual(datesDesc.OrderByDescending(d => d));
            logger.LogInformation($"✅ Обратная хронологическая сортировка работает: {isSortedDesc}");

            // Тест 3: Двойная сортировка - сначала по приоритету, потом по дате
            logger.LogInformation("");
            logger.LogInformation("📋 Тест 3: OrderBy(Priority).ThenBy(EventDate)");
            var sortedByPriorityThenDate = await (await redb.QueryAsync<EventTestProps>())
                .OrderBy(e => e.Priority)
                .ThenBy(e => e.EventDate)
                .ToListAsync();
            
            logger.LogInformation($"📊 События отсортированы по приоритету + дате:");
            foreach (var evt in sortedByPriorityThenDate)
            {
                logger.LogInformation($"  - {evt.name}: Priority={evt.properties.Priority}, Date={evt.properties.EventDate:yyyy-MM-dd HH:mm:ss}");
            }

            // Проверяем сортировку по группам приоритета
            var priorities = sortedByPriorityThenDate.GroupBy(e => e.properties.Priority);
            logger.LogInformation($"✅ Найдено приоритетов: {priorities.Count()}");
            foreach (var priority in priorities)
            {
                var priorityDates = priority.Select(e => e.properties.EventDate).ToList();
                var isPrioritySorted = priorityDates.SequenceEqual(priorityDates.OrderBy(d => d));
                logger.LogInformation($"  - Приоритет {priority.Key}: {priority.Count()} событий, сортировка по дате: {isPrioritySorted}");
            }

            // Очистка тестовых данных
            logger.LogInformation("");
            logger.LogInformation("🎯 === ОЧИСТКА ТЕСТОВЫХ ДАННЫХ ===");
            foreach (var id in createdIds)
            {
                var obj = await redb.LoadAsync<ProductTestProps>(id);
                await redb.DeleteAsync(obj);
                logger.LogInformation($"🗑️ Удалено событие ID: {id}");
            }

            logger.LogInformation("✅ Сортировка дат и времени протестирована успешно");
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ Ошибка в этапе {Order}: {Name}");
            logger.LogError($"❌ {ex.Message}");
            throw;
        }
    }
}

// Модель для тестирования дат
public class EventTestProps
{
    public DateTime EventDate { get; set; }
    public int Priority { get; set; }
}
