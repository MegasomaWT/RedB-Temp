using System.Collections.Generic;

namespace redb.Core.Models.Users
{
    /// <summary>
    /// Результат валидации данных пользователя
    /// </summary>
    public class UserValidationResult
    {
        /// <summary>
        /// Валидация прошла успешно
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// Список ошибок валидации
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new();
        
        /// <summary>
        /// Добавить ошибку валидации
        /// </summary>
        public void AddError(string field, string message)
        {
            Errors.Add(new ValidationError { Field = field, Message = message });
            IsValid = false;
        }
        
        /// <summary>
        /// Добавить ошибку валидации
        /// </summary>
        public void AddError(ValidationError error)
        {
            Errors.Add(error);
            IsValid = false;
        }
        
        /// <summary>
        /// Создать успешный результат валидации
        /// </summary>
        public static UserValidationResult Success()
        {
            return new UserValidationResult { IsValid = true };
        }
        
        /// <summary>
        /// Создать результат валидации с ошибкой
        /// </summary>
        public static UserValidationResult WithError(string field, string message)
        {
            var result = new UserValidationResult();
            result.AddError(field, message);
            return result;
        }
    }
    
    /// <summary>
    /// Ошибка валидации
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Поле с ошибкой
        /// </summary>
        public string Field { get; set; } = "";
        
        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public string Message { get; set; } = "";
        
        /// <summary>
        /// Код ошибки (опционально)
        /// </summary>
        public string? ErrorCode { get; set; }
    }
}
