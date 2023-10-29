// <author>
//   KumoKyaku: https://github.com/KumoKyaku
//
//   Original author: HiddenMonk & Johannes Deml
//   Ref: http://answers.unity3d.com/questions/627090/convert-serializedproperty-to-custom-class.html
//
//   Original author: douduck08: https://github.com/douduck08
//   https://gist.github.com/douduck08/6d3e323b538a741466de00c30aa4b61f
//   Use Reflection to get instance of Unity's SerializedProperty in Custom Editor.
//   Modified codes from 'Unity Answers', in order to apply on nested List<T> or Array. 
// </author>

#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Megumin
{
    public static class SerializedPropertyExtensions_B4C8CAE8AAFD410981DE4CCC2553F15F
    {
        public static readonly Regex GetIndexRegex = new(@"(?<list>.*)\[(?<index>.+)\]$");

        /// <summary>
        /// 获取值的所属对象
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static object GetOwner(this SerializedProperty property)
        {
            object obj = property.serializedObject.targetObject;
            string path = property.propertyPath.Replace(".Array.data", "");
            string[] fieldStructure = path.Split('.');

            for (int i = 0; i < fieldStructure.Length - 1; i++)
            {
                string name = fieldStructure[i];
                if (name.Contains("["))
                {
                    var match = GetIndexRegex.Match(name);
                    if (match.Success)
                    {
                        var filedName = match.Groups["list"].Value;
                        int index = System.Convert.ToInt32(match.Groups["index"].Value);
                        obj = GetFieldValueWithIndex(filedName, obj, index);
                    }
                    else
                    {
                        throw new Exception("Index Match Faild.");
                    }
                }
                else
                {
                    obj = GetFieldValue(name, obj);
                }
            }
            return obj;
        }

        /// <summary>
        /// 获取值的所属对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool TryGetOwner<T>(this SerializedProperty property, out T obj)
        {
            var owner = GetOwner(property);
            if (owner is T tObj)
            {
                obj = tObj;
                return true;
            }

            obj = default;
            return false;
        }

        public static T GetValue<T>(this SerializedProperty property) where T : class
        {
            object obj = property.serializedObject.targetObject;
            string path = property.propertyPath.Replace(".Array.data", "");
            string[] fieldStructure = path.Split('.');

            for (int i = 0; i < fieldStructure.Length; i++)
            {
                string name = fieldStructure[i];
                if (name.Contains("["))
                {
                    var match = GetIndexRegex.Match(name);
                    if (match.Success)
                    {
                        var filedName = match.Groups["list"].Value;
                        int index = System.Convert.ToInt32(match.Groups["index"].Value);
                        obj = GetFieldValueWithIndex(filedName, obj, index);
                    }
                    else
                    {
                        throw new Exception("Index Match Faild.");
                    }
                }
                else
                {
                    obj = GetFieldValue(name, obj);
                }
            }
            return (T)obj;
        }

        public static bool SetValue<T>(this SerializedProperty property, T value) where T : class
        {
            object obj = property.serializedObject.targetObject;
            string path = property.propertyPath.Replace(".Array.data", "");
            string[] fieldStructure = path.Split('.');

            obj = GetOwner(property);

            string name = fieldStructure.Last();
            if (name.Contains("["))
            {
                var match = GetIndexRegex.Match(name);
                if (match.Success)
                {
                    var filedName = match.Groups["list"].Value;
                    int index = System.Convert.ToInt32(match.Groups["index"].Value);
                    return SetFieldValueWithIndex(filedName, obj, index, value);
                }
                else
                {
                    throw new Exception("Index Match Faild.");
                }
            }
            else
            {
                return SetFieldValue(name, obj, value);
            }
        }

        private static object GetFieldValue(string fieldName, object obj, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, bindings);
            if (field != null)
            {
                return field.GetValue(obj);
            }
            return default(object);
        }

        private static object GetFieldValueWithIndex(string fieldName, object obj, int index, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, bindings);
            if (field != null)
            {
                object list = field.GetValue(obj);
                if (list.GetType().IsArray)
                {
                    return ((object[])list)[index];
                }
                else if (list is IEnumerable)
                {
                    return ((IList)list)[index];
                }
            }
            return default(object);
        }

        public static bool SetFieldValue(string fieldName, object obj, object value, bool includeAllBases = false, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, bindings);
            if (field != null)
            {
                field.SetValue(obj, value);
                return true;
            }
            return false;
        }

        public static bool SetFieldValueWithIndex(string fieldName, object obj, int index, object value, bool includeAllBases = false, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, bindings);
            if (field != null)
            {
                object list = field.GetValue(obj);
                if (list.GetType().IsArray)
                {
                    ((object[])list)[index] = value;
                    return true;
                }
                else if (list is IEnumerable)
                {
                    ((IList)list)[index] = value;
                    return true;
                }
            }
            return false;
        }
    }
}

#endif

