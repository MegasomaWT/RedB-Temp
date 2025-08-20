using System;
using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;

namespace redb.Core.Serialization
{
    // Абстракция сериализации JSON объекта из БД в типизированную обёртку
    public interface IRedbObjectSerializer
    {
        RedbObject<TProps> Deserialize<TProps>(string json) where TProps : class, new();
        
        /// <summary>
        /// Динамическая десериализация JSON в типизированный объект на основе runtime типа
        /// </summary>
        /// <param name="json">JSON строка</param>
        /// <param name="propsType">Тип свойств для десериализации</param>
        /// <returns>Десериализованный объект как интерфейс</returns>
        IRedbObject DeserializeDynamic(string json, Type propsType);
    }
}
