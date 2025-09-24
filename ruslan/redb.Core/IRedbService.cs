using Microsoft.EntityFrameworkCore;
using redb.Core.Query;
using redb.Core.Providers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using redb.Core.Models.Entities;
using redb.Core.Models.Contracts;
using redb.Core.Models.Configuration;

namespace redb.Core
{
    /// <summary>
    /// Главный интерфейс REDB сервиса - композиция всех провайдеров
    /// </summary>
    public interface IRedbService : 
        ISchemeSyncProvider,
        IObjectStorageProvider,
        ITreeProvider,
        IPermissionProvider,
        IQueryableProvider,
        IValidationProvider
    {
        // === ПРОВАЙДЕРЫ ===
        /// <summary>
        /// Провайдер для управления пользователями
        /// </summary>
        IUserProvider UserProvider { get; }
        
        /// <summary>
        /// Провайдер для управления ролями
        /// </summary>
        IRoleProvider RoleProvider { get; }
        
        // === КОНФИГУРАЦИЯ ===
        /// <summary>
        /// Текущая конфигурация сервиса
        /// </summary>
        RedbServiceConfiguration Configuration { get; }
        
        /// <summary>
        /// Обновить конфигурацию
        /// </summary>
        void UpdateConfiguration(Action<RedbServiceConfiguration> configure);
        
        /// <summary>
        /// Обновить конфигурацию через builder
        /// </summary>
        void UpdateConfiguration(Action<RedbServiceConfigurationBuilder> configureBuilder);
        
        // === КОНТЕКСТ БЕЗОПАСНОСТИ ===
        /// <summary>
        /// Контекст безопасности для управления пользователями и правами
        /// </summary>
        IRedbSecurityContext SecurityContext { get; }
        
        /// <summary>
        /// Установить текущего пользователя
        /// </summary>
        void SetCurrentUser(IRedbUser user);
        
        /// <summary>
        /// Создать временный системный контекст
        /// </summary>
        IDisposable CreateSystemContext();
        
        /// <summary>
        /// Получить эффективный ID пользователя с fallback логикой
        /// </summary>
        long GetEffectiveUserId();
        

        // === МЕТАДАННЫЕ БАЗЫ ДАННЫХ ===
        string dbVersion { get; }
        string dbType { get; }
        string dbMigration { get; }
        int? dbSize { get; }

        // === БАЗОВЫЕ EF CORE ОПЕРАЦИИ (legacy, можно убрать в будущем) ===
        IQueryable<T> GetAll<T>() where T : class;
        Task<T?> GetById<T>(long id) where T : class;
        Task<int> DeleteById<T>(long id) where T : class;

        RedbContext RedbContext { get; }
    }
}
