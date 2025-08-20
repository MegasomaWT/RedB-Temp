using redb.Core.DBModels;
using redb.Core.Models.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace redb.Core.Models.Entities
{
    /// <summary>
    /// Реализация интерфейса IRedbScheme на основе _RScheme с инкапсуляцией структур
    /// </summary>
    public class RedbScheme : IRedbScheme
    {
        private readonly _RScheme _scheme;
        private readonly Lazy<IReadOnlyCollection<IRedbStructure>> _structures;
        private readonly Lazy<Dictionary<string, IRedbStructure>> _structuresByName;

        public RedbScheme(_RScheme scheme)
        {
            _scheme = scheme ?? throw new ArgumentNullException(nameof(scheme));
            
            // Ленивая загрузка структур
            _structures = new Lazy<IReadOnlyCollection<IRedbStructure>>(() =>
                _scheme.Structures?.Select(s => (IRedbStructure)RedbStructure.FromEntity(s)).ToList().AsReadOnly() 
                ?? new List<IRedbStructure>().AsReadOnly());
            
            // Ленивая загрузка карты структур по имени для быстрого поиска
            _structuresByName = new Lazy<Dictionary<string, IRedbStructure>>(() =>
                Structures.ToDictionary(s => s.Name, s => s));
        }

        public long Id => _scheme.Id;
        public long? IdParent => _scheme.IdParent;
        public string Name => _scheme.Name;
        public string? Alias => _scheme.Alias;
        public string? NameSpace => _scheme.NameSpace;
        
        /// <summary>
        /// Коллекция структур данной схемы (инкапсулированная)
        /// </summary>
        public IReadOnlyCollection<IRedbStructure> Structures => _structures.Value;
        
        /// <summary>
        /// Быстрый доступ к структуре по имени
        /// </summary>
        public IRedbStructure? GetStructureByName(string name)
        {
            return _structuresByName.Value.TryGetValue(name, out var structure) ? structure : null;
        }

        /// <summary>
        /// Создать RedbScheme из _RScheme (статический метод)
        /// </summary>
        public static RedbScheme FromEntity(_RScheme scheme) => new RedbScheme(scheme);

        /// <summary>
        /// Создать IRedbScheme из _RScheme
        /// </summary>
        public static implicit operator RedbScheme(_RScheme scheme) => new RedbScheme(scheme);

        /// <summary>
        /// Получить _RScheme из IRedbScheme
        /// </summary>
        public static implicit operator _RScheme(RedbScheme redbScheme) => redbScheme._scheme;

        public override string ToString()
        {
            var nameSpace = !string.IsNullOrEmpty(NameSpace) ? $"{NameSpace}." : "";
            var alias = !string.IsNullOrEmpty(Alias) ? $" ({Alias})" : "";
            return $"Scheme {Id}: {nameSpace}{Name}{alias}";
        }
    }
}
