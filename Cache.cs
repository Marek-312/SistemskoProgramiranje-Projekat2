using System.Net;
using System.Collections.Generic;
using System.Threading;
using System;
namespace PalindromeServer
{
    public class Cache<TKey, TValue>
    {
        private readonly Dictionary<TKey, CacheItem<TValue>> _cache;
        private readonly TimeSpan _defaultExparation;
        private readonly ReaderWriterLockSlim _lock;
        private readonly SemaphoreSlim _semaphore;
        private class CacheItem<T>
        {
            public T Value { get; set; }
            public DateTime ExpirationTime { get; set; }
        }
        public Cache(TimeSpan defaultExparation)
        {
            _cache = new Dictionary<TKey, CacheItem<TValue>>();
            _defaultExparation = defaultExparation;
            _lock = new ReaderWriterLockSlim();
            _semaphore = new SemaphoreSlim(1, 1);
        }
        public Add(TKey key, TValue value, TimeSpan expiration)
        {

        }
    }
}