using System;
using System.Collections.Generic;
using System.Threading;

public class SimpleCache<TKey, TValue> : IDisposable
{
    private readonly Dictionary<TKey, CacheItem<TValue>> _cache;
    private readonly TimeSpan _defaultExpiration;
    private readonly ReaderWriterLockSlim _lock;

    private class CacheItem<T>
    {
        public T Value { get; set; }
        public DateTime ExpirationTime { get; set; }
    }

    public SimpleCache(TimeSpan defaultExpiration)
    {
        _cache = new Dictionary<TKey, CacheItem<TValue>>();
        _defaultExpiration = defaultExpiration;
        _lock = new ReaderWriterLockSlim();
    }

    public void Add(TKey key, TValue value)
    {
        Add(key, value, _defaultExpiration);
    }

    public void Add(TKey key, TValue value, TimeSpan expiration)
    {
        _lock.EnterWriteLock();
        try
        {
            _cache[key] = new CacheItem<TValue>
            {
                Value = value,
                ExpirationTime = DateTime.UtcNow.Add(expiration)
            };
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool TryGet(TKey key, out TValue value)
    {
        _lock.EnterReadLock();
        try
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (item.ExpirationTime > DateTime.UtcNow)
                {
                    value = item.Value;
                    return true;
                }
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        value = default;
        return false;
    }

    public void Remove(TKey key)
    {
        _lock.EnterWriteLock();
        try
        {
            _cache.Remove(key);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _cache.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void CleanExpired()
    {
        _lock.EnterWriteLock();
        try
        {
            var expiredKeys = new List<TKey>();
            foreach (var kvp in _cache)
            {
                if (kvp.Value.ExpirationTime <= DateTime.UtcNow)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    public void Dispose()
    {
        _lock.Dispose();
    }
}