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

        // Метод для получения следующего ключа (ПОЛНОСТЬЮ СИНХРОННЫЙ)
        public override long GetNextKey()
        {
            // Пытаемся получить ключ из кэша
            if (_keyCache.TryDequeue(out long key))
            {
                // Проверяем, нужно ли пополнить кэш (СИНХРОННО!)
                int currentCount = _keyCache.Count;
                int threshold = (int)(_cacheSize * REFILL_THRESHOLD);

                if (currentCount <= threshold && !_isRefilling)
                {
                    // ✅ СИНХРОННОЕ пополнение кэша - никаких Task.Run!
                    RefillCacheSync();
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

        // ❌ УДАЛЯЕМ асинхронный метод - заменяем синхронным!

        // Синхронный метод для пополнения кэша (ЕДИНСТВЕННЫЙ!)
        private void RefillCacheSync()
        {
            if (_isRefilling)
                return; // Уже идет пополнение

            // Блокировка для предотвращения одновременного пополнения кэша
            if (!_cacheLock.Wait(0))
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

                // Получаем новые ключи из последовательности (СИНХРОННО!)
                var keys = GenerateKeys(keysToGenerate);

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

        // ❌ УДАЛЯЕМ асинхронный GenerateKeysAsync - используем только синхронный GenerateKeys!

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
        // Асинхронные варианты (ОБЕРТКИ НАД СИНХРОННЫМИ!)
        // ==========================

        public override Task<long> GetNextKeyAsync()
        {
            // ✅ ПРОСТАЯ ОБЕРТКА - вызываем синхронный метод
            return Task.FromResult(GetNextKey());
        }

        public override Task<List<long>> GetKeysBatchAsync(int count)
        {
            // ✅ ПРОСТАЯ ОБЕРТКА - вызываем синхронный метод
            return Task.FromResult(GetKeysBatch(count));
        }
    }
}

//// Настройка размера кэша ключей
//// Можно получить значение из конфигурации
//int keyCacheSize = Configuration.GetValue<int>("KeyCacheSize", 1000);
//LtContext.SetCacheSize(keyCacheSize);