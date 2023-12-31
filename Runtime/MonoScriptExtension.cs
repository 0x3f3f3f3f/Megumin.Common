using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Profiling;
using System.IO;


#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Megumin
{
    /// <summary>
    /// 类型->文件guid 的查找缓存。保存在EditorPrefs中。只能主线程访问。
    /// </summary>
    public class TypeGuidCache : AsyncCache<Type, string>
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
    }

#if UNITY_EDITOR

    /// <summary>
    /// MonoScript->text缓存，永远同步完成。只有主线程才能反序列化。缓存之后可以非主线程访问。
    /// </summary>
    public class MonoScriptCodeCache : AsyncDictionaryCache<MonoScript, string>
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
        public int Line { get; set; }

        public void FindLine()
        {
            if (string.IsNullOrEmpty(Code))
            {
                return;
            }

            if (Type == null)
            {
                return;
            }

            using (StringReader stringReader = new StringReader(Code))
            {
                string lineCode;
                int lineNumber = 1;//行号从1开始
                while ((lineCode = stringReader.ReadLine()) != null)
                {
                    if (lineCode.Contains($"class {Type.Name}"))
                    {
                        Line = lineNumber;
                        break;
                    }

                    if (stringReader.Peek() <= 0)
                    {
                        break;
                    }
                    lineNumber++;
                }
            }
        }
    }

    /// <summary>
    /// 类型->TypeMonoScriptPair缓存，用于快速通过Type找到Type所在的代码文件。
    /// <para/> 缓存查找 -> guid缓存查找并验证 -> 全局暴论搜索代码文本
    /// </summary>
    public class TypeScriptCache : AsyncDictionaryCache<Type, TypeMonoScriptPair>
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

        /// <summary>
        /// 看似是异步，其实所有步骤都是同步完成的。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="forceReCache"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public override async ValueTask<TypeMonoScriptPair> Calculate(Type key, bool forceReCache, object option = null)
        {
            //Debug.LogError($"{Time.frameCount} GetMonoScript2   {key.Name}");
            TypeMonoScriptPair result = new();
            result.Type = key;

            var (script, code) = await GetValidMonoScript(key);

            if (script)
            {
                result.Code = code;
                result.MonoScript = script;

                //更新guid到TypeGuidCache中
                var path = AssetDatabase.GetAssetPath(script);
                var newGUID = AssetDatabase.AssetPathToGUID(path);
                result.GUID = newGUID;
                MonoScriptExtension_5723CD2B78954C329E0643673F68FE22.TypeGuidCache.UpdateCache(key, newGUID);

                //异步计算行号
                Task.Run(result.FindLine);

                return result;
            }
            else
            {
                Debug.LogError("No match script");
                return null;
            }
        }

        public async Task<(MonoScript script, string code)> GetValidMonoScript(Type key)
        {
            MonoScript script = null;
            string code = null;

            //通过GUID缓存找到MonoScript
            var guid = await MonoScriptExtension_5723CD2B78954C329E0643673F68FE22.TypeGuidCache.Get(key);
            if (!string.IsNullOrEmpty(guid))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);

                if (script)
                {
                    code = await MonoScriptExtension_5723CD2B78954C329E0643673F68FE22.MonoScriptCodeCache.Get(script);
                    //异步验证脚本是否真的包含指定类型
                    //下面可能会调用AssetDatabase.GetAssetPath，需要保证后续主线程调用。
                    var success = Valid(code, key); //await Task.Run(() => { return Valid(result.Code, key); }).ConfigureAwait(false);
                    if (success)
                    {
                        return (script, code);
                    }
                }
            }


            //暴力遍历所有MonoScript，找到含有Type的脚本。
            //Debug.LogError($"{Time.frameCount} GetMonoScript3   {key.Name}");

            var list = MonoScriptExtension_5723CD2B78954C329E0643673F68FE22.GetAllMonoScripts();
            foreach (var item in list)
            {
                code = await MonoScriptExtension_5723CD2B78954C329E0643673F68FE22.MonoScriptCodeCache.Get(item);
                var success = Valid(code, key); // await Task.Run(() => { return Valid(code, key); });
                if (success)
                {
                    script = item;
                    return (script, code);
                }
            }

            return (script, code);
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

        /// <summary>
        /// MonoScript->text缓存，永远同步完成。只有主线程才能反序列化。缓存之后可以非主线程访问。
        /// </summary>
        /// <remarks>
        /// 不要怕缓存了所有代码字符串会占用很多内存。只是在编辑器下有效，代码文件纯文本最多也就几百mb，完全不是问题。
        /// </remarks>
        public static MonoScriptCodeCache MonoScriptCodeCache { get; } = new();
        public static TypeScriptCache TypeScriptCache { get; } = new();

        /// <summary>
        /// 通过类型获取定义此类型的MonoScript对象
        /// </summary>
        /// <param name="type"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        /// <remarks>
        /// 尽可能的试用await 而不是访问Result,极有可能导致死锁。
        /// </remarks>
        public static async ValueTask<MonoScript> GetMonoScript(this Type type, bool force = false)
        {
            if (type is null)
            {
                return null;
            }

            //Debug.LogError($"{Time.frameCount} GetMonoScript1   {type.Name}");
            var result = await TypeScriptCache.Get(type, force).ConfigureAwait(false);
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
            if (type is null)
            {
                return;
            }

            //Debug.LogError($"{Time.frameCount} GetMonoScript1   {type.Name}");
            var result = await TypeScriptCache.Get(type).ConfigureAwait(false);

            if (result.MonoScript)
            {
                AssetDatabase.OpenAsset(result.MonoScript, result.Line, 0);
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



