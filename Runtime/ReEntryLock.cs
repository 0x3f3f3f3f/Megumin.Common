using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Megumin
{
    /// <summary>
    /// 重入锁
    /// <para/> 保证一个长时间任务执行期间尝试多次调用，返回相同的任务，不多次开始新任务。
    /// 任务完成后，则可以再次开启新任务。
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    /// <remarks>
    /// <para/> 实现中WrapCall为同步调用，没有使用await，方法不会在WrapCall中挂起。也就是说WrapCall不会导致线程切换。
    /// <code>
    /// 用例：
    /// A去酒吧点一瓶啤酒，酒保说吧台没酒了，稍等，现在叫库房送来一箱啤酒。
    /// 酒保打电话给库房，要求送一箱啤酒。
    /// B这时来到酒吧也点了一瓶啤酒，酒保说现在吧台没酒了，稍等，库房正在送酒来。
    ///
    /// 此时酒保不需要再次打电话要求库房送酒，刚才已经通知了，现在等结果就可以。
    /// 这就是防止重入机制，在异步任务执行过程中，不要重复进入相同任务。
    ///
    /// 一段时间后，库房送来一箱啤酒。
    /// 酒保先拿给A一瓶啤酒，然后再拿给B一瓶啤酒。
    ///
    /// 特别注意:要保证A和B的回调执行顺序。
    /// </code>
    /// </remarks>
    public interface IReEntryLock<K, V>
    {
        /// <summary>
        /// 防止重入调用。
        /// <para/> 在function执行期间，相同的key再次调用，不会执行function。
        /// 而是会返回第一次执行function的结果。
        /// </summary>
        /// <param name="key">有Lambda闭包，K不能带in标记</param>
        /// <param name="function"></param>
        /// <returns></returns>
        /// <param name="enable">是否开启</param>
        V WrapCall(K key, Func<V> function, bool enable = true);

        /// <inheritdoc cref="WrapCall(K, Func{V}, bool)"/>
        V WrapCall(K key, Func<K, V> function, bool enable = true);

        /// <inheritdoc cref="WrapCall(K, Func{V}, bool)"/>
        V WrapCall<P1>(K key, in P1 param1, Func<K, P1, V> function, bool enable = true);

        /// <inheritdoc cref="WrapCall(K, Func{V}, bool)"/>
        V WrapCall<P1, P2>(K key, in P1 param1, in P2 param2, Func<K, P1, P2, V> function, bool enable = true);

        /// <inheritdoc cref="WrapCall(K, Func{V}, bool)"/>
        V WrapCall<P1>(K key, in P1 param1, Func<P1, V> function, bool enable = true);

        /// <inheritdoc cref="WrapCall(K, Func{V}, bool)"/>
        V WrapCall<P1, P2>(K key, in P1 param1, in P2 param2, Func<P1, P2, V> function, bool enable = true);
    }

    //function 三种情况，
    //同步函数
    //异步函数同步完成
    //异步函数挂起

    //function函数执行期间，后续调用来自哪个线程
    //与第一次调用相同线程，
    //与第一次调用不同线程，

    //组合有6种情况，有些情况不存在，有些情况不需要保证callback顺序。
    //同步函数           相同线程   不存在
    //异步函数同步完成   相同线程   不存在
    //异步函数挂起       相同线程   需要保证callback顺序
    //同步函数           不同线程   不保证顺序
    //异步函数同步完成   不同线程   不保证顺序
    //异步函数挂起       不同线程   需要保证callback顺序


    //function 三种情况，可以总结为
    //后续调用发生在  function同步执行时，还是 异步挂起时。
    //如果是同步执行时，肯定多线程调用。
    //如果是异步挂起时，可能是多线程，也可能是同一个线程。

    //同步执行时，需要创建一个source，通过source创建task返回。
    //function函数一旦挂起，证明function的结果task可以存在，直接返回即可。

    //function结果的task缓存 和 新创建的source缓存， 有task返回task，无task返回通过Source创建的task。
    //使用source返回的 肯定都是多线程的，不需要保证callback执行顺序。
    //使用Task.Run执行source.TrySetResult，保证不插入后续调用的callback到第一次的callback执行前。

    ///<inheritdoc cref="IReEntryLock{K, V}"/>
    public class ReEntryLockSync<K> //: IReEntryLock<K, V>
    {
        protected ConcurrentDictionary<K, TaskCompletionSource<int>> IsRunningSource { get; } = new();
        public void WrapCall(K key, Action action, bool enable = true)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (enable == false)
            {
                action();
                return;
            }

            if (IsRunningSource.TryGetValue(key, out var source))
            {
                var result = source.Task.Result;
                return;
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                action();
                Task.Run(() =>
                {
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                });
                return;
            }
        }

        public void WrapCall(K key, Action<K> action, bool enable = true)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (enable == false)
            {
                action(key);
                return;
            }

            if (IsRunningSource.TryGetValue(key, out var source))
            {
                var result = source.Task.Result;
                return;
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                action(key);
                Task.Run(() =>
                {
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                });
                return;
            }
        }

        public void WrapCall<P1>(K key, in P1 param1, Action<K, P1> action, bool enable = true)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (enable == false)
            {
                action(key, param1);
                return;
            }

            if (IsRunningSource.TryGetValue(key, out var source))
            {
                var result = source.Task.Result;
                return;
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                action(key, param1);
                Task.Run(() =>
                {
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                });
                return;
            }
        }

        public void WrapCall<P1, P2>(K key, in P1 param1, in P2 param2, Action<K, P1, P2> action, bool enable = true)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (enable == false)
            {
                action(key, param1, param2);
                return;
            }

            if (IsRunningSource.TryGetValue(key, out var source))
            {
                var result = source.Task.Result;
                return;
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                action(key, param1, param2);
                Task.Run(() =>
                {
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                });
                return;
            }
        }

        public void WrapCall<P1>(K key, in P1 param1, Action<P1> action, bool enable = true)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (enable == false)
            {
                action(param1);
                return;
            }

            if (IsRunningSource.TryGetValue(key, out var source))
            {
                var result = source.Task.Result;
                return;
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                action(param1);
                Task.Run(() =>
                {
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                });
                return;
            }
        }

        public void WrapCall<P1, P2>(K key, in P1 param1, in P2 param2, Action<P1, P2> action, bool enable = true)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (enable == false)
            {
                action(param1, param2);
                return;
            }

            if (IsRunningSource.TryGetValue(key, out var source))
            {
                var result = source.Task.Result;
                return;
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                action(param1, param2);
                Task.Run(() =>
                {
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                });
                return;
            }
        }
    }

    ///<inheritdoc cref="IReEntryLock{K, V}"/>
    public class ReEntryLockSync<K, V> : IReEntryLock<K, V>
    {
        protected ConcurrentDictionary<K, TaskCompletionSource<V>> IsRunningSource { get; } = new();
        public V WrapCall(K key, Func<V> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function();
            }

            if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task.Result;
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var result = function();
                Task.Run(() =>
                {
                    //先完成在移除，后续访问尽可能使用当前结果。
                    //当TrySetResult正在执行时遇到其他线程await，其他线程会同步完成，不会影响正确性。
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                });
                return result;
            }
        }

        public V WrapCall(K key, Func<K, V> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(key);
            }

            if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task.Result;
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var result = function(key);
                Task.Run(() =>
                {
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                });
                return result;
            }
        }

        public V WrapCall<P1>(K key, in P1 param1, Func<K, P1, V> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(key, param1);
            }

            if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task.Result;
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var result = function(key, param1);
                Task.Run(() =>
                {
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                });
                return result;
            }
        }

        public V WrapCall<P1, P2>(K key, in P1 param1, in P2 param2, Func<K, P1, P2, V> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(key, param1, param2);
            }

            if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task.Result;
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var result = function(key, param1, param2);
                Task.Run(() =>
                {
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                });
                return result;
            }
        }

        public V WrapCall<P1>(K key, in P1 param1, Func<P1, V> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(param1);
            }

            if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task.Result;
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var result = function(param1);
                Task.Run(() =>
                {
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                });
                return result;
            }
        }

        public V WrapCall<P1, P2>(K key, in P1 param1, in P2 param2, Func<P1, P2, V> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(param1, param2);
            }

            if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task.Result;
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var result = function(param1, param2);
                Task.Run(() =>
                {
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                });
                return result;
            }
        }
    }

    public class ReEntryLockTask<K> : IReEntryLock<K, Task>
    {
        protected ConcurrentDictionary<K, TaskCompletionSource<int>> IsRunningSource { get; } = new();
        Dictionary<K, Task> IsRunningTask = new();
        public Task WrapCall(K key, Func<Task> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function();
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task;
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function();

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    await resultTask;
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public Task WrapCall(K key, Func<K, Task> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(key);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task;
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(key);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    await resultTask;
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public Task WrapCall<P1>(K key, in P1 param1, Func<K, P1, Task> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(key, param1);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task;
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(key, param1);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    await resultTask;
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public Task WrapCall<P1, P2>(K key, in P1 param1, in P2 param2, Func<K, P1, P2, Task> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(key, param1, param2);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task;
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(key, param1, param2);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    await resultTask;
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public Task WrapCall<P1>(K key, in P1 param1, Func<P1, Task> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(param1);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task;
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(param1);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    await resultTask;
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public Task WrapCall<P1, P2>(K key, in P1 param1, in P2 param2, Func<P1, P2, Task> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(param1, param2);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task;
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(param1, param2);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    await resultTask;
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }
    }

    ///<inheritdoc cref="IReEntryLock{K, V}"/>
    public class ReEntryLockTask<K, V> : IReEntryLock<K, Task<V>>
    {
        protected ConcurrentDictionary<K, TaskCompletionSource<V>> IsRunningSource { get; } = new();
        Dictionary<K, Task<V>> IsRunningTask = new();
        public Task<V> WrapCall(K key, Func<Task<V>> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function();
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task;
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function();

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    var result = await resultTask;
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public Task<V> WrapCall(K key, Func<K, Task<V>> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(key);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task;
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(key);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    var result = await resultTask;
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public Task<V> WrapCall<P1>(K key, in P1 param1, Func<K, P1, Task<V>> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(key, param1);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task;
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(key, param1);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    var result = await resultTask;
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public Task<V> WrapCall<P1, P2>(K key, in P1 param1, in P2 param2, Func<K, P1, P2, Task<V>> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(key, param1, param2);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task;
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(key, param1, param2);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    var result = await resultTask;
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public Task<V> WrapCall<P1>(K key, in P1 param1, Func<P1, Task<V>> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(param1);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task;
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(param1);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    var result = await resultTask;
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public Task<V> WrapCall<P1, P2>(K key, in P1 param1, in P2 param2, Func<P1, P2, Task<V>> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(param1, param2);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return source.Task;
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(param1, param2);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    var result = await resultTask;
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }
    }

    ///<inheritdoc cref="IReEntryLock{K, V}"/>
    public class ReEntryLockValueTask<K> : IReEntryLock<K, ValueTask>
    {
        protected ConcurrentDictionary<K, TaskCompletionSource<int>> IsRunningSource { get; } = new();
        Dictionary<K, ValueTask> IsRunningTask = new();
        public ValueTask WrapCall(K key, Func<ValueTask> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function();
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return new ValueTask(source.Task);
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function();

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    await resultTask;
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public ValueTask WrapCall(K key, Func<K, ValueTask> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(key);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return new ValueTask(source.Task);
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(key);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    await resultTask;
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public ValueTask WrapCall<P1>(K key, in P1 param1, Func<K, P1, ValueTask> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(key, param1);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return new ValueTask(source.Task);
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(key, param1);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    await resultTask;
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public ValueTask WrapCall<P1, P2>(K key, in P1 param1, in P2 param2, Func<K, P1, P2, ValueTask> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(key, param1, param2);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return new ValueTask(source.Task);
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(key, param1, param2);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    await resultTask;
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public ValueTask WrapCall<P1>(K key, in P1 param1, Func<P1, ValueTask> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(param1);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return new ValueTask(source.Task);
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(param1);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    await resultTask;
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public ValueTask WrapCall<P1, P2>(K key, in P1 param1, in P2 param2, Func<P1, P2, ValueTask> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(param1, param2);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return new ValueTask(source.Task);
            }
            else
            {
                source = new TaskCompletionSource<int>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(param1, param2);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    await resultTask;
                    source.TrySetResult(1);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }
    }

    ///<inheritdoc cref="IReEntryLock{K, V}"/>
    public class ReEntryLockValueTask<K, V> : IReEntryLock<K, ValueTask<V>>
    {
        protected ConcurrentDictionary<K, TaskCompletionSource<V>> IsRunningSource { get; } = new();
        Dictionary<K, ValueTask<V>> IsRunningTask = new();
        public ValueTask<V> WrapCall(K key, Func<ValueTask<V>> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function();
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return new ValueTask<V>(source.Task);
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function();

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    var result = await resultTask;
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public ValueTask<V> WrapCall(K key, Func<K, ValueTask<V>> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(key);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return new ValueTask<V>(source.Task);
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(key);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    var result = await resultTask;
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public ValueTask<V> WrapCall<P1>(K key, in P1 param1, Func<K, P1, ValueTask<V>> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(key, param1);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return new ValueTask<V>(source.Task);
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(key, param1);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    var result = await resultTask;
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public ValueTask<V> WrapCall<P1, P2>(K key, in P1 param1, in P2 param2, Func<K, P1, P2, ValueTask<V>> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(key, param1, param2);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return new ValueTask<V>(source.Task);
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(key, param1, param2);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    var result = await resultTask;
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public ValueTask<V> WrapCall<P1>(K key, in P1 param1, Func<P1, ValueTask<V>> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(param1);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return new ValueTask<V>(source.Task);
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(param1);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    var result = await resultTask;
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }

        public ValueTask<V> WrapCall<P1, P2>(K key, in P1 param1, in P2 param2, Func<P1, P2, ValueTask<V>> function, bool enable = true)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (enable == false)
            {
                return function(param1, param2);
            }

            if (IsRunningTask.TryGetValue(key, out var task))
            {
                return task;
            }
            else if (IsRunningSource.TryGetValue(key, out var source))
            {
                return new ValueTask<V>(source.Task);
            }
            else
            {
                source = new TaskCompletionSource<V>();
                IsRunningSource.TryAdd(key, source);

                var resultTask = function(param1, param2);

                if (resultTask.IsCompleted == false)
                {
                    IsRunningTask[key] = resultTask;
                }

                Task.Run(async () =>
                {
                    var result = await resultTask;
                    source.TrySetResult(result);
                    IsRunningSource.TryRemove(key, out var _);
                    IsRunningTask.Remove(key);
                });

                return resultTask;
            }
        }
    }
}
