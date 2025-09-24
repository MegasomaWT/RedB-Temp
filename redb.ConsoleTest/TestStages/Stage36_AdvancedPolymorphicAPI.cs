using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Models.Entities;
using redb.Core.Providers;
using redb.Core.Utils;
using redb.Core.Models.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using redb.Core.Models.Attributes;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Этап 36: Демонстрация улучшенного полиморфного API с автоматической типизацией
    /// Показывает извлечение бизнес-данных из полиморфных интерфейсов
    /// </summary>
    public class Stage36_AdvancedPolymorphicAPI : BaseTestStage
    {
        public override string Name => "🎯 Улучшенный полиморфный API с типизацией";
        public override string Description => "Демонстрация автоматического определения типов и извлечения бизнес-данных";
        public override int Order => 36;

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🎯 === ДЕМОНСТРАЦИЯ УЛУЧШЕННОГО ПОЛИМОРФНОГО API ===");
            logger.LogInformation("");

            // === ИНИЦИАЛИЗАЦИЯ АВТОМАТИЧЕСКОГО РЕЕСТРА ТИПОВ ===
            await InitializeTypeRegistryAsync(logger, redb);

            // === СОЗДАНИЕ БИЗНЕС-ОБЪЕКТОВ РАЗНЫХ ТИПОВ ===
            var createdObjects = await CreateBusinessObjectsAsync(logger, redb);

            // === ПОСТРОЕНИЕ ДЕРЕВА ИЗ ОБЪЕКТОВ РАЗНЫХ ТИПОВ ===
            await BuildMixedTypeTreeAsync(logger, redb, createdObjects);

            // === ДЕМОНСТРАЦИЯ ПОЛИМОРФНОЙ ЗАГРУЗКИ С ТИПИЗАЦИЕЙ ===
            await DemonstratePolymorphicLoadingAsync(logger, redb, createdObjects);

            // === АНАЛИЗ ДЕРЕВА С ИЗВЛЕЧЕНИЕМ БИЗНЕС-ДАННЫХ ===
            await AnalyzeTreeWithBusinessDataAsync(logger, redb, createdObjects);

            logger.LogInformation("");
            logger.LogInformation("✅ Демонстрация улучшенного полиморфного API завершена!");
        }

        /// <summary>
        /// Инициализация AutomaticTypeRegistry для автоматического определения типов
        /// </summary>
        private async Task InitializeTypeRegistryAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🔧 === ИНИЦИАЛИЗАЦИЯ АВТОМАТИЧЕСКОГО РЕЕСТРА ТИПОВ ===");

            try
            {
                // Пока пропускаем инициализацию AutomaticTypeRegistry
                // Это будет добавлено в следующих версиях
                logger.LogInformation("ℹ️ Имитация инициализации AutomaticTypeRegistry");
                
                // Показываем какие типы будут найдены
                logger.LogInformation("📋 Типы с атрибутами [RedbScheme] (имитация):");
                logger.LogInformation("  • CompanyInfo (схема: CompanyInfo)");
                logger.LogInformation("  • EmployeeInfo (схема: EmployeeInfo)");
                logger.LogInformation("  • ProjectInfo (схема: ProjectInfo)");
                logger.LogInformation("  • AnalyticsRecordProps (схема: Записи аналитики)");
                logger.LogInformation("");
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка инициализации: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Создание бизнес-объектов разных типов для демонстрации
        /// </summary>
        private async Task<CreatedObjectsInfo> CreateBusinessObjectsAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🏭 === СОЗДАНИЕ БИЗНЕС-ОБЪЕКТОВ РАЗНЫХ ТИПОВ ===");

            var objects = new CreatedObjectsInfo();

            using (redb.CreateSystemContext())
            {
                // Создаем компанию
                var company = new RedbObject<CompanyInfo>
                {
                    name = "ООО \"ТехноИнновации\"",
                    note = "Головная компания холдинга",
                    properties = new CompanyInfo
                    {
                        CompanyName = "ООО \"ТехноИнновации\"",
                        Inn = "7707083893",
                        Industry = "Информационные технологии",
                        FoundedYear = 2015,
                        EmployeeCount = 150
                    }
                };

                objects.CompanyId = await redb.SaveAsync(company);
                logger.LogInformation($"🏢 Компания создана: ID={objects.CompanyId}");
                logger.LogInformation($"   • Название: {company.properties.CompanyName}");
                logger.LogInformation($"   • ИНН: {company.properties.Inn}");
                logger.LogInformation($"   • Отрасль: {company.properties.Industry}");

                // Создаем сотрудника
                var employee = new RedbObject<EmployeeInfo>
                {
                    name = "Иванов Иван Иванович",
                    note = "Ведущий разработчик",
                    parent_id = objects.CompanyId,
                    properties = new EmployeeInfo
                    {
                        FullName = "Иванов Иван Иванович",
                        Position = "Senior Developer",
                        Department = "Разработка",
                        HireDate = new DateTime(2020, 3, 15),
                        Salary = 120000,
                        Email = "i.ivanov@technoinnovations.ru"
                    }
                };

                objects.EmployeeId = await redb.SaveAsync(employee);
                logger.LogInformation($"👤 Сотрудник создан: ID={objects.EmployeeId}");
                logger.LogInformation($"   • ФИО: {employee.properties.FullName}");
                logger.LogInformation($"   • Должность: {employee.properties.Position}");
                logger.LogInformation($"   • Зарплата: {employee.properties.Salary:C0}");

                // Создаем проект
                var project = new RedbObject<ProjectInfo>
                {
                    name = "Система управления заказами",
                    note = "Внутренний проект компании",
                    parent_id = objects.EmployeeId,
                    properties = new ProjectInfo
                    {
                        ProjectName = "Система управления заказами",
                        Description = "Автоматизация процесса обработки заказов",
                        StartDate = new DateTime(2024, 1, 10),
                        Status = "В разработке",
                        Budget = 2500000,
                        Progress = 65
                    }
                };

                objects.ProjectId = await redb.SaveAsync(project);
                logger.LogInformation($"📋 Проект создан: ID={objects.ProjectId}");
                logger.LogInformation($"   • Название: {project.properties.ProjectName}");
                logger.LogInformation($"   • Статус: {project.properties.Status}");
                logger.LogInformation($"   • Прогресс: {project.properties.Progress}%");
                logger.LogInformation($"   • Бюджет: {project.properties.Budget:C0}");

                // Создаем аналитический отчет
                var analytics = new RedbObject<AnalyticsRecordProps>
                {
                    name = "Квартальный отчет Q1 2024",
                    note = "Аналитика по проекту",
                    parent_id = objects.ProjectId,
                    properties = new AnalyticsRecordProps
                    {
                        Date = DateTime.Now,
                        Article = "REPORT_Q1_2024",
                        Stock = 100,
                        Orders = 45,
                        TestName = "Производительность системы",
                        Tag = "quarterly-report",
                        stringArr = new[] { "performance", "scalability", "user-experience" },
                        longArr = new long[] { 1000, 2500, 1800 }
                    }
                };

                objects.AnalyticsId = await redb.SaveAsync(analytics);
                logger.LogInformation($"📊 Аналитика создана: ID={objects.AnalyticsId}");
                logger.LogInformation($"   • Артикул: {analytics.properties.Article}");
                logger.LogInformation($"   • Дата: {analytics.properties.Date:dd.MM.yyyy}");
                logger.LogInformation($"   • Заказов: {analytics.properties.Orders}");
            }

            logger.LogInformation($"✅ Создано {4} объектов разных типов в иерархии");
            logger.LogInformation("");

            return objects;
        }

        /// <summary>
        /// Построение древовидной структуры из объектов разных типов
        /// </summary>
        private async Task BuildMixedTypeTreeAsync(ILogger logger, IRedbService redb, CreatedObjectsInfo objects)
        {
            logger.LogInformation("🌳 === ПОСТРОЕНИЕ СМЕШАННОГО ДЕРЕВА ТИПОВ ===");
            logger.LogInformation("Структура дерева:");
            logger.LogInformation("🏢 Компания (CompanyInfo)");
            logger.LogInformation("  └── 👤 Сотрудник (EmployeeInfo)");
            logger.LogInformation("      └── 📋 Проект (ProjectInfo)");
            logger.LogInformation("          └── 📊 Аналитика (AnalyticsRecordProps)");
            logger.LogInformation("");
            logger.LogInformation("Каждый уровень имеет разный тип данных и схему!");
            logger.LogInformation("");
        }

        /// <summary>
        /// Демонстрация полиморфной загрузки с автоматической типизацией
        /// </summary>
        private async Task DemonstratePolymorphicLoadingAsync(ILogger logger, IRedbService redb, CreatedObjectsInfo objects)
        {
            logger.LogInformation("🎯 === ПОЛИМОРФНАЯ ЗАГРУЗКА С АВТОМАТИЧЕСКОЙ ТИПИЗАЦИЕЙ ===");

            try
            {
                // Загружаем корневой объект
                var rootCompany = await redb.LoadAsync<CompanyInfo>(objects.CompanyId);
                
                logger.LogInformation($"📥 Загружаем обычное дерево от корня: {rootCompany.name}");

                // Для демонстрации загружаем обычное типизированное дерево
                // В будущих версиях здесь будет LoadPolymorphicTreeAsync
                var companyTree = await redb.LoadTreeAsync<CompanyInfo>(rootCompany, maxDepth: 5);

                logger.LogInformation("✅ Дерево успешно загружено!");
                logger.LogInformation($"Корневой объект: {companyTree.GetType().Name}");
                logger.LogInformation($"Детей в дереве: {companyTree.Children?.Count ?? 0}");
                logger.LogInformation("");

                // Сохраняем дерево для дальнейшего анализа
                SetStageData("LoadedTree", companyTree);
                SetStageData("RootCompany", rootCompany);
                
                // Загружаем также отдельные объекты для демонстрации
                var employee = await redb.LoadAsync<EmployeeInfo>(objects.EmployeeId);
                var project = await redb.LoadAsync<ProjectInfo>(objects.ProjectId);
                var analytics = await redb.LoadAsync<AnalyticsRecordProps>(objects.AnalyticsId);
                
                SetStageData("Employee", employee);
                SetStageData("Project", project);
                SetStageData("Analytics", analytics);
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка при загрузке: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Анализ дерева с извлечением конкретных бизнес-данных
        /// </summary>
        private async Task AnalyzeTreeWithBusinessDataAsync(ILogger logger, IRedbService redb, CreatedObjectsInfo objects)
        {
            logger.LogInformation("💎 === АНАЛИЗ ОБЪЕКТОВ С ИЗВЛЕЧЕНИЕМ БИЗНЕС-ДАННЫХ ===");

            // Получаем загруженные объекты
            var company = GetStageData<RedbObject<CompanyInfo>>("RootCompany");
            var employee = GetStageData<RedbObject<EmployeeInfo>>("Employee");
            var project = GetStageData<RedbObject<ProjectInfo>>("Project");
            var analytics = GetStageData<RedbObject<AnalyticsRecordProps>>("Analytics");

            if (company == null || employee == null || project == null || analytics == null)
            {
                logger.LogError("❌ Не все объекты найдены в кеше");
                return;
            }

            logger.LogInformation("🔍 Демонстрируем извлечение типизированных бизнес-данных:");
            logger.LogInformation("");

            // Анализируем каждый объект индивидуально
            await AnalyzeBusinessObject(logger, company, 0);
            await AnalyzeBusinessObject(logger, employee, 1);  
            await AnalyzeBusinessObject(logger, project, 2);
            await AnalyzeBusinessObject(logger, analytics, 3);

            logger.LogInformation("");
            logger.LogInformation("💡 === КЛЮЧЕВЫЕ ПРЕИМУЩЕСТВА ТИПИЗАЦИИ ===");
            logger.LogInformation("✅ Полный доступ к бизнес-свойствам каждого типа");
            logger.LogInformation("✅ Безопасное приведение типов с проверкой");
            logger.LogInformation("✅ Intellisense и автодополнение в IDE");
            logger.LogInformation("✅ Компиляционная проверка типов");
            logger.LogInformation("✅ Удобное извлечение конкретных данных");
        }

        /// <summary>
        /// Универсальный анализ бизнес-объекта с извлечением типизированных данных
        /// </summary>
        private async Task AnalyzeBusinessObject(ILogger logger, IRedbObject businessObject, int level)
        {
            var indent = new string(' ', level * 2);

            // 🎯 КЛЮЧЕВАЯ ДЕМОНСТРАЦИЯ: Работа с типизированными объектами
            // Показываем как извлекать конкретные бизнес-данные из каждого типа
            
            switch (businessObject)
            {
                case RedbObject<CompanyInfo> company:
                    logger.LogInformation($"{indent}🏢 КОМПАНИЯ: {company.name}");
                    logger.LogInformation($"{indent}   📋 ИНН: {company.properties.Inn}");
                    logger.LogInformation($"{indent}   🏭 Отрасль: {company.properties.Industry}");
                    logger.LogInformation($"{indent}   📅 Год основания: {company.properties.FoundedYear}");
                    logger.LogInformation($"{indent}   👥 Сотрудников: {company.properties.EmployeeCount}");
                    logger.LogInformation($"{indent}   💰 ID схемы: {company.scheme_id}");
                    logger.LogInformation($"{indent}   📝 Примечание: {company.note}");
                    break;

                case RedbObject<EmployeeInfo> employee:
                    logger.LogInformation($"{indent}👤 СОТРУДНИК: {employee.name}");
                    logger.LogInformation($"{indent}   💼 Должность: {employee.properties.Position}");
                    logger.LogInformation($"{indent}   🏢 Отдел: {employee.properties.Department}");
                    logger.LogInformation($"{indent}   📅 Дата найма: {employee.properties.HireDate:dd.MM.yyyy}");
                    logger.LogInformation($"{indent}   💰 Зарплата: {employee.properties.Salary:C0}");
                    logger.LogInformation($"{indent}   📧 Email: {employee.properties.Email}");
                    logger.LogInformation($"{indent}   💰 ID схемы: {employee.scheme_id}");
                    logger.LogInformation($"{indent}   🔗 Родитель: ID {employee.parent_id}");
                    break;

                case RedbObject<ProjectInfo> project:
                    logger.LogInformation($"{indent}📋 ПРОЕКТ: {project.name}");
                    logger.LogInformation($"{indent}   📝 Описание: {project.properties.Description}");
                    logger.LogInformation($"{indent}   📅 Дата старта: {project.properties.StartDate:dd.MM.yyyy}");
                    logger.LogInformation($"{indent}   🎯 Статус: {project.properties.Status}");
                    logger.LogInformation($"{indent}   📊 Прогресс: {project.properties.Progress}%");
                    logger.LogInformation($"{indent}   💰 Бюджет: {project.properties.Budget:C0}");
                    logger.LogInformation($"{indent}   💰 ID схемы: {project.scheme_id}");
                    logger.LogInformation($"{indent}   🔗 Родитель: ID {project.parent_id}");
                    break;

                case RedbObject<AnalyticsRecordProps> analytics:
                    logger.LogInformation($"{indent}📊 АНАЛИТИКА: {analytics.name}");
                    logger.LogInformation($"{indent}   📄 Артикул: {analytics.properties.Article}");
                    logger.LogInformation($"{indent}   📅 Дата: {analytics.properties.Date:dd.MM.yyyy HH:mm}");
                    logger.LogInformation($"{indent}   📦 Остаток: {analytics.properties.Stock}");
                    logger.LogInformation($"{indent}   🛒 Заказов: {analytics.properties.Orders}");
                    logger.LogInformation($"{indent}   🏷️ Тег: {analytics.properties.Tag}");
                    logger.LogInformation($"{indent}   🔍 Тест: {analytics.properties.TestName}");
                    if (analytics.properties.stringArr?.Length > 0)
                    {
                        logger.LogInformation($"{indent}   📋 Строки: [{string.Join(", ", analytics.properties.stringArr)}]");
                    }
                    if (analytics.properties.longArr?.Length > 0)
                    {
                        logger.LogInformation($"{indent}   📊 Числа: [{string.Join(", ", analytics.properties.longArr)}]");
                    }
                    logger.LogInformation($"{indent}   💰 ID схемы: {analytics.scheme_id}");
                    logger.LogInformation($"{indent}   🔗 Родитель: ID {analytics.parent_id}");
                    break;

                default:
                    logger.LogInformation($"{indent}❓ НЕИЗВЕСТНЫЙ ТИП: {businessObject.GetType().Name}");
                    logger.LogInformation($"{indent}   🆔 ID: {businessObject.Id}");
                    logger.LogInformation($"{indent}   📝 Имя: {businessObject.Name}");
                    break;
            }

            logger.LogInformation("");
        }
    }

    /// <summary>
    /// Информация о созданных объектах
    /// </summary>
    public class CreatedObjectsInfo
    {
        public long CompanyId { get; set; }
        public long EmployeeId { get; set; }
        public long ProjectId { get; set; }
        public long AnalyticsId { get; set; }
    }

    /// <summary>
    /// Модель бизнес-данных: Компания
    /// </summary>
    [RedbScheme]
    public class CompanyInfo
    {
        public string CompanyName { get; set; } = string.Empty;
        public string Inn { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public int FoundedYear { get; set; }
        public int EmployeeCount { get; set; }
    }

    /// <summary>
    /// Модель бизнес-данных: Сотрудник
    /// </summary>
    [RedbScheme]
    public class EmployeeInfo
    {
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public decimal Salary { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Модель бизнес-данных: Проект
    /// </summary>
    [RedbScheme]
    public class ProjectInfo
    {
        public string ProjectName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public int Progress { get; set; }
    }
}
