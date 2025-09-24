using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace redb.Core.Postgres
{
    public partial class RedbContext
    {
        private static readonly ConcurrentQueue<long> _keyCache = new ConcurrentQueue<long>();
        private static readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);
        private static int _cacheSize = 10000;
        private const string SEQUENCE_NAME = "global_identity";
        private const double REFILL_THRESHOLD = 0.1; // 10% от размера кэша
        private static volatile bool _isRefilling = false;


        // Настройка размера кэша
        public static void SetCacheSize(int size)
        {
            _cacheSize = size;
        }

        // Метод для получения следующего ключа
        public override long GetNextKey()
        {
            // Пытаемся получить ключ из кэша
            if (_keyCache.TryDequeue(out long key))
            {
                // Проверяем, нужно ли пополнить кэш (в фоновом режиме)
                int currentCount = _keyCache.Count;
                int threshold = (int)(_cacheSize * REFILL_THRESHOLD);

                if (currentCount <= threshold && !_isRefilling)
                {
                    // Запускаем асинхронное пополнение кэша в фоновом режиме
                    _ = Task.Run(async () => await RefillCacheAsync());
                }

                return key;
            }

            // Если кэш пуст, синхронно заполняем его и возвращаем ключ
            RefillCacheSync();

            if (_keyCache.TryDequeue(out key))
            {
                return key;
            }

            // Если по какой-то причине кэш все еще пуст, возвращаем одиночный ключ
            return GenerateSingleKey();
        }

        // Асинхронный метод для пополнения кэша
        private async Task RefillCacheAsync()
        {
            if (_isRefilling)
                return; // Уже идет пополнение

            // Блокировка для предотвращения одновременного пополнения кэша
            if (!await _cacheLock.WaitAsync(0))
            {
                return; // Кто-то уже пополняет кэш
            }

            try
            {
                _isRefilling = true;

                // Проверяем еще раз, может быть кэш уже пополнили
                int currentCount = _keyCache.Count;
                int keysToGenerate = _cacheSize - currentCount;

                if (keysToGenerate <= 0)
                {
                    return;
                }

                // Получаем новые ключи из последовательности асинхронно
                var keys = await GenerateKeysAsync(keysToGenerate);

                // Добавляем ключи в кэш
                foreach (var newKey in keys)
                {
                    _keyCache.Enqueue(newKey);
                }
            }
            finally
            {
                _isRefilling = false;
                _cacheLock.Release();
            }
        }

        // Синхронный метод для пополнения кэша (для случаев, когда кэш пуст)
        private void RefillCacheSync()
        {
            // Блокировка для предотвращения одновременного пополнения кэша
            if (!_cacheLock.Wait(0))
            {
                return; // Кто-то уже пополняет кэш
            }

            try
            {
                // Проверяем еще раз, может быть кэш уже пополнили
                int currentCount = _keyCache.Count;
                int keysToGenerate = _cacheSize - currentCount;

                if (keysToGenerate <= 0)
                {
                    return;
                }

                // Получаем новые ключи из последовательности
                var keys = GenerateKeys(keysToGenerate);

                // Добавляем ключи в кэш
                foreach (var newKey in keys)
                {
                    _keyCache.Enqueue(newKey);
                }
            }
            finally
            {
                _cacheLock.Release();
            }
        }


        // Метод для получения одного ключа из последовательности
        private long GenerateSingleKey()
        {
            var key = Database.SqlQuery<long>($"SELECT nextval({SEQUENCE_NAME})  as \"Value\"").FirstOrDefault();
            return key;
        }

        // Метод для получения нескольких ключей из последовательности (синхронный)
        private List<long> GenerateKeys(int count)
        {
            var keys = Database.SqlQuery<long>($"SELECT nextval({SEQUENCE_NAME}) FROM generate_series(1, {count})").ToList();
            return keys;
        }

        // Метод для получения нескольких ключей из последовательности (асинхронный)
        private async Task<List<long>> GenerateKeysAsync(int count)
        {
            var sql = $"SELECT nextval('{SEQUENCE_NAME}') AS \"Value\" FROM generate_series(1, {count})";
            var keys = await Database.SqlQueryRaw<long>(sql).ToListAsync();
            return keys;
        }

        /// <summary>
        /// Получает указанное количество ключей напрямую из последовательности БД (минуя кэш)
        /// </summary>
        /// <param name="count">Количество ключей для генерации</param>
        /// <returns>Список сгенерированных ключей</returns>
        /// <exception cref="ArgumentException">Если count <= 0</exception>
        /// <exception cref="InvalidOperationException">При ошибке БД</exception>
        public override List<long> GetKeysBatch(int count)
        {
            // Валидация
            if (count <= 0)
                throw new ArgumentException("Количество ключей должно быть больше 0", nameof(count));

            // Для одного ключа - используем кэш
            if (count == 1)
                return new List<long> { GetNextKey() };

            // Для множественных ключей - прямо из БД
            try
            {
                var keys = GenerateKeys(count > _cacheSize ? _cacheSize : count);
                return keys;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка генерации {count} ключей", ex);
            }
        }

        // ==========================
        // Асинхронные варианты
        // ==========================

        public override async Task<long> GetNextKeyAsync()
        {
            // Быстрая попытка из кэша (синхронно)
            if (_keyCache.TryDequeue(out long key))
            {
                // Проверяем, нужно ли пополнить кэш в фоновом режиме
                int currentCount = _keyCache.Count;
                int threshold = (int)(_cacheSize * REFILL_THRESHOLD);

                if (currentCount <= threshold && !_isRefilling)
                {
                    // Запускаем асинхронное пополнение кэша в фоновом режиме
                    _ = Task.Run(async () => await RefillCacheAsync());
                }

                return key;
            }

            // Если кэш пуст — асинхронно пополнить и взять ключ
            await RefillCacheAsync();
            if (_keyCache.TryDequeue(out key))
                return key;

            // Запасной путь — напрямую из БД
            var val = await Database.SqlQueryRaw<long>($"SELECT nextval('{SEQUENCE_NAME}') AS \"Value\"").FirstAsync();
            return val;
        }

        public override async Task<List<long>> GetKeysBatchAsync(int count)
        {
            if (count <= 0)
                throw new ArgumentException("Количество ключей должно быть больше 0", nameof(count));

            if (count == 1)
                return new List<long> { await GetNextKeyAsync() };

            // Прямой запрос к последовательности (без кэша)
            var sql = $"SELECT nextval('{SEQUENCE_NAME}') AS \"Value\" FROM generate_series(1, {count})";
            var list = await Database.SqlQueryRaw<long>(sql).ToListAsync();
            return list;
        }
    }
}

//// Настройка размера кэша ключей
//// Можно получить значение из конфигурации
//int keyCacheSize = Configuration.GetValue<int>("KeyCacheSize", 1000);
//LtContext.SetCacheSize(keyCacheSize);