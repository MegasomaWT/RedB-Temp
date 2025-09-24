using redb.Core.DBModels;
using redb.Core.Models.Contracts;
using System;

namespace redb.Core.Models.Entities
{
    /// <summary>
    /// Реализация интерфейса IRedbUser на основе _RUser
    /// </summary>
    public class RedbUser : IRedbUser
    {
        private readonly _RUser _user;

        public RedbUser(_RUser user)
        {
            _user = user ?? throw new ArgumentNullException(nameof(user));
        }

        public long Id => _user.Id;
        public string Login => _user.Login;
        public string Name => _user.Name;
        public string Password => _user.Password;
        public bool Enabled => _user.Enabled;
        public DateTime DateRegister => _user.DateRegister;
        public DateTime? DateDismiss => _user.DateDismiss;
        public string? Phone => _user.Phone;
        public string? Email => _user.Email;
        
        // === НОВЫЕ ПОЛЯ ===
        public long? Key => _user.Key;
        public long? CodeInt => _user.CodeInt;
        public string? CodeString => _user.CodeString;
        public Guid? CodeGuid => _user.CodeGuid;
        public string? Note => _user.Note;
        public Guid? Hash => _user.Hash;

        /// <summary>
        /// Системный пользователь (SYS_USER_ID = 0)
        /// </summary>
        public static RedbUser SystemUser => new RedbUser(new _RUser
        {
            Id = 0,
            Login = "sys",
            Name = "System User",
            Password = "",
            Enabled = true,
            DateRegister = DateTime.MinValue,
            DateDismiss = null,
            Phone = null,
            Email = null,
            
            // === НОВЫЕ ПОЛЯ ДЛЯ СИСТЕМНОГО ПОЛЬЗОВАТЕЛЯ ===
            Key = null,
            CodeInt = 0,  // Системный код
            CodeString = "SYS",
            CodeGuid = new Guid("00000000-0000-0000-0000-000000000001"), // Предопределенный GUID для системы
            Note = "Системный пользователь REDB",
            Hash = null  // Хеш для системного пользователя не требуется
        });

        /// <summary>
        /// Создать RedbUser из _RUser (статический метод)
        /// </summary>
        public static RedbUser FromEntity(_RUser user) => new RedbUser(user);

        /// <summary>
        /// Создать IRedbUser из _RUser
        /// </summary>
        public static implicit operator RedbUser(_RUser user) => new RedbUser(user);

        /// <summary>
        /// Получить _RUser из IRedbUser
        /// </summary>
        public static implicit operator _RUser(RedbUser redbUser) => redbUser._user;

        public override string ToString()
        {
            var status = Enabled ? "Active" : "Disabled";
            var codes = new List<string>();
            
            if (CodeInt.HasValue) codes.Add($"Int={CodeInt}");
            if (!string.IsNullOrEmpty(CodeString)) codes.Add($"Str={CodeString}");
            if (Key.HasValue) codes.Add($"Key={Key}");
            
            var codesStr = codes.Count > 0 ? $" [{string.Join(", ", codes)}]" : "";
            
            return $"User {Id}: {Login} ({Name}) - {status}{codesStr}";
        }
    }
}