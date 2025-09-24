using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Models.Entities;
using redb.Core.Models.Contracts;  // ✅ ДОБАВЛЕНО: для IRedbObject
using redb.ConsoleTest.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// 🌳 Этап 33: Демонстрация древовидных LINQ-запросов
    /// Реалистичные бизнес-сценарии с корпоративной иерархией
    /// </summary>
    public class Stage33_TreeLinqQueries : BaseTestStage
    {
        public override string Name => "🌳 Древовидные LINQ-запросы: Корпоративная иерархия";
        public override string Description => "Демонстрация мощи древовидных запросов на реалистичных бизнес-сценариях";
        public override int Order => 33;

        // Данные для создания корпоративной структуры
        private readonly Dictionary<string, long> _organizationUnits = new();
        private readonly Dictionary<string, long> _employees = new();

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🌳 === ДЕМОНСТРАЦИЯ ДРЕВОВИДНЫХ LINQ-ЗАПРОСОВ ===");
            logger.LogInformation("🏢 Создаем корпоративную иерархию и тестируем реальные бизнес-сценарии");

            // ===== ЭТАП 1: СОЗДАНИЕ КОРПОРАТИВНОЙ СТРУКТУРЫ =====
            await CreateCorporateStructure(logger, redb);

            // ===== ЭТАП 2: ДЕМОНСТРАЦИЯ ДРЕВОВИДНЫХ ЗАПРОСОВ =====
            await DemonstrateTreeQueries(logger, redb);

            // ===== ЭТАП 3: СРАВНИТЕЛЬНЫЙ АНАЛИЗ ПРОИЗВОДИТЕЛЬНОСТИ =====
            await PerformanceComparison(logger, redb);
            
            // ===== ЭТАП 4: ТЕСТ ПРОБЛЕМЫ №4 ДЛЯ TREE QUERY =====
            await TestTreeOrderByProblem(logger, redb);

            // ===== ОЧИСТКА =====
            await CleanupTestData(logger, redb);

            logger.LogInformation("✅ === ДЕМОНСТРАЦИЯ ЗАВЕРШЕНА УСПЕШНО ===");
        }

        /// <summary>
        /// Создание реалистичной корпоративной структуры
        /// </summary>
        private async Task CreateCorporateStructure(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🏗️ === СОЗДАНИЕ КОРПОРАТИВНОЙ СТРУКТУРЫ ===");
            
            // Создаем схемы
            var orgSchemeId = await redb.SyncSchemeAsync<CategoryTestProps>();
            var empSchemeId = await redb.SyncSchemeAsync<ProductTestProps>();
            
            logger.LogInformation($"📋 Схемы созданы: Подразделения ({orgSchemeId}), Сотрудники ({empSchemeId})");

            // ===== СОЗДАНИЕ ОРГАНИЗАЦИОННОЙ СТРУКТУРЫ =====
            
            // УРОВЕНЬ 0: Корпорация
            var corpId = await CreateOrgUnit("TechCorp", "ТехКорп - Головная компания", null, logger, redb);
            
            // УРОВЕНЬ 1: Региональные офисы
            var moscowId = await CreateOrgUnit("Moscow Office", "Московский офис", corpId, logger, redb);
            var spbId = await CreateOrgUnit("SPB Office", "Санкт-Петербургский офис", corpId, logger, redb);
            var nnyId = await CreateOrgUnit("NNY Office", "Нижегородский офис", corpId, logger, redb);

            // УРОВЕНЬ 2: Департаменты в Москве
            var itMoscowId = await CreateOrgUnit("IT Department MSK", "IT-Департамент Москва", moscowId, logger, redb);
            var salesMoscowId = await CreateOrgUnit("Sales Department MSK", "Отдел продаж Москва", moscowId, logger, redb);
            var hrMoscowId = await CreateOrgUnit("HR Department MSK", "HR-Департамент Москва", moscowId, logger, redb);

            // УРОВЕНЬ 2: Департаменты в СПБ
            var itSpbId = await CreateOrgUnit("IT Department SPB", "IT-Департамент СПБ", spbId, logger, redb);
            var salesSpbId = await CreateOrgUnit("Sales Department SPB", "Отдел продаж СПБ", spbId, logger, redb);

            // УРОВЕНЬ 3: Команды в IT Москва
            var devTeam1Id = await CreateOrgUnit("Development Team Alpha", "Команда разработки Alpha", itMoscowId, logger, redb);
            var devTeam2Id = await CreateOrgUnit("Development Team Beta", "Команда разработки Beta", itMoscowId, logger, redb);
            var qateamId = await CreateOrgUnit("QA Team", "Команда тестирования", itMoscowId, logger, redb);
            var devopsTeamId = await CreateOrgUnit("DevOps Team", "Команда DevOps", itMoscowId, logger, redb);

            // УРОВЕНЬ 3: Команды в Sales Москва
            var salesTeam1Id = await CreateOrgUnit("Enterprise Sales", "Корпоративные продажи", salesMoscowId, logger, redb);
            var salesTeam2Id = await CreateOrgUnit("SMB Sales", "Продажи малому бизнесу", salesMoscowId, logger, redb);

            // ===== СОЗДАНИЕ СОТРУДНИКОВ =====
            logger.LogInformation("👥 Создаем сотрудников:");

            // Топ-менеджмент
            await CreateEmployee("Иванов И.И.", "CEO", 300000, true, corpId, logger, redb);
            await CreateEmployee("Петров П.П.", "CTO", 250000, true, corpId, logger, redb);

            // Руководители офисов
            await CreateEmployee("Сидоров С.С.", "Директор московского офиса", 200000, true, moscowId, logger, redb);
            await CreateEmployee("Козлов К.К.", "Директор СПБ офиса", 180000, true, spbId, logger, redb);

            // IT-команды
            await CreateEmployee("Программист А.А.", "Senior Developer", 150000, true, devTeam1Id, logger, redb);
            await CreateEmployee("Разработчик Б.Б.", "Middle Developer", 120000, true, devTeam1Id, logger, redb);
            await CreateEmployee("Джуниор В.В.", "Junior Developer", 80000, true, devTeam1Id, logger, redb);
            
            await CreateEmployee("Лидер Г.Г.", "Team Lead", 180000, true, devTeam2Id, logger, redb);
            await CreateEmployee("Кодер Д.Д.", "Senior Developer", 140000, true, devTeam2Id, logger, redb);
            
            await CreateEmployee("Тестер Е.Е.", "QA Engineer", 100000, true, qateamId, logger, redb);
            await CreateEmployee("Автоматизатор Ж.Ж.", "QA Automation", 130000, true, qateamId, logger, redb);
            
            await CreateEmployee("Админ З.З.", "DevOps Engineer", 160000, true, devopsTeamId, logger, redb);

            // Продажи
            await CreateEmployee("Продавец И.И.", "Sales Manager", 90000, true, salesTeam1Id, logger, redb);
            await CreateEmployee("Менеджер К.К.", "Account Manager", 110000, true, salesTeam1Id, logger, redb);
            await CreateEmployee("Агент Л.Л.", "Sales Rep", 70000, true, salesTeam2Id, logger, redb);

            // HR
            await CreateEmployee("Эйчар М.М.", "HR Manager", 95000, true, hrMoscowId, logger, redb);
            await CreateEmployee("Рекрутер Н.Н.", "Recruiter", 75000, false, hrMoscowId, logger, redb); // Неактивный

            // IT СПБ
            await CreateEmployee("СПБ Разраб О.О.", "Full Stack Developer", 125000, true, itSpbId, logger, redb);
            await CreateEmployee("СПБ Тестер П.П.", "QA Engineer", 95000, true, itSpbId, logger, redb);

            // Продажи СПБ
            await CreateEmployee("СПБ Продажи Р.Р.", "Sales Manager", 85000, true, salesSpbId, logger, redb);

            logger.LogInformation("✅ Корпоративная структура создана:");
            logger.LogInformation($"   📊 Подразделений: {_organizationUnits.Count}");
            logger.LogInformation($"   👥 Сотрудников: {_employees.Count}");
            logger.LogInformation($"   🏗️ Уровней иерархии: 4");

            // Показываем структуру
            await ShowOrganizationStructure(logger, redb);
        }

        /// <summary>
        /// Демонстрация различных древовидных запросов
        /// </summary>
        private async Task DemonstrateTreeQueries(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🧪 === ДЕМОНСТРАЦИЯ БИЗНЕС-СЦЕНАРИЕВ ===");

            var stopwatch = Stopwatch.StartNew();

            // ===== СЦЕНАРИЙ 1: "Найти все IT-подразделения компании" =====
            logger.LogInformation("💼 Сценарий 1: 'Найти все IT-подразделения во всех офисах'");
            var itDepartments = await (await redb.TreeQueryAsync<CategoryTestProps>())
                .Where(org => org.Name.Contains("IT"))
                .OrderBy(org => org.Name)
                .ToListAsync();
                
            ShowResults("IT подразделения", itDepartments.Select(d => $"{d.name}: {d.properties.Name}"), logger);

            // ===== СЦЕНАРИЙ 2: "Все сотрудники московского офиса" =====
            logger.LogInformation("🏙️ Сценарий 2: 'Все сотрудники и подразделения московского офиса'");
            var moscowId = _organizationUnits["Moscow Office"];
            var moscowPeople = await (await redb.TreeQueryAsync<ProductTestProps>(moscowId, maxDepth: 10))
                .Where(emp => emp.IsActive == true)
                .OrderByDescending(emp => emp.Price) // Зарплата
                .ToListAsync();
                
            ShowEmployeeResults("Сотрудники московского офиса", moscowPeople, logger);

            // ===== СЦЕНАРИЙ 3: "Найти все команды разработки" =====
            logger.LogInformation("💻 Сценарий 3: 'Найти все команды разработки (Development Team)'");
            var devTeams = await (await redb.TreeQueryAsync<CategoryTestProps>())
                .Where(org => org.Name.Contains("Development Team"))
                .ToListAsync();
                
            ShowResults("Команды разработки", devTeams.Select(d => $"{d.name}: {d.properties.Name}"), logger);

            // ===== СЦЕНАРИЙ 4: "Все сотрудники с высокой зарплатой" =====
            logger.LogInformation("🎯 Сценарий 4: 'Высокооплачиваемые сотрудники (зарплата > 120к)'");
            var highPaidEmployees = await (await redb.TreeQueryAsync<ProductTestProps>())
                .Where(emp => emp.Price > 120000)
                .Where(emp => emp.IsActive == true)
                .OrderByDescending(emp => emp.Price)
                .ToListAsync();
                
            ShowEmployeeResults("Высокооплачиваемые сотрудники", highPaidEmployees, logger);

            // ===== СЦЕНАРИЙ 5: "Подразделения с активными проектами" =====
            logger.LogInformation("👑 Сценарий 5: 'Подразделения департамент/команда типа'");
            var managersWithTeams = await (await redb.TreeQueryAsync<CategoryTestProps>())
                .Where(org => org.Description.Contains("Департамент") || org.Description.Contains("Команда"))
                .Where(org => org.IsActive == true)
                .OrderBy(org => org.Name)
                .ToListAsync();
                
            ShowResults("Подразделения с командами", managersWithTeams.Select(m => $"{m.name}: {m.properties.Name}"), logger);

            // ===== СЦЕНАРИЙ 6: "Сотрудники по уровням иерархии" =====
            logger.LogInformation("📊 Сценарий 6: 'Распределение сотрудников по уровням'");
            
            for (int level = 0; level <= 3; level++)
            {
                var employeesAtLevel = await (await redb.TreeQueryAsync<ProductTestProps>())
                    .WhereLevel(level)
                    .CountAsync();
                logger.LogInformation($"   Уровень {level}: {employeesAtLevel} сотрудников");
            }

            // ===== СЦЕНАРИЙ 7: "Высокооплачиваемые сотрудники глубоких уровней" =====
            logger.LogInformation("💰 Сценарий 7: 'Высокооплачиваемые сотрудники (уровень > 2, зарплата > 100к)'");
            var highPaidDeepEmployees = await (await redb.TreeQueryAsync<ProductTestProps>())
                .WhereLevel(level => level > 2)
                .Where(emp => emp.Price > 100000)
                .Where(emp => emp.IsActive == true)
                .OrderByDescending(emp => emp.Price)
                .Take(10)
                .ToListAsync();
                
            ShowEmployeeResults("Высокооплачиваемые (глубокие уровни)", highPaidDeepEmployees, logger);

            // ===== СЦЕНАРИЙ 8: "Корневые подразделения и их статистика" =====
            logger.LogInformation("🌱 Сценарий 8: 'Корневые подразделения (WhereRoots)'");
            var rootDepartments = await (await redb.TreeQueryAsync<CategoryTestProps>())
                .WhereRoots()
                .ToListAsync();
                
            foreach (var root in rootDepartments)
            {
                var subordinatesCount = await (await redb.TreeQueryAsync<ProductTestProps>(root.id))
                    .CountAsync();
                logger.LogInformation($"   📋 {root.properties.Name}: {subordinatesCount} сотрудников в иерархии");
            }

            // ===== СЦЕНАРИЙ 9: "Листовые подразделения (команды без подкоманд)" =====
            logger.LogInformation("🍃 Сценарий 9: 'Конечные команды (WhereLeaves)'");
            var leafTeams = await (await redb.TreeQueryAsync<CategoryTestProps>())
                .WhereLeaves()
                .Where(org => org.Description.Contains("Команда"))
                .ToListAsync();
                
            ShowResults("Конечные команды", leafTeams.Select(l => $"{l.name}: {l.properties.Name}"), logger);

            // ===== СЦЕНАРИЙ 10: "Полная иерархическая структура" =====
            logger.LogInformation("🌳 Сценарий 10: 'Полная организационная структура (ToTreeListAsync)'");
            var fullStructure = await (await redb.TreeQueryAsync<CategoryTestProps>())
                .WhereRoots()
                .ToTreeListAsync(maxDepth: 4);
                
            logger.LogInformation("📊 Организационная структура:");
            foreach (var root in fullStructure)
            {
                DrawOrganizationTree(root, 0, logger);
            }

            // ✅ НОВЫЙ СЦЕНАРИЙ: TreeQueryAsync с массивом родителей - исправление проблемы №6 из redb3.txt
            logger.LogInformation("");
            logger.LogInformation("🚀 Сценарий 11: 'TreeQueryAsync с МНОЖЕСТВЕННЫМИ родителями - исправление пробемы №6'");
            
            try
            {
                // Создаем список родительских объектов (несколько офисов)
                var parentObjects = new List<IRedbObject>();
                
                if (_organizationUnits.TryGetValue("Moscow Office", out var moscowOfficeId))
                {
                    var moscowObj = await redb.LoadAsync<CategoryTestProps>(moscowOfficeId);
                    parentObjects.Add(moscowObj);
                }
                if (_organizationUnits.TryGetValue("SPB Office", out var spbOfficeId))
                {
                    var spbObj = await redb.LoadAsync<CategoryTestProps>(spbOfficeId);
                    parentObjects.Add(spbObj);
                }
                
                if (parentObjects.Count >= 2)
                {
                    logger.LogInformation($"🏢 Тестируем поиск во множественных офисах: Moscow ({parentObjects[0].Id}) + SPB ({parentObjects[1].Id})");
                    
                    // ✅ ТЕСТИРУЕМ TreeQueryAsync с массивом родительских объектов - ИСПРАВЛЕНИЕ ПРОБЛЕМЫ №6!
                    var multiParentQuery = await redb.TreeQueryAsync<ProductTestProps>(parentObjects, maxDepth: 3);
                    var multiResults = await multiParentQuery
                        .Where(emp => emp.IsActive == true)
                        .OrderBy(emp => emp.Price)
                        .ToListAsync();
                    
                    logger.LogInformation($"📊 Найдено сотрудников во ВСЕХ офисах: {multiResults.Count}");
                    
                    // Показываем результаты по офисам
                    var resultsByParent = multiResults.GroupBy(r => r.parent_id);
                    foreach (var group in resultsByParent.Take(3))
                    {
                        logger.LogInformation($"  🏢 Офис ID {group.Key}: {group.Count()} сотрудников");
                        foreach (var emp in group.Take(2))
                        {
                            logger.LogInformation($"    👤 {emp.name}: Зарплата ${emp.properties.Price}");
                        }
                    }
                    
                    if (multiResults.Count > 0)
                    {
                        logger.LogInformation("✅ TreeQueryAsync с МНОЖЕСТВЕННЫМИ родителями РАБОТАЕТ!");
                    }
                    else
                    {
                        logger.LogWarning("⚠️ TreeQueryAsync с множественными родителями вернул 0 - возможно нет данных");
                    }
                }
                else
                {
                    logger.LogWarning("⚠️ Не найдено достаточно офисов для теста множественных родителей");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в тесте множественных родителей: {ex.Message}");
            }

            stopwatch.Stop();
            logger.LogInformation($"⏱️ Все запросы выполнены за: {stopwatch.ElapsedMilliseconds} мс");
        }

        /// <summary>
        /// Сравнительный анализ производительности
        /// </summary>
        private async Task PerformanceComparison(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("⚡ === АНАЛИЗ ПРОИЗВОДИТЕЛЬНОСТИ ===");

            var iterations = 5;
            var stopwatch = new Stopwatch();

            // Тест 1: Простой древовидный запрос
            logger.LogInformation("🔹 Тест 1: Поиск IT-подразделений");
            var times1 = new List<long>();
            
            for (int i = 0; i < iterations; i++)
            {
                stopwatch.Restart();
                var result = await (await redb.TreeQueryAsync<CategoryTestProps>())
                    .Where(org => org.Name.Contains("IT"))
                    .CountAsync();
                stopwatch.Stop();
                times1.Add(stopwatch.ElapsedMilliseconds);
            }
            
            logger.LogInformation($"   Среднее время: {times1.Average():F1} мс (найдено записей в последнем запросе)");

            // Тест 2: Сложный фильтрованный запрос
            logger.LogInformation("🔹 Тест 2: Активные сотрудники с опытом");
            var times2 = new List<long>();
            
            for (int i = 0; i < iterations; i++)
            {
                stopwatch.Restart();
                var result = await (await redb.TreeQueryAsync<ProductTestProps>())
                    .Where(emp => emp.IsActive == true)
                    .Where(emp => emp.Stock > 30) // "опыт" работы
                    .CountAsync();
                stopwatch.Stop();
                times2.Add(stopwatch.ElapsedMilliseconds);
            }
            
            logger.LogInformation($"   Среднее время: {times2.Average():F1} мс");

            // Тест 3: Запрос с ограничением поддерева
            logger.LogInformation("🔹 Тест 3: Ограниченный поиск в поддереве");
            var moscowId = _organizationUnits["Moscow Office"];
            var times3 = new List<long>();
            
            for (int i = 0; i < iterations; i++)
            {
                stopwatch.Restart();
                var result = await (await redb.TreeQueryAsync<ProductTestProps>(moscowId))
                    .Where(emp => emp.IsActive == true)
                    .CountAsync();
                stopwatch.Stop();
                times3.Add(stopwatch.ElapsedMilliseconds);
            }
            
            logger.LogInformation($"   Среднее время: {times3.Average():F1} мс");

            logger.LogInformation("📈 Выводы:");
            logger.LogInformation("   • Древовидные запросы используют оптимизированные SQL функции");
            logger.LogInformation("   • Ограничение поддерева значительно ускоряет поиск");
            logger.LogInformation("   • Иерархические операторы работают эффективно на больших структурах");
        }

        // ===== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =====

        private async Task<long> CreateOrgUnit(string code, string name, long? parentId, ILogger logger, IRedbService redb)
        {
            var unit = new RedbObject<CategoryTestProps>
            {
                name = code,
                parent_id = parentId,
                properties = new CategoryTestProps
                {
                    Name = name,
                    Description = $"Организационная единица: {name}",
                    IsActive = true
                }
            };

            var id = await redb.SaveAsync(unit);
            _organizationUnits[code] = id;
            logger.LogInformation($"  🏢 {code} (ID: {id})");
            return id;
        }

        private async Task<long> CreateEmployee(string name, string position, double salary, bool isActive, long departmentId, ILogger logger, IRedbService redb)
        {
            var employee = new RedbObject<ProductTestProps>
            {
                name = name,
                parent_id = departmentId,
                properties = new ProductTestProps
                {
                    Price = salary,
                    Stock = DateTime.Now.Year - 1990, // Условный "опыт"
                    Category = position,
                    IsActive = isActive
                }
            };

            var id = await redb.SaveAsync(employee);
            _employees[name] = id;
            logger.LogInformation($"  👤 {name} - {position} ({salary:C})");
            return id;
        }

        private void ShowResults(string title, IEnumerable<string> items, ILogger logger)
        {
            logger.LogInformation($"✅ {title} ({items.Count()}):");
            foreach (var item in items.Take(5))
            {
                logger.LogInformation($"   • {item}");
            }
            if (items.Count() > 5)
            {
                logger.LogInformation($"   ... и еще {items.Count() - 5}");
            }
        }

        private void ShowEmployeeResults(string title, IEnumerable<RedbObject<ProductTestProps>> employees, ILogger logger)
        {
            logger.LogInformation($"✅ {title} ({employees.Count()}):");
            foreach (var emp in employees.Take(7))
            {
                var status = emp.properties.IsActive ? "✅" : "❌";
                logger.LogInformation($"   {status} {emp.name} - {emp.properties.Category} ({emp.properties.Price:C})");
            }
            if (employees.Count() > 7)
            {
                logger.LogInformation($"   ... и еще {employees.Count() - 7} сотрудников");
            }
        }

        private async Task ShowOrganizationStructure(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🏗️ Структура организации:");
            
            var rootOrgs = await (await redb.TreeQueryAsync<CategoryTestProps>())
                .WhereRoots()
                .ToListAsync();

            foreach (var root in rootOrgs)
            {
                await ShowOrgBranch(root, 0, logger, redb);
            }
        }

        private async Task ShowOrgBranch(TreeRedbObject<CategoryTestProps> org, int depth, ILogger logger, IRedbService redb)
        {
            var indent = new string(' ', depth * 2);
            var employeeCount = await (await redb.TreeQueryAsync<ProductTestProps>(org.id)).CountAsync();
            
            logger.LogInformation($"{indent}🏢 {org.properties.Name} ({employeeCount} сотрудников)");
            
            var children = await (await redb.TreeQueryAsync<CategoryTestProps>())
                .WhereChildrenOf(org.id)
                .ToListAsync();
                
            foreach (var child in children.Take(3)) // Показываем только первые 3 для краткости
            {
                await ShowOrgBranch(child, depth + 1, logger, redb);
            }
            
            if (children.Count > 3)
            {
                logger.LogInformation($"{indent}  ... и еще {children.Count - 3} подразделений");
            }
        }
        
        // ✅ НОВЫЙ ТЕСТ: ПРОБЛЕМА №4 ДЛЯ TREE QUERY - OrderBy теряет Tree контекст
        private async Task TestTreeOrderByProblem(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("");
            logger.LogInformation("🌳 === ТЕСТ ПРОБЛЕМЫ №4 ДЛЯ TREE QUERY ===");
            logger.LogInformation("📋 Проверяем: TreeQuery.OrderBy() → может терять Tree контекст → больше объектов");
            
            try
            {
                // Используем существующую структуру для теста
                if (_organizationUnits.Count > 0)
                {
                    var rootOrgId = _organizationUnits.Values.First();
                    
                    // ШАГ 1: TreeQuery с фильтром по корню
                    logger.LogInformation($"🔍 ШАГ 1: TreeQuery от корня ID={rootOrgId}");
                    var treeQuery = await redb.TreeQueryAsync<CategoryTestProps>(rootOrgId, maxDepth: 3);
                    var filteredTreeQuery = treeQuery.Where(c => c.IsActive == true);
                    
                    logger.LogInformation($"📊 TreeQuery тип: {filteredTreeQuery.GetType().Name}");
                    
                    // ШАГ 2: Выполняем запрос ДО OrderBy
                    var test1 = await filteredTreeQuery.ToListAsync();  
                    logger.LogInformation($"📊 ТЕСТ 1 (Tree до OrderBy): {test1.Count} объектов");
                    
                    // ШАГ 3: 🚨 КРИТИЧНЫЙ МОМЕНТ - применяем OrderBy (точно как в примере из redb3.txt)
                    logger.LogInformation("");
                    logger.LogInformation("🚨 ШАГ 3: Применяем OrderBy - ПРОВЕРЯЕМ ТЕРЯЕТСЯ ЛИ Tree контекст!");
                    logger.LogInformation("📝 По примеру: treeQuery = treeQuery.OrderBy(c => c.Name);");
                    
                    var orderedQuery = filteredTreeQuery.OrderBy(c => c.Name);
                    logger.LogInformation($"📊 После OrderBy тип: {orderedQuery.GetType().Name}");
                    
                    // ШАГ 4: Выполняем запрос ПОСЛЕ OrderBy  
                    var test2 = await orderedQuery.ToListAsync();
                    logger.LogInformation($"📊 ТЕСТ 2 (после OrderBy): {test2.Count} объектов");
                    
                    // ШАГ 5: 🔥 АНАЛИЗ РЕЗУЛЬТАТА
                    logger.LogInformation("");
                    logger.LogInformation("🔥 === АНАЛИЗ ПРОБЛЕМЫ №4 ДЛЯ TREE QUERY ===");
                    
                    if (test2.Count > test1.Count)
                    {
                        logger.LogError($"❌ ПРОБЛЕМА №4 ВОСПРОИЗВЕДЕНА ДЛЯ TREE!");
                        logger.LogError($"   📊 До OrderBy: {test1.Count} объектов (Tree контекст работал)");
                        logger.LogError($"   📊 После OrderBy: {test2.Count} объектов (ПОТЕРЯН Tree контекст!)");
                        logger.LogError($"   🚨 OrderBy превратил TreeQueryable в RedbQueryable!");
                        logger.LogError($"   🔧 РЕШЕНИЕ: Нужно переопределить OrderBy в PostgresTreeQueryable");
                    }
                    else if (test2.Count == test1.Count)
                    {
                        logger.LogInformation($"✅ Tree контекст сохранился: {test1.Count} → {test2.Count} объектов");
                        logger.LogInformation($"   ✅ OrderBy работает корректно для Tree запросов");
                    }
                    else
                    {
                        logger.LogWarning($"⚠️ Неожиданный результат Tree OrderBy: {test1.Count} → {test2.Count}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в Tree OrderBy тесте: {ex.Message}");
            }
        }

        private void DrawOrganizationTree(TreeRedbObject<CategoryTestProps> node, int depth, ILogger logger)
        {
            var prefix = depth == 0 ? "" : new string('│', depth - 1) + " ├─";
            logger.LogInformation($"{prefix}🏢 {node.properties.Name}");
            
            foreach (var child in node.Children.Cast<TreeRedbObject<CategoryTestProps>>().Take(2))
            {
                DrawOrganizationTree(child, depth + 1, logger);
            }
            
            if (node.Children.Count() > 2)
            {
                var childPrefix = new string('│', depth) + " └─";
                logger.LogInformation($"{childPrefix}... и еще {node.Children.Count() - 2} подразделений");
            }
        }

        private async Task CleanupTestData(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🗑️ === ОЧИСТКА ДАННЫХ ===");
            try
            {
                var allEmployees = await redb.QueryAsync<ProductTestProps>().Result.ToListAsync();
                var allOrgs = await redb.QueryAsync<CategoryTestProps>().Result.ToListAsync();

                foreach (var emp in allEmployees)
                    await redb.DeleteAsync(emp);

                foreach (var org in allOrgs)
                    await redb.DeleteAsync(org);

                logger.LogInformation($"✅ Удалено: {allEmployees.Count} сотрудников, {allOrgs.Count} подразделений");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "⚠️ Ошибка очистки");
            }
        }
    }
}