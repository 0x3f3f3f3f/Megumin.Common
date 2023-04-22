using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Megumin
{
    public class UnityTraceListener : TraceListener
    {
        [HideInCallstack]
        public override void Write(string message)
        {
            Debug.Log(message);
        }

        [HideInCallstack]
        public override void Write(object o, string category)
        {
            if (category.Contains("Warning", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning(o);
            }
            else
            {
                base.Write(o, category);
            }
        }

        [HideInCallstack]
        public override void Write(string message, string category)
        {
            if (category.Contains("Warning", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning(message);
            }
            else
            {
                base.Write(message, category);
            }
        }

        [HideInCallstack]
        public override void WriteLine(string message)
        {
            Debug.Log(message);
        }

        [HideInCallstack]
        public override void WriteLine(object o, string category)
        {
            if (category.Contains("Warning", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning(o);
            }
            else
            {
                base.WriteLine(o, category);
            }
        }

        [HideInCallstack]
        public override void WriteLine(string message, string category)
        {
            if (category.Contains("Warning", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning(message);
            }
            else
            {
                base.WriteLine(message, category);
            }
        }

        [HideInCallstack]
        public override void Fail(string message, string detailMessage)
        {
            Debug.LogError($"{message}\n{detailMessage}");
        }

        [HideInCallstack]
        public override void Fail(string message)
        {
            Debug.LogError(message);
        }
    }
}

#if UNITY_2021_3_OR_NEWER
//Unity 内置
#else

namespace UnityEngine
{
    //
    // 摘要:
    //     Marks the methods you want to hide from the Console window callstack. When you
    //     hide these methods they are removed from the detail area of the selected message
    //     in the Console window.
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HideInCallstackAttribute : Attribute
    {
    }
}

#endif


