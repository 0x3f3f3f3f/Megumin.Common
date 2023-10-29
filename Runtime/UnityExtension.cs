using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Networking;
using System.IO;
using Megumin;
using System.Runtime.CompilerServices;
using UnityEngine.Profiling;
using System.Runtime.InteropServices;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Megumin
{
    /// <summary>
    /// 常用扩展
    /// </summary>
    public static class UnityExtension_765D8FDEA6B04280BDF031886E30566C
    {
        static MethodInfo OpenPropertyEditorMethod;

        /// <summary>
        /// 通过反射打开属性面板
        /// </summary>
        /// <param name="object"></param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void OpenPropertyEditor(this UnityEngine.Object @object)
        {
#if UNITY_EDITOR

            if (OpenPropertyEditorMethod == null)
            {
                var ab = Assembly.GetAssembly(typeof(EditorWindow));
                var propertyEditor = ab.GetType("UnityEditor.PropertyEditor");
                Type[] types = new Type[] { typeof(UnityEngine.Object), typeof(bool) };
                OpenPropertyEditorMethod = propertyEditor.GetMethod("OpenPropertyEditor",
                                                                    BindingFlags.NonPublic | BindingFlags.Static,
                                                                    null,
                                                                    types,
                                                                    null);
            }

            if (@object)
            {
                OpenPropertyEditorMethod?.Invoke(null, new object[] { @object, true });
            }
#endif
        }
    }
}



