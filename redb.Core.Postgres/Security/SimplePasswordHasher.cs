using System;
using System.Security.Cryptography;
using System.Text;

namespace redb.Core.Postgres.Security
{
    /// <summary>
    /// Простой хешер паролей с использованием SHA256 + соль
    /// FUTURE: В будущем заменить на BCrypt для лучшей безопасности
    /// </summary>
    public static class SimplePasswordHasher
    {
        /// <summary>
        /// Захешировать пароль с солью
        /// </summary>
        /// <param name="password">Пароль в открытом виде</param>
        /// <returns>Хешированный пароль с солью</returns>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Пароль не может быть пустым", nameof(password));

            // Генерируем соль
            var salt = GenerateSalt();
            
            // Хешируем пароль с солью
            var hash = ComputeHash(password, salt);
            
            // Возвращаем соль + хеш в формате: salt:hash
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }
        
        /// <summary>
        /// Проверить пароль
        /// </summary>
        /// <param name="password">Пароль в открытом виде</param>
        /// <param name="hashedPassword">Хешированный пароль из БД</param>
        /// <returns>true если пароль верный</returns>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return false;

            try
            {
                // Разбираем соль и хеш
                var parts = hashedPassword.Split(':');
                if (parts.Length != 2)
                    return false;

                var salt = Convert.FromBase64String(parts[0]);
                var storedHash = Convert.FromBase64String(parts[1]);
                
                // Хешируем введенный пароль с той же солью
                var computedHash = ComputeHash(password, salt);
                
                // Сравниваем хеши
                return AreEqual(storedHash, computedHash);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Генерировать случайную соль
        /// </summary>
        private static byte[] GenerateSalt()
        {
            var salt = new byte[32]; // 256 бит
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
        
        /// <summary>
        /// Вычислить хеш пароля с солью
        /// </summary>
        private static byte[] ComputeHash(string password, byte[] salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                var saltedPassword = new byte[passwordBytes.Length + salt.Length];
                
                Array.Copy(passwordBytes, 0, saltedPassword, 0, passwordBytes.Length);
                Array.Copy(salt, 0, saltedPassword, passwordBytes.Length, salt.Length);
                
                return sha256.ComputeHash(saltedPassword);
            }
        }
        
        /// <summary>
        /// Безопасное сравнение массивов байт (защита от timing attacks)
        /// </summary>
        private static bool AreEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            var result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }
            
            return result == 0;
        }
    }
}
