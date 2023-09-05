using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Megumin
{
    /// <summary>
    /// 仅保证一个异步任务执行期间在次被调用，返回相同的任务，不多次调用。
    /// 任务完成后，可以再次被调用。
    /// 
    /// 
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

        public ValueTask<V> SafeCall(K key, Func<ValueTask<V>> Cal = null)
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


        Dictionary<K,ValueTask<V>> already = new Dictionary<K, ValueTask<V>>();
        public ValueTask<V> SafeCall2(K key, Func<ValueTask<V>> Cal = null)
        {
            if (already.TryGetValue(key,out var task))
            {
                return task;
            }
            else
            {
                task = Cal.Invoke();
                RemoveKey(key, task);
                return task;    
            }

            async void RemoveKey(K key, ValueTask<V> task)
            {
                //Task.Run( SetResult );
                await task.ConfigureAwait(false);
                already.Remove(key);
            }
        }

        //Cal 三种情况，
        //同步函数
        //异步函数同步完成
        //异步函数挂起


        //Cal函数执行期间，第二次调用来自哪个线程
        //与第一次调用相同线程，
        //与第一次调用不同线程，

        //组合有6种情况，有些情况不存在，有些情况不需要保证callback顺序。
        //同步函数           相同线程   不存在
        //异步函数同步完成   相同线程   不存在
        //异步函数挂起       相同线程   需要保证callback顺序
        //同步函数           不同线程   不保证顺序
        //异步函数同步完成   不同线程   不保证顺序
        //异步函数挂起       不同线程   需要保证callback顺序


        //Cal 三种情况，可以总结为
        //第二次调用发生在  Cal同步执行时，还是 函数挂起时。
        //同步执行时，肯定时多线程，挂起时可能时多线程，也可能是同一个线程。
        //同步执行时，需要创建一个source，通过source创建task返回。
        //函数一旦挂起，证明Cal的结果Task可以存在，直接返回即可。


        //Cal结果的Task 缓存 和新创建的Source缓存， 有Task返回task，无task 返回通过Source创建的task。
        //使用source的 肯定都是多线程的，不需要保证callback执行顺序，
        //使用task.run 执行SetResult，保证不插入第二次的callback到第一次的callback执行前
    }
}
