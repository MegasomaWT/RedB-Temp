using System;

namespace redb.Core.Models.Contracts
{
    /// <summary>
    /// Контекст безопасности REDB
    /// Управляет текущим пользователем и режимом работы
    /// </summary>
    public interface IRedbSecurityContext
    {
        /// <summary>
        /// Текущий пользователь (может быть null для системного контекста)
        /// </summary>
        IRedbUser? CurrentUser { get; }
        
        /// <summary>
        /// Системный контекст (без проверки прав)
        /// </summary>
        bool IsSystemContext { get; }
        
        /// <summary>
        /// Пользователь аутентифицирован (не системный контекст и пользователь установлен)
        /// </summary>
        bool IsAuthenticated { get; }
        
        /// <summary>
        /// Получить эффективный ID пользователя с fallback логикой
        /// Возвращает ID текущего пользователя или sys ID (0)
        /// </summary>
        long GetEffectiveUserId();
        
        /// <summary>
        /// Получить эффективного пользователя
        /// Возвращает текущего пользователя или системного пользователя
        /// </summary>
        IRedbUser GetEffectiveUser();
        
        /// <summary>
        /// Установить текущего пользователя
        /// </summary>
        void SetCurrentUser(IRedbUser? user);
        
        /// <summary>
        /// Создать временный системный контекст
        /// </summary>
        IDisposable CreateSystemContext();
    }
}
