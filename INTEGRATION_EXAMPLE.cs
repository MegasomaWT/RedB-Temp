using Microsoft.Extensions.DependencyInjection;
using redb.Core.Models.Attributes;
using redb.Core.Postgres.Providers;
using redb.Core.Utils;

namespace redb.Examples
{
    // 1. Определяем модели с новыми атрибутами
    
    [RedbScheme]
    public class Company
    {
        public string Name { get; set; } = string.Empty;
        public string Inn { get; set; } = string.Empty;
        public DateTime RegisterDate { get; set; }
    }

    [RedbScheme("emp")]  // Алиас для краткости
    public class Employee
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public DateTime HireDate { get; set; }
    }

    [RedbScheme]
    public class Project
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = "Active";
    }

    // 2. Пример сервиса, использующего новое API
    
    public class OrganizationService
    {
        private readonly PostgresTreeProvider _treeProvider;

        public OrganizationService(PostgresTreeProvider treeProvider)
        {
            _treeProvider = treeProvider;
        }

        /// <summary>
        /// Получить полную организационную структуру с типизацией
        /// </summary>
        public async Task<OrganizationStructure> GetOrganizationStructureAsync(long companyId)
        {
            // Загружаем компанию как корень
            var company = await LoadCompanyAsync(companyId);
            
            // Получаем полиморфное дерево - автоматически типизированное!
            var orgTree = await _treeProvider.LoadPolymorphicTreeAsync(company, maxDepth: 10);
            
            // Анализируем структуру
            var structure = new OrganizationStructure();
            
            if (orgTree is RedbObject<Company> companyObj)
            {
                structure.CompanyName = companyObj.properties.Name;
                structure.CompanyInn = companyObj.properties.Inn;
            }

            // Собираем сотрудников и проекты из дерева
            await AnalyzeOrganizationTree(orgTree, structure);
            
            return structure;
        }

        /// <summary>
        /// Рекурсивный анализ дерева с автоматической типизацией
        /// </summary>
        private async Task AnalyzeOrganizationTree(ITreeRedbObject node, OrganizationStructure structure)
        {
            // Каждый узел уже типизирован благодаря AutomaticTypeRegistry!
            
            switch (node)
            {
                case RedbObject<Employee> emp:
                    structure.Employees.Add(new EmployeeInfo
                    {
                        Id = emp.Id,
                        FullName = $"{emp.properties.FirstName} {emp.properties.LastName}",
                        Position = emp.properties.Position,
                        Salary = emp.properties.Salary,
                        HireDate = emp.properties.HireDate
                    });
                    break;
                    
                case RedbObject<Project> proj:
                    structure.Projects.Add(new ProjectInfo
                    {
                        Id = proj.Id,
                        Title = proj.properties.Title,
                        Description = proj.properties.Description,
                        Status = proj.properties.Status,
                        StartDate = proj.properties.StartDate,
                        EndDate = proj.properties.EndDate
                    });
                    break;
                    
                // Можно легко добавить новые типы
                case RedbObject<Company> subCompany:
                    structure.Subsidiaries.Add(subCompany.properties.Name);
                    break;
            }

            // Рекурсивно обрабатываем детей
            foreach (var child in node.Children)
            {
                await AnalyzeOrganizationTree(child, structure);
            }
        }

        /// <summary>
        /// Найти всех сотрудников под определенным менеджером
        /// </summary>
        public async Task<List<Employee>> GetTeamMembersAsync(long managerId)
        {
            var manager = await LoadEmployeeAsync(managerId);
            
            // Получаем всех потомков - автоматически типизированных!
            var allSubordinates = await _treeProvider.GetPolymorphicDescendantsAsync(manager);
            
            var teamMembers = new List<Employee>();
            
            foreach (var subordinate in allSubordinates)
            {
                // Фильтруем только сотрудников
                if (subordinate is RedbObject<Employee> emp)
                {
                    teamMembers.Add(emp.properties);
                }
            }
            
            return teamMembers;
        }

        /// <summary>
        /// Получить путь карьерного роста сотрудника
        /// </summary>
        public async Task<List<string>> GetCareerPathAsync(long employeeId)
        {
            var employee = await LoadEmployeeAsync(employeeId);
            
            // Получаем путь к корню - каждый узел типизирован!
            var careerPath = await _treeProvider.GetPolymorphicPathToRootAsync(employee);
            
            var path = new List<string>();
            
            foreach (var level in careerPath.Reverse())
            {
                var levelDescription = level switch
                {
                    RedbObject<Company> company => $"🏢 {company.properties.Name}",
                    RedbObject<Employee> emp => $"👤 {emp.properties.FirstName} {emp.properties.LastName} - {emp.properties.Position}",
                    RedbObject<Project> proj => $"📋 Проект: {proj.properties.Title}",
                    _ => $"❓ {level.Name}"
                };
                
                path.Add(levelDescription);
            }
            
            return path;
        }

        // Вспомогательные методы загрузки (заглушки)
        private async Task<RedbObject<Company>> LoadCompanyAsync(long companyId) 
        {
            // Реализация загрузки компании
            throw new NotImplementedException("Реализовать через IObjectStorageProvider");
        }

        private async Task<RedbObject<Employee>> LoadEmployeeAsync(long employeeId)
        {
            // Реализация загрузки сотрудника  
            throw new NotImplementedException("Реализовать через IObjectStorageProvider");
        }
    }

    // 3. Вспомогательные модели результатов
    
    public class OrganizationStructure
    {
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyInn { get; set; } = string.Empty;
        public List<EmployeeInfo> Employees { get; set; } = new();
        public List<ProjectInfo> Projects { get; set; } = new();
        public List<string> Subsidiaries { get; set; } = new();
    }

    public class EmployeeInfo
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public DateTime HireDate { get; set; }
    }

    public class ProjectInfo
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    // 4. Конфигурация DI контейнера
    
    public static class ServiceConfiguration
    {
        public static async Task ConfigureRedbServicesAsync(IServiceCollection services)
        {
            // ... стандартная конфигурация redb ...
            
            // Инициализация AutomaticTypeRegistry после создания провайдеров
            var serviceProvider = services.BuildServiceProvider();
            var treeProvider = serviceProvider.GetRequiredService<PostgresTreeProvider>();
            
            // 🚀 Ключевой момент - инициализация реестра типов
            await treeProvider.InitializeTypeRegistryAsync();
            
            Console.WriteLine("✅ AutomaticTypeRegistry инициализирован!");
            Console.WriteLine($"✅ Зарегистрированы типы: Company, Employee (alias: emp), Project");
        }
    }

    // 5. Пример использования
    
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Конфигурация сервисов
            var services = new ServiceCollection();
            await ServiceConfiguration.ConfigureRedbServicesAsync(services);
            services.AddScoped<OrganizationService>();
            
            var serviceProvider = services.BuildServiceProvider();
            var orgService = serviceProvider.GetRequiredService<OrganizationService>();
            
            // Использование
            try
            {
                var structure = await orgService.GetOrganizationStructureAsync(companyId: 1);
                
                Console.WriteLine($"🏢 Компания: {structure.CompanyName} (ИНН: {structure.CompanyInn})");
                Console.WriteLine($"👥 Сотрудников: {structure.Employees.Count}");
                Console.WriteLine($"📋 Проектов: {structure.Projects.Count}");
                
                foreach (var employee in structure.Employees)
                {
                    Console.WriteLine($"  👤 {employee.FullName} - {employee.Position} ({employee.Salary:C})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
            }
        }
    }
}
