using Microsoft.Extensions.Logging;
using redb.Core;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Интерфейс для этапа тестирования
    /// </summary>
    public interface ITestStage
    {
        /// <summary>
        /// Название этапа
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Описание этапа
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Номер этапа для сортировки
        /// </summary>
        int Order { get; }
        
        /// <summary>
        /// Выполнить этап тестирования
        /// </summary>
        Task ExecuteAsync(ILogger logger, IRedbService redb);
    }
}
