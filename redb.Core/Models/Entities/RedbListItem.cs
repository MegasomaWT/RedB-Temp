using redb.Core.DBModels;
using redb.Core.Models.Contracts;
using System;

namespace redb.Core.Models.Entities
{
    /// <summary>
    /// Реализация интерфейса IRedbListItem на основе _RListItem
    /// </summary>
    public class RedbListItem : IRedbListItem
    {
        private readonly _RListItem _listItem;

        public RedbListItem(_RListItem listItem)
        {
            _listItem = listItem ?? throw new ArgumentNullException(nameof(listItem));
        }

        public long Id => _listItem.Id;
        public long IdList => _listItem.IdList;
        public string? Value => _listItem.Value;
        public long? IdObject => _listItem.IdObject;

        /// <summary>
        /// Проверить, является ли элемент ссылкой на объект
        /// </summary>
        public bool IsObjectReference => IdObject.HasValue;

        /// <summary>
        /// Получить отображаемое значение элемента
        /// </summary>
        public string GetDisplayValue()
        {
            if (!string.IsNullOrEmpty(Value))
                return Value;
            
            if (IdObject.HasValue)
                return $"Object #{IdObject}";
                
            return $"Item #{Id}";
        }

        /// <summary>
        /// Создать RedbListItem из _RListItem (статический метод)
        /// </summary>
        public static RedbListItem FromEntity(_RListItem listItem) => new RedbListItem(listItem);

        /// <summary>
        /// Создать IRedbListItem из _RListItem
        /// </summary>
        public static implicit operator RedbListItem(_RListItem listItem) => new RedbListItem(listItem);

        /// <summary>
        /// Получить _RListItem из IRedbListItem
        /// </summary>
        public static implicit operator _RListItem(RedbListItem redbListItem) => redbListItem._listItem;

        public override string ToString()
        {
            var displayValue = GetDisplayValue();
            return $"ListItem {Id}: {displayValue} [List: {IdList}]";
        }
    }
}
