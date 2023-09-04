using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Megumin
{
    /// <summary>
    /// 现在当同步完成时，后调用的比先调用的先得到结果，AB的回调顺序是反的。
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    /// <remarks>
    /// <code>
    ///A去酒吧点一瓶啤酒，酒保说吧台没酒了，稍等，现在叫库房送来一箱啤酒。
    ///酒保打电话给库房，要求送一箱啤酒。
    ///B这时来到酒吧也点了一瓶啤酒，酒保说现在吧台没酒了，稍等，库房正在送酒来。
    ///
    ///此时酒保不需要再次打电话要求库房送酒，刚才已经通知了，现在等结果就可以。
    ///这就是防止重入机制，在异步任务执行过程中，不要重复进入相同任务。
    ///
    ///一段时间后，库房送来一箱啤酒。
    ///酒保先拿给A一瓶啤酒，然后再拿给B一瓶啤酒。
    ///
    ///特别注意:要保证A和B的回调执行顺序。
    /// </code>
    /// 
    /// </remarks>
    public class ReEntryLock<K, V>
    {
        /// <summary>
        /// 防止重入。
        /// 当正在进行Calculate时，同一个key再出触发ReCache，自动合并，防止Calculate多次调用。
        /// </summary>
        public bool CombineRecacheOnCalculate = true;
        Dictionary<K, TaskCompletionSource<V>> locker = new Dictionary<K, TaskCompletionSource<V>>();

        public ValueTask<V> SafeCall(K key, bool forceReCache = false, object option = null, Func<ValueTask<V>> Cal = null)
        {
            ///完成任务。
            async void ComplateSource(K key, TaskCompletionSource<V> source)
            {
                source.SetResult(await Cal.Invoke());
                locker.Remove(key);
            }

            if (CombineRecacheOnCalculate)
            {
                ///区分cal 是否同步完成。
                ///同步完成时，这时候第二个进入的调用，必然是多线程调用，因为当前线程正在执行Cal函数，
                ///那么第二个调用的callback可以不用保证在当前线程执行
                ///那么可以使用task.run 来SetResult，
                ///保证第二个调用的callback，不会插入到当前线程的第一次调用的执行callback之前。


                if (!locker.TryGetValue(key, out var source))
                {
                    source = new TaskCompletionSource<V>();
                    locker[key] = source;
                    ComplateSource(key, source);
                }

                return new ValueTask<V>(source.Task);
            }
            else
            {
                return Cal.Invoke();
            }
        }

    }
}
