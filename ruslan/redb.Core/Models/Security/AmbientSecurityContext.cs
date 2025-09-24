using redb.Core.Models.Contracts;
using System;
using System.Threading;

namespace redb.Core.Models.Security
{
    /// <summary>
    /// Ambient контекст безопасности (Thread-Local)
    /// Позволяет автоматически получать текущий контекст безопасности в любом месте кода
    /// </summary>
    public static class AmbientSecurityContext
    {
        private static readonly AsyncLocal<IRedbSecurityContext?> _current = new();
        
        /// <summary>
        /// Текущий контекст безопасности для данного потока
        /// </summary>
        public static IRedbSecurityContext? Current
        {
            get => _current.Value;
            set => _current.Value = value;
        }
        
        /// <summary>
        /// Получить текущий контекст или создать системный по умолчанию
        /// </summary>
        public static IRedbSecurityContext GetOrCreateDefault()
        {
            return Current ?? RedbSecurityContext.WithAdmin();
        }
        
        /// <summary>
        /// Установить контекст на время выполнения действия
        /// </summary>
        public static IDisposable SetContext(IRedbSecurityContext context)
        {
            return new AmbientContextScope(context);
        }
        
        /// <summary>
        /// Создать временный системный контекст
        /// </summary>
        public static IDisposable CreateSystemContext()
        {
            return SetContext(RedbSecurityContext.System());
        }
        
        /// <summary>
        /// Создать временный контекст с пользователем
        /// </summary>
        public static IDisposable CreateUserContext(IRedbUser user)
        {
            return SetContext(RedbSecurityContext.WithUser(user));
        }
        
        /// <summary>
        /// Создать временный sys контекст
        /// </summary>
        public static IDisposable CreateAdminContext()
        {
            return SetContext(RedbSecurityContext.WithAdmin());
        }
    }
    
    /// <summary>
    /// Scope для временного изменения ambient контекста
    /// </summary>
    internal class AmbientContextScope : IDisposable
    {
        private readonly IRedbSecurityContext? _previousContext;
        
        public AmbientContextScope(IRedbSecurityContext newContext)
        {
            _previousContext = AmbientSecurityContext.Current;
            AmbientSecurityContext.Current = newContext;
        }
        
        public void Dispose()
        {
            AmbientSecurityContext.Current = _previousContext;
        }
    }
}
