using System;

namespace redb.Core.Attributes
{
    /// <summary>
    /// Исключает свойство из схемы REDB (но не из JSON сериализации)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class RedbIgnoreAttribute : Attribute
    {
    }
}
