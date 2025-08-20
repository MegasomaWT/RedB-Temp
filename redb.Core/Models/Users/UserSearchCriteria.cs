using System;

namespace redb.Core.Models.Users
{
    /// <summary>
    /// Критерии поиска пользователей
    /// </summary>
    public class UserSearchCriteria
    {
        /// <summary>
        /// Поиск по логину (частичное совпадение)
        /// </summary>
        public string? LoginPattern { get; set; }
        
        /// <summary>
        /// Поиск по имени (частичное совпадение)
        /// </summary>
        public string? NamePattern { get; set; }
        
        /// <summary>
        /// Поиск по email (частичное совпадение)
        /// </summary>
        public string? EmailPattern { get; set; }
        
        /// <summary>
        /// Фильтр по статусу активности
        /// null - все пользователи, true - только активные, false - только неактивные
        /// </summary>
        public bool? Enabled { get; set; }
        
        /// <summary>
        /// Фильтр по роли (ID роли)
        /// </summary>
        public long? RoleId { get; set; }
        
        /// <summary>
        /// Фильтр по дате регистрации (от)
        /// </summary>
        public DateTime? RegisteredFrom { get; set; }
        
        /// <summary>
        /// Фильтр по дате регистрации (до)
        /// </summary>
        public DateTime? RegisteredTo { get; set; }
        
        /// <summary>
        /// Исключить системных пользователей (ID 0, 1)
        /// </summary>
        public bool ExcludeSystemUsers { get; set; } = true;
        
        /// <summary>
        /// Максимальное количество результатов (0 = без ограничений)
        /// </summary>
        public int Limit { get; set; } = 100;
        
        /// <summary>
        /// Смещение для пагинации
        /// </summary>
        public int Offset { get; set; } = 0;
        
        /// <summary>
        /// Поле для сортировки
        /// </summary>
        public UserSortField SortBy { get; set; } = UserSortField.Name;
        
        /// <summary>
        /// Направление сортировки
        /// </summary>
        public UserSortDirection SortDirection { get; set; } = UserSortDirection.Ascending;
    }
    
    /// <summary>
    /// Поля для сортировки пользователей
    /// </summary>
    public enum UserSortField
    {
        Id,
        Login,
        Name,
        Email,
        DateRegister,
        DateDismiss,
        Enabled
    }
    
    /// <summary>
    /// Направление сортировки пользователей
    /// </summary>
    public enum UserSortDirection
    {
        Ascending,
        Descending
    }
}
