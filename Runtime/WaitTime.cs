using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Timers;

namespace Megumin.Timers
{
    public enum UntiyTimeType
    {
        GameTime,
        UnscaledTime,
        Realtime,
    }

    public interface IWaitTimeable<T>
    {
        T Now { get; }
        /// <summary>
        /// 开始等待的时间戳
        /// </summary>
        T StartTimestamp { get; }
        T GetLeftTime(T waitTime);
        bool WaitStart();
        /// <summary>
        /// 实时调用WaitEnd，在等待期间waitTime可能改变，所以在结束检测时传入waitTime参数。
        /// </summary>
        /// <param name="waitTime"></param>
        /// <returns></returns>
        bool WaitEnd(T waitTime);
    }

    public abstract class UnityWaitTime<T> : IWaitTimeable<T>
    {
        public T StartTimestamp { get; protected set; }
        public bool WaitStart()
        {
            StartTimestamp = Now;
            return true;
        }

        public abstract T Now { get; }

        public abstract T GetLeftTime(T waitTime);
        public abstract bool WaitEnd(T waitTime);
    }

    public class WaitGameTime : UnityWaitTime<double>
    {
        public override bool WaitEnd(double waitTime)
        {
            return GetLeftTime(waitTime) <= 0;
        }

        public override double GetLeftTime(double waitTime)
        {
            var left = waitTime - (Now - StartTimestamp);
            return left;
        }

        public override double Now => UnityEngine.Time.timeAsDouble;
    }

    public class WaitUnscaledTime : UnityWaitTime<double>
    {
        public override bool WaitEnd(double waitTime)
        {
            return GetLeftTime(waitTime) <= 0;
        }

        public override double GetLeftTime(double waitTime)
        {
            var left = waitTime - (Now - StartTimestamp);
            return left;
        }

        public override double Now => UnityEngine.Time.unscaledTimeAsDouble;
    }

    public class WaitRealtime : UnityWaitTime<double>
    {
        public override bool WaitEnd(double waitTime)
        {
            return GetLeftTime(waitTime) <= 0;
        }

        public override double GetLeftTime(double waitTime)
        {
            var left = waitTime - (Now - StartTimestamp);
            return left;
        }

        public override double Now => UnityEngine.Time.realtimeSinceStartupAsDouble;
    }
}


