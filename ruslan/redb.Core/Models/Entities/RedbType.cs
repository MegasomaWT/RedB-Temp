using redb.Core.DBModels;
using redb.Core.Models.Contracts;
using System;

namespace redb.Core.Models.Entities
{
    /// <summary>
    /// Реализация интерфейса IRedbType на основе _RType
    /// </summary>
    public class RedbType : IRedbType
    {
        private readonly _RType _type;

        public RedbType(_RType type)
        {
            _type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public long Id => _type.Id;
        public string Name => _type.Name;
        public string? DbType => _type.DbType;
        public string? Type1 => _type.Type1;

        /// <summary>
        /// Получить .NET тип из строкового представления
        /// </summary>
        public Type? GetDotNetType()
        {
            if (string.IsNullOrEmpty(Type1))
                return null;

            return Type1 switch
            {
                "System.String" => typeof(string),
                "System.Int64" => typeof(long),
                "System.Int32" => typeof(int),
                "System.Double" => typeof(double),
                "System.DateTime" => typeof(DateTime),
                "System.Boolean" => typeof(bool),
                "System.Guid" => typeof(Guid),
                _ => Type.GetType(Type1)
            };
        }

        /// <summary>
        /// Проверить, поддерживает ли тип массивы
        /// </summary>
        public bool SupportsArrays()
        {
            // Большинство типов поддерживают массивы
            return !string.IsNullOrEmpty(Name);
        }

        /// <summary>
        /// Создать RedbType из _RType (статический метод)
        /// </summary>
        public static RedbType FromEntity(_RType type) => new RedbType(type);

        /// <summary>
        /// Создать IRedbType из _RType
        /// </summary>
        public static implicit operator RedbType(_RType type) => new RedbType(type);

        /// <summary>
        /// Получить _RType из IRedbType
        /// </summary>
        public static implicit operator _RType(RedbType redbType) => redbType._type;

        public override string ToString()
        {
            var dotNetType = !string.IsNullOrEmpty(Type1) ? $" (.NET: {Type1})" : "";
            var dbType = !string.IsNullOrEmpty(DbType) ? $" (DB: {DbType})" : "";
            return $"Type {Id}: {Name}{dotNetType}{dbType}";
        }
    }
}
