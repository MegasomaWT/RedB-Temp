using System;

namespace redb.Core.Models.Contracts
{
    /// <summary>
    /// Интерфейс структуры поля схемы REDB
    /// Представляет метаданные поля в схеме объекта
    /// </summary>
    public interface IRedbStructure
    {
        /// <summary>
        /// Уникальный идентификатор структуры
        /// </summary>
        long Id { get; }
        
        /// <summary>
        /// Идентификатор родительской структуры (для вложенных полей)
        /// </summary>
        long? IdParent { get; }
        
        /// <summary>
        /// Идентификатор схемы, к которой принадлежит структура
        /// </summary>
        long IdScheme { get; }
        
        /// <summary>
        /// Идентификатор переопределяемой структуры (для наследования)
        /// </summary>
        long? IdOverride { get; }
        
        /// <summary>
        /// Идентификатор типа данных
        /// </summary>
        long IdType { get; }
        
        /// <summary>
        /// Идентификатор списка (для полей типа список)
        /// </summary>
        long? IdList { get; }
        
        /// <summary>
        /// Наименование поля
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Псевдоним поля (краткое имя)
        /// </summary>
        string? Alias { get; }
        
        /// <summary>
        /// Порядок поля в схеме
        /// </summary>
        long? Order { get; }
        
        /// <summary>
        /// Поле только для чтения
        /// </summary>
        bool? Readonly { get; }
        
        /// <summary>
        /// Поле обязательно для заполнения
        /// </summary>
        bool? AllowNotNull { get; }
        
        /// <summary>
        /// Поле является массивом
        /// </summary>
        bool? IsArray { get; }
        
        /// <summary>
        /// Сжимать значения поля
        /// </summary>
        bool? IsCompress { get; }
        
        /// <summary>
        /// Сохранять null значения
        /// </summary>
        bool? StoreNull { get; }
        
        /// <summary>
        /// Значение по умолчанию (в бинарном виде)
        /// </summary>
        byte[]? DefaultValue { get; }
        
        /// <summary>
        /// Редактор по умолчанию для поля
        /// </summary>
        string? DefaultEditor { get; }
    }
}
