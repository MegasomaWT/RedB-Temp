using redb.Core.DBModels;
using redb.Core.Models.Contracts;
using System;

namespace redb.Core.Models.Entities
{
    /// <summary>
    /// Реализация интерфейса IRedbList на основе _RList
    /// </summary>
    public class RedbList : IRedbList
    {
        private readonly _RList _list;

        public RedbList(_RList list)
        {
            _list = list ?? throw new ArgumentNullException(nameof(list));
        }

        public long Id => _list.Id;
        public string Name => _list.Name;
        public string? Alias => _list.Alias;

        /// <summary>
        /// Создать RedbList из _RList (статический метод)
        /// </summary>
        public static RedbList FromEntity(_RList list) => new RedbList(list);

        /// <summary>
        /// Создать IRedbList из _RList
        /// </summary>
        public static implicit operator RedbList(_RList list) => new RedbList(list);

        /// <summary>
        /// Получить _RList из IRedbList
        /// </summary>
        public static implicit operator _RList(RedbList redbList) => redbList._list;

        public override string ToString()
        {
            var alias = !string.IsNullOrEmpty(Alias) ? $" ({Alias})" : "";
            return $"List {Id}: {Name}{alias}";
        }
    }
}
