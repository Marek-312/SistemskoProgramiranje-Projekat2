using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using PalindromeServer;

public class SimpleCache<TKey, TValue> : IDisposable
{
    private readonly Dictionary<TKey, CacheItem<TValue>> _cache;
    private readonly TimeSpan _defaultExpiration;
    private readonly ReaderWriterLockSlim _lock;
    public readonly ConcurrentDictionary<TKey, SemaphoreSlim> _protection;
    private readonly Logger log;

    private class CacheItem<T>
    {
        public T Value { get; set; }
        public DateTime ExpirationTime { get; set; }
    }

    public SimpleCache(TimeSpan defaultExpiration, string path)
    {
        _cache = new Dictionary<TKey, CacheItem<TValue>>();
        _defaultExpiration = defaultExpiration;
        _lock = new ReaderWriterLockSlim();
        log = new Logger(path);
        _protection = new ConcurrentDictionary<TKey, SemaphoreSlim>()
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
        CacheItem<TValue> x;
        _lock.EnterReadLock();


        if (_cache.TryGetValue(key, out x))
        {
            return x.Value;
        }
        _lock.ExitReadLock();
        SemaphoreSlim slim = new SemaphoreSlim(1, 1);
        if (_protection.TryAdd(key, slim))
        {
            TValue s = compute();

            x.Value = s;
            x.ExpirationTime = DateTime.UtcNow + _defaultExpiration;
            _lock.EnterWriteLock();
            _cache.Add(key, x);
            _lock.ExitWriteLock();
            _protection.TryRemove(key, out slim);
            return s;
        }
        else
        {

            slim.Wait();
            _lock.EnterReadLock();
            CacheItem<TValue> pls;
            _cache.TryGetValue(key, out pls);
            _lock.ExitReadLock();
            slim.Release();
            return pls.Value;
        }
    }
}