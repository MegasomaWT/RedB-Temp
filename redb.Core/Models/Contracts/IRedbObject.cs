using System;

namespace redb.Core.Models.Contracts
{
    /// <summary>
    /// Базовый интерфейс для всех объектов REDB
    /// Позволяет работать с объектами как с классами, а не с ID
    /// Обогащен полной функциональностью для работы с деревьями, временными метками и аудитом
    /// </summary>
    public interface IRedbObject
    {
        // ===== ОСНОВНЫЕ ИДЕНТИФИКАТОРЫ =====
        
        /// <summary>
        /// Уникальный идентификатор объекта
        /// Используется для извлечения ID из объекта в новых методах
        /// </summary>
        long Id { get; set; }
        
        /// <summary>
        /// Идентификатор схемы объекта
        /// Используется для проверки прав на схему и валидации
        /// </summary>
        long SchemeId { get; set; }
        
        /// <summary>
        /// Наименование объекта
        /// Используется для логирования, отладки и пользовательского интерфейса
        /// </summary>
        string Name { get; set; }

        // ===== ДРЕВОВИДНАЯ СТРУКТУРА =====
        
        /// <summary>
        /// Идентификатор родительского объекта
        /// null - если объект является корневым
        /// </summary>
        long? ParentId { get; set; }
        
        /// <summary>
        /// Проверяет, имеет ли объект родителя
        /// </summary>
        bool HasParent { get; }
        
        /// <summary>
        /// Проверяет, является ли объект корневым (без родителя)
        /// </summary>
        bool IsRoot { get; }

        // ===== ВРЕМЕННЫЕ МЕТКИ =====
        
        /// <summary>
        /// Дата и время создания объекта
        /// </summary>
        DateTime DateCreate { get; set; }
        
        /// <summary>
        /// Дата и время последнего изменения объекта
        /// </summary>
        DateTime DateModify { get; set; }
        
        /// <summary>
        /// Дата начала действия объекта (опционально)
        /// </summary>
        DateTime? DateBegin { get; set; }
        
        /// <summary>
        /// Дата окончания действия объекта (опционально)
        /// </summary>
        DateTime? DateComplete { get; set; }

        // ===== ВЛАДЕНИЕ И АУДИТ =====
        
        /// <summary>
        /// Идентификатор владельца объекта
        /// </summary>
        long OwnerId { get; set; }
        
        /// <summary>
        /// Идентификатор пользователя, который последний раз изменял объект
        /// </summary>
        long WhoChangeId { get; set; }

        // ===== ДОПОЛНИТЕЛЬНЫЕ ИДЕНТИФИКАТОРЫ =====
        
        /// <summary>
        /// Ключевое поле объекта
        /// </summary>
        long? Key { get; set; }
        
        /// <summary>
        /// Целочисленный код объекта
        /// </summary>
        long? CodeInt { get; set; }
        
        /// <summary>
        /// Строковый код объекта
        /// </summary>
        string? CodeString { get; set; }
        
        /// <summary>
        /// GUID код объекта
        /// </summary>
        Guid? CodeGuid { get; set; }

        // ===== СОСТОЯНИЕ ОБЪЕКТА =====
        
        /// <summary>
        /// Булевый флаг объекта
        /// </summary>
        bool? Bool { get; set; }
        
        /// <summary>
        /// Заметки к объекту
        /// </summary>
        string? Note { get; set; }
        
        /// <summary>
        /// MD5 хеш объекта для контроля целостности
        /// </summary>
        Guid? Hash { get; set; }

        /// <summary>
        /// Сбросить ID объекта в 0 и опционально ParentId в null
        /// </summary>
        /// <param name="withParent">Если true, также сбрасывает ParentId в null (по умолчанию true)</param>
        void ResetId(bool withParent = true);
        
        /// <summary>
        /// Сбросить ID и ParentId объекта (рекурсивно обрабатывает вложенные объекты)
        /// </summary>
        /// <param name="recursive">Если true, рекурсивно сбрасывает ID во всех вложенных IRedbObject в properties</param>
        void ResetIds(bool recursive = false);
    }
}
