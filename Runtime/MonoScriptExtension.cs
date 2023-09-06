using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Profiling;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Megumin
{
    public class TypeGuidCache : Cache<Type, string>
    {
        public override bool TryGetCache(in Type key, out string value, object option = null)
        {

#if UNITY_EDITOR
            value = EditorPrefs.GetString(key.FullName, null);
            return true;
#else
            value = null;
            return false;
#endif

        }

        public override ValueTask<string> Calculate(Type key, bool forceReCache = false, object option = null)
        {
            return new ValueTask<string>(result: null);
        }

        public override void UpdateCache(in Type key, string value, bool forceReCache = false, object option = null)
        {

#if UNITY_EDITOR
            EditorPrefs.SetString(key.FullName, value);
#endif

        }

        public override bool ClearCache()
        {
            return true;
        }
    }

#if UNITY_EDITOR

    public class MonoScriptCodeCache : DictionaryCache<MonoScript, string>
    {
        public override ValueTask<string> Calculate(MonoScript key, bool forceReCache, object option = null)
        {
            //Debug.LogError($"{Time.frameCount} GetMonoScript4   {key.name}");
            return new ValueTask<string>(result: key.text);
        }
    }

    public class TypeMonoScriptPair
    {
        public Type Type { get; set; }
        public string GUID { get; set; }
        public string Code { get; set; }
        public MonoScript MonoScript { get; set; }
    }

    public class TypeScriptCache : DictionaryCache<Type, TypeMonoScriptPair>
    {
        public static bool Valid(string code, Type type)
        {
            if (type == null)
            {
                return false;
            }

            if (code.Contains($"class {type.Name}"))
            {
                if (string.IsNullOrEmpty(type.Namespace))
                {
                    return true;
                }
                else
                {
                    if (code.Contains(type.Namespace))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool TryGetCache(in Type key, out TypeMonoScriptPair value, object option = null)
        {
            if (CacheDic.TryGetValue(key, out value))
            {
                return Valid(value.Code, key);
            }
            return false;
        }

        public override async ValueTask<TypeMonoScriptPair> Calculate(Type key, bool forceReCache, object option = null)
        {
            //Debug.LogError($"{Time.frameCount} GetMonoScript2   {key.Name}");
            TypeMonoScriptPair result = new();
            MonoScript script = null;

            result.Type = key;

            //通过GUID缓存找到MonoScript
            var guid = await MonoScriptExtension_5723CD2B78954C329E0643673F68FE22.TypeGuidCache.Get(key);
            if (!string.IsNullOrEmpty(guid))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);

                if (script)
                {
                    result.GUID = guid;
                    result.Code = await MonoScriptExtension_5723CD2B78954C329E0643673F68FE22.MonoScriptCodeCache.Get(script);
                    result.MonoScript = script;
                    //异步验证脚本是否真的包含指定类型
                    var success = await Task.Run(() => { return Valid(result.Code, key); });
                    if (success)
                    {
                        return result;
                    }
                }
            }


            //暴力遍历所有MonoScript，找到含有Type的脚本。
            //Debug.LogError($"{Time.frameCount} GetMonoScript3   {key.Name}");

            var list = MonoScriptExtension_5723CD2B78954C329E0643673F68FE22.GetAllMonoScripts();
            foreach (var item in list)
            {
                var code = await MonoScriptExtension_5723CD2B78954C329E0643673F68FE22.MonoScriptCodeCache.Get(item);
                var success = Valid(code, key); // await Task.Run(() => { return Valid(code, key); });
                if (success)
                {
                    script = item;
                    break;
                }
            }

            if (script)
            {
                result.Code = await MonoScriptExtension_5723CD2B78954C329E0643673F68FE22.MonoScriptCodeCache.Get(script);
                result.MonoScript = script;
                var path = AssetDatabase.GetAssetPath(script);
                var newGUID = AssetDatabase.AssetPathToGUID(path);
                result.GUID = newGUID;
                MonoScriptExtension_5723CD2B78954C329E0643673F68FE22.TypeGuidCache.UpdateCache(key, newGUID);
                return result;
            }
            else
            {
                Debug.LogError("No match script");
                return null;
            }
        }
    }

#endif

    public static class MonoScriptExtension_5723CD2B78954C329E0643673F68FE22
    {
        public static TypeGuidCache TypeGuidCache { get; } = new();

#if UNITY_EDITOR

        private static List<MonoScript> AllMonoScript { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="force"></param>
        /// <returns></returns>
        /// <remarks>
        /// 只能主线程调用
        /// </remarks>
        public static List<MonoScript> GetAllMonoScripts(bool force = false)
        {
            if (AllMonoScript == null || force)
            {
                Profiler.BeginSample("CacheAllMonoScripts");

                AllMonoScript = new();
                //Debug.LogError($"FrameCount:{Time.frameCount}    AssetDatabase.FindAssets AllMonoScripts");

                var scriptGUIDs = AssetDatabase.FindAssets($"t:script");

                foreach (var scriptGUID in scriptGUIDs)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(scriptGUID);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                    AllMonoScript.Add(script);
                }

                Profiler.EndSample();
            }

            return AllMonoScript;
        }

        public static MonoScriptCodeCache MonoScriptCodeCache { get; } = new();
        public static TypeScriptCache TypeScriptCache { get; } = new();

        /// <summary>
        /// 通过类型获取定义此类型的MonoScript对象
        /// </summary>
        /// <param name="type"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        public static async ValueTask<MonoScript> GetMonoScript(this Type type, bool force = false)
        {
            if (type is null)
            {
                return null;
            }

            //Debug.LogError($"{Time.frameCount} GetMonoScript1   {type.Name}");
            var result = await TypeScriptCache.Get(type, force);
            return result.MonoScript;
        }

#endif

        /// <summary>
        /// 打开类型所在的脚本
        /// </summary>
        /// <param name="type"></param>
        public static async void OpenScript(this Type type)
        {

#if UNITY_EDITOR
            var obj = await GetMonoScript(type);
            if (obj)
            {
                AssetDatabase.OpenAsset(obj, 0, 0);
            }
#endif

        }

        /// <summary>
        /// 在unity编辑中选中类型所在的脚本
        /// </summary>
        /// <param name="type"></param>
        public static async void SelectScript(this Type type)
        {

#if UNITY_EDITOR
            var obj = await GetMonoScript(type);
            if (obj)
            {
                Selection.activeObject = obj;
            }
#endif
        }

    }

}



