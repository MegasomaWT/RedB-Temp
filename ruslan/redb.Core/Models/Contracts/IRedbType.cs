using System;

namespace redb.Core.Models.Contracts
{
    /// <summary>
    /// Интерфейс типа данных REDB
    /// Представляет тип данных для полей в схемах
    /// </summary>
    public interface IRedbType
    {
        /// <summary>
        /// Уникальный идентификатор типа
        /// </summary>
        long Id { get; }
        
        /// <summary>
        /// Наименование типа (String, Long, DateTime, Boolean, etc.)
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Тип в базе данных (varchar, bigint, timestamp, boolean, etc.)
        /// </summary>
        string? DbType { get; }
        
        /// <summary>
        /// Тип в .NET (System.String, System.Int64, System.DateTime, etc.)
        /// </summary>
        string? Type1 { get; }
    }
}
