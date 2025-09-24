using Microsoft.Extensions.Logging;
using redb.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Базовый класс для этапов тестирования
    /// </summary>
    public abstract class BaseTestStage : ITestStage
    {
        // Статическое хранилище данных между этапами
        private static readonly Dictionary<string, object> _stageData = new();
        private static readonly List<BaseTestStage> _executedStages = new();
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract int Order { get; }

        public async Task ExecuteAsync(ILogger logger, IRedbService redb)
        {
            try
            {
                logger.LogInformation("");
                logger.LogInformation("🔗 === ЭТАП {Order}: {Name} ===", Order, Name.ToUpper());
                
                if (!string.IsNullOrEmpty(Description))
                {
                    logger.LogInformation(Description);
                }

                await ExecuteStageAsync(logger, redb);
                
                // Добавляем этап в список выполненных
                _executedStages.Add(this);
                
                logger.LogInformation("✅ === ЭТАП {Order} ЗАВЕРШЕН УСПЕШНО ===", Order);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Ошибка в этапе {Order}: {Name}", Order, Name);
                throw;
            }
        }

        /// <summary>
        /// Реализация конкретного этапа
        /// </summary>
        protected abstract Task ExecuteStageAsync(ILogger logger, IRedbService redb);

        /// <summary>
        /// Сохранить данные для использования в следующих этапах
        /// </summary>
        protected void SetStageData<T>(string key, T value)
        {
            _stageData[key] = value!;
        }

        /// <summary>
        /// Получить данные из предыдущих этапов
        /// </summary>
        protected T? GetStageData<T>(string key)
        {
            return _stageData.TryGetValue(key, out var value) ? (T)value : default;
        }

        /// <summary>
        /// Получить предыдущий этап определенного типа
        /// </summary>
        protected T? GetPreviousStage<T>() where T : BaseTestStage
        {
            return _executedStages.OfType<T>().LastOrDefault();
        }
    }
}
