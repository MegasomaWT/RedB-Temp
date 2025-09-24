using redb.Core.DBModels;
using redb.Core.Models.Contracts;
using System;

namespace redb.Core.Models.Entities
{
    /// <summary>
    /// Реализация интерфейса IRedbStructure на основе _RStructure
    /// </summary>
    public class RedbStructure : IRedbStructure
    {
        private readonly _RStructure _structure;

        public RedbStructure(_RStructure structure)
        {
            _structure = structure ?? throw new ArgumentNullException(nameof(structure));
        }

        public long Id => _structure.Id;
        public long? IdParent => _structure.IdParent;
        public long IdScheme => _structure.IdScheme;
        public long? IdOverride => _structure.IdOverride;
        public long IdType => _structure.IdType;
        public long? IdList => _structure.IdList;
        public string Name => _structure.Name;
        public string? Alias => _structure.Alias;
        public long? Order => _structure.Order;
        public bool? Readonly => _structure.Readonly;
        public bool? AllowNotNull => _structure.AllowNotNull;
        public bool? IsArray => _structure.IsArray;
        public bool? IsCompress => _structure.IsCompress;
        public bool? StoreNull => _structure.StoreNull;
        public byte[]? DefaultValue => _structure.DefaultValue;
        public string? DefaultEditor => _structure.DefaultEditor;

        /// <summary>
        /// Создать RedbStructure из _RStructure (статический метод)
        /// </summary>
        public static RedbStructure FromEntity(_RStructure structure) => new RedbStructure(structure);

        /// <summary>
        /// Создать IRedbStructure из _RStructure
        /// </summary>
        public static implicit operator RedbStructure(_RStructure structure) => new RedbStructure(structure);

        /// <summary>
        /// Получить _RStructure из IRedbStructure
        /// </summary>
        public static implicit operator _RStructure(RedbStructure redbStructure) => redbStructure._structure;

        public override string ToString()
        {
            var alias = !string.IsNullOrEmpty(Alias) ? $" ({Alias})" : "";
            var arrayIndicator = IsArray == true ? "[]" : "";
            var requiredIndicator = AllowNotNull == true ? "*" : "";
            return $"Structure {Id}: {Name}{alias}{arrayIndicator}{requiredIndicator} [Type: {IdType}]";
        }
    }
}
