using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;
using System;

namespace redb.Core.Models.Security
{
    /// <summary>
    /// Реализация контекста безопасности REDB
    /// Управляет текущим пользователем и системным контекстом
    /// </summary>
    public class RedbSecurityContext : IRedbSecurityContext
    {
        internal IRedbUser? _currentUser;
        internal bool _isSystemContext;
        
        public IRedbUser? CurrentUser => _currentUser;
        public bool IsSystemContext => _isSystemContext;
        public bool IsAuthenticated => _currentUser != null && !_isSystemContext;
        
        public long GetEffectiveUserId()
        {
            var user = GetEffectiveUser();
            return user.Id;
        }
        
        /// <summary>
        /// Получить эффективного пользователя
        /// </summary>
        public IRedbUser GetEffectiveUser()
        {
            // Если есть текущий пользователь и не системный контекст
            if (_currentUser != null && !_isSystemContext)
            {
                return _currentUser;
            }
            
            // Иначе возвращаем системного пользователя
            return RedbUser.SystemUser;
        }
        
        public void SetCurrentUser(IRedbUser? user)
        {
            _currentUser = user;
            _isSystemContext = false; // Сбрасываем системный режим при установке пользователя
        }
        
        public IDisposable CreateSystemContext()
        {
            return new SystemContextScope(this);
        }
        
        /// <summary>
        /// Создать контекст с указанным пользователем
        /// </summary>
        public static RedbSecurityContext WithUser(IRedbUser user)
        {
            var context = new RedbSecurityContext();
            context.SetCurrentUser(user);
            return context;
        }
        
        /// <summary>
        /// Создать системный контекст
        /// </summary>
        public static RedbSecurityContext System()
        {
            return new RedbSecurityContext { _isSystemContext = true };
        }
        
        /// <summary>
        /// Создать контекст с sys пользователем
        /// </summary>
        public static RedbSecurityContext WithAdmin()
        {
            var context = new RedbSecurityContext();
            context.SetCurrentUser(RedbUser.SystemUser);
            return context;
        }
    }
    
    /// <summary>
    /// Временный системный контекст (IDisposable)
    /// </summary>
    internal class SystemContextScope : IDisposable
    {
        private readonly RedbSecurityContext _context;
        private readonly IRedbUser? _previousUser;
        private readonly bool _previousSystemMode;
        
        public SystemContextScope(RedbSecurityContext context)
        {
            _context = context;
            _previousUser = context.CurrentUser;
            _previousSystemMode = context.IsSystemContext;
            
            // Устанавливаем системный режим
            _context._isSystemContext = true;
        }
        
        public void Dispose()
        {
            // Восстанавливаем предыдущее состояние
            _context._currentUser = _previousUser;
            _context._isSystemContext = _previousSystemMode;
        }
    }
}
