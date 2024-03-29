﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Megumin
{
    /// <summary>
    /// 一种常用的多层缓存机制
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    /// <remarks>
    /// Q: 在传递K key参数时，要不要使用in 参数修饰符？
    /// <para/> https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/keywords/in-parameter-modifier
    /// A: 决定尽可能的使用in。
    /// 只有在K 为int 或者更小的类型时，in会导致更多的开销。
    /// Cache通常用于性能敏感业务，调用次数会非常高，大结构类型做K时，有必要使用in优化。
    /// </remarks>
    public interface ICache<K, V>
    {
        /// <summary>
        /// 清除缓存
        /// </summary>
        /// <returns></returns>
        bool Clear();

        /// <summary>
        /// 可能在初始化时，需要通过某种方式加载缓存，例如磁盘文件
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// 不存在SaveCache方法，通常在UpdateCache中实现。
        /// </remarks>
        bool Load();

        /// <summary>
        /// 从缓存中获取结果
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="option">备用参数，可能的包含缓存淘汰机制相关参数</param>
        /// <returns></returns>
        /// <remarks>
        /// 缓存过期机制在这个方法内部实现
        /// </remarks>
        bool TryGetCache(in K key, out V value, object option = null);

        /// <summary>
        /// 更新结果到缓存中
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="forceReCache"></param>
        /// <param name="option"></param>
        void UpdateCache(in K key, V value, bool forceReCache = false, object option = null);
    }

    public interface ICacheGetter<K, V>
    {
        /// <summary>
        /// 根据Key查找结果
        /// </summary>
        /// <param name="key"></param>
        /// <param name="forceReCache">是否强制重新计算并更新缓存</param>
        /// <param name="option">备用参数，可能的包含缓存淘汰机制相关参数</param>
        /// <returns></returns>
        V Get(K key, bool forceReCache = false, object option = null);
    }

    ///<inheritdoc cref="ICache{K, V}"/>
    public abstract class CacheBase<K, V> : ICache<K, V>
    {
        public abstract bool TryGetCache(in K key, out V value, object option = null);

        public abstract void UpdateCache(in K key, V value, bool forceReCache = false, object option = null);

        public virtual bool Clear()
        {
            return true;
        }

        public virtual bool Load()
        {
            return true;
        }
    }

    ///<inheritdoc/>
    public abstract class Cache<K, V> : CacheBase<K, V>, ICacheGetter<K, V>
    {
        public V Get(K key, bool forceReCache = false, object option = null)
        {
            if (forceReCache == false && TryGetCache(in key, out var result, option))
            {
                return result;
            }
            else
            {
                return ReCache(key, forceReCache, option);
            }
        }

        /// <summary>
        /// 重新计算结果，并更新到缓存中
        /// </summary>
        /// <param name="key"></param>
        /// <param name="forceReCache"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public virtual V ReCache(K key, bool forceReCache = false, object option = null)
        {
            var result = Calculate(key, forceReCache, option);
            UpdateCache(in key, result, forceReCache, option);
            return result;
        }

        /// <summary>
        /// 计算结果
        /// </summary>
        /// <param name="key"></param>
        /// <param name="forceReCache"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public abstract V Calculate(K key, bool forceReCache = false, object option = null);
    }

    ///<inheritdoc/>
    public abstract class DictionaryCache<K, V> : Cache<K, V>
    {
        protected static ConcurrentDictionary<K, V> CacheDic { get; } = new();

        public override bool TryGetCache(in K key, out V value, object option = null)
        {
            return CacheDic.TryGetValue(key, out value);
        }

        public override void UpdateCache(in K key, V value, bool forceReCache = false, object option = null)
        {
            CacheDic[key] = value;
        }

        public override bool Clear()
        {
            CacheDic.Clear();
            return true;
        }
    }

    ///<inheritdoc/>
    public abstract class AsyncCache<K, V> : CacheBase<K, V>, ICacheGetter<K, ValueTask<V>>
    {
        public bool EnabledReEntryLock { get; set; } = true;
        protected ReEntryLockValueTask<K, V> ReEntryLock { get; } = new();

        public ValueTask<V> Get(K key, bool forceReCache = false, object option = null)
        {
            if (forceReCache == false && TryGetCache(in key, out var result, option))
            {
                return new ValueTask<V>(result: result);
            }
            else
            {
                if (EnabledReEntryLock)
                {
                    return ReEntryLock.WrapCall(key, forceReCache, option, ReCache);
                }
                else
                {
                    return ReCache(key, forceReCache, option);
                }
            }
        }

        /// <summary>
        /// 重新计算结果，并更新到缓存中
        /// </summary>
        /// <param name="key"></param>
        /// <param name="forceReCache"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public virtual async ValueTask<V> ReCache(K key, bool forceReCache = false, object option = null)
        {
            var result = await Calculate(key, forceReCache, option);
            UpdateCache(in key, result, forceReCache, option);
            return result;
        }

        /// <summary>
        /// 计算结果
        /// </summary>
        /// <param name="key"></param>
        /// <param name="forceReCache"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public abstract ValueTask<V> Calculate(K key, bool forceReCache = false, object option = null);
    }

    ///<inheritdoc/>
    public abstract class AsyncDictionaryCache<K, V> : AsyncCache<K, V>
    {
        protected static Dictionary<K, V> CacheDic { get; } = new();

        public override bool TryGetCache(in K key, out V value, object option = null)
        {
            return CacheDic.TryGetValue(key, out value);
        }

        public override void UpdateCache(in K key, V value, bool forceReCache = false, object option = null)
        {
            CacheDic[key] = value;
        }

        public override bool Clear()
        {
            CacheDic.Clear();
            return true;
        }
    }
}


