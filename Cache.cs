using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using PalindromeServer;

public class Cache<TKey, TValue> : IDisposable
{
    private readonly Dictionary<TKey, CacheItem<TValue>> _cache;
    private readonly TimeSpan _defaultExpiration;
    private readonly ReaderWriterLockSlim _lock;
    public readonly ConcurrentDictionary<TKey, SemaphoreSlim> _protection;
    private readonly int _maxSize;
    private readonly Logger log;

    private class CacheItem<T>
    {
        public T Value { get; set; }
        public DateTime ExpirationTime { get; set; }
    }

    public Cache(TimeSpan defaultExpiration, int MaxSize = 5)
    {
        _cache = new Dictionary<TKey, CacheItem<TValue>>(MaxSize);
        _defaultExpiration = defaultExpiration;
        _lock = new ReaderWriterLockSlim();
        //log = new Logger(path);
        _protection = new ConcurrentDictionary<TKey, SemaphoreSlim>();
        _maxSize = MaxSize;
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
            Logger.Log("Uspesno dodavanje u kes", Logger.Metode.Info, "KES");
        }
        catch (Exception e)
        {
            Logger.Log(e.Message, Logger.Metode.Error, "KES");
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
            _protection.Clear();
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
    public TValue GetOrCompute(TKey key, Func<TValue> compute)
    {

        _lock.EnterReadLock();
        try
        {
            if (_cache.TryGetValue(key, out var existing))
            {
                if (existing.ExpirationTime > DateTime.UtcNow)
                {
                    Logger.Log($"Kes hit za kljuc: {key}", Logger.Metode.Info, "KES");
                    return existing.Value;
                }
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }


        SemaphoreSlim newSlim = new SemaphoreSlim(1, 1);
        newSlim.Wait();

        if (_protection.TryAdd(key, newSlim))
        {

            try
            {
                Logger.Log($"Kes miss, racunanje za kljuc: {key}", Logger.Metode.Info, "KES");
                TValue computed = compute();


                _lock.EnterWriteLock();
                try
                {
                    _cache[key] = new CacheItem<TValue>
                    {
                        Value = computed,
                        ExpirationTime = DateTime.UtcNow.Add(_defaultExpiration)
                    };
                }
                finally
                {
                    _lock.ExitWriteLock();
                }

                return computed;
            }
            finally
            {

                _protection.TryRemove(key, out _);
                newSlim.Release();
            }
        }
        else
        {

            newSlim.Release();


            if (_protection.TryGetValue(key, out SemaphoreSlim existingSlim))
            {
                existingSlim.Wait();
                existingSlim.Release();
            }


            _lock.EnterReadLock();
            try
            {
                if (_cache.TryGetValue(key, out var computed))
                {
                    Logger.Log($"Preuzeta vrednost iz kesa nakon cekanja za kljuc: {key}", Logger.Metode.Info, "KES");
                    return computed.Value;
                }


                Logger.Log($"Vrednost nije u kesu nakon cekanja, racunanje za kljuc: {key}", Logger.Metode.Warning, "KES");
                return compute();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}