using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Megumin;
using System;
using System.Linq;

namespace Megumin
{
    /// <summary>
    /// <see cref="RefSetterAttribute"/> 通过此接口获得所有可设置的引用集合
    /// </summary>
    public interface IRefSetterCollection
    {
        ///// <summary>
        ///// 用于更新缓存
        ///// </summary>
        //string DataVersion { get; }

        IEnumerable<(string OptionDisplay, object Value)> GetRefObjs(string filter = null, string[] category = null, Type[] type = null);

        public IEnumerable<(string OptionDisplay, object Value)> GetRefObjs(RefSetterAttribute refSetterAttribute)
        {
            return this.GetRefObjs(refSetterAttribute.Filter, refSetterAttribute.Category, refSetterAttribute.Type);
        }

        /// <summary>
        /// 从引用对象角度判断两个对象是否相等。
        /// 用于GUID判断，重新序列化可能导致创建不同实例。
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        bool EqualsRef(object lhs, object rhs);
    }

    /// <summary>
    /// 设置引用对象。序列化对象的根对象需要实现<see cref="IRefSetterCollection"/>
    /// </summary>
    public class RefSetterAttribute : PropertyAttribute
    {
        public string Filter { get; set; }
        public string[] Category { get; set; }
        public Type[] Type { get; set; }

        /// <summary>
        /// 是否扩展显示引用对象
        /// </summary>
        public bool CanExpand { get; set; } = false;
        /// <summary>
        /// 引用对象是不是只读
        /// </summary>
        public bool ReadOnly { get; set; } = false;
    }
}

#if UNITY_EDITOR

namespace UnityEditor.Megumin
{
    //#if !DISABLE_MEGUMIN_PROPERTYDRWAER
    [CustomPropertyDrawer(typeof(RefSetterAttribute))]
    //#endif
    internal sealed class RefSetterAttributeDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attri = attribute as RefSetterAttribute;
            if (attri.CanExpand)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
            else
            {
                return base.GetPropertyHeight(property, label);
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                var owner = property.serializedObject.targetObject;
                if (owner is IRefSetterCollection refFinder && OnGUISelector(position, property, label, refFinder))
                {
                    //OnGUISelector绘制选项菜单
                }
                else
                {
                    //fallback 绘制
                    EditorGUI.PropertyField(position, property, label, true);
                }
            }
        }

        /// <summary>
        /// 使用缓存，必要每次都获取选项，开销很大
        /// </summary>
        Dictionary<string, (string[] Options, object[] Values)> cache = new();

        public bool OnGUISelector(Rect position, SerializedProperty property, GUIContent label, IRefSetterCollection refFinder)
        {
            var attri = attribute as RefSetterAttribute;

            EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var index = 0;
            var currentValue = property.GetValue<object>();
            string[] options = null;
            object[] values = null;

            if (cache.TryGetValue(property.propertyPath, out var res))
            {
                //从缓存中获取可设置对象
                options = res.Options;
                values = res.Values;

                //搜索当前选项的index。
                var collectionIndex = 0;
                foreach (var value in values)
                {
                    if (refFinder.EqualsRef(currentValue, value))
                    {
                        index = collectionIndex;
                    }
                    collectionIndex++;
                }
            }
            else
            {
                //制作缓存
                var collection = refFinder.GetRefObjs(attri);
                if (collection == null)
                {
                    return false;
                }

                var optionCount = collection.Count();
                options = new string[optionCount + 1];
                options[0] = "Null";
                values = new object[optionCount + 1];
                values[0] = null;

                var collectionIndex = 1;
                foreach (var (OptionDisplay, Value) in collection)
                {
                    options[collectionIndex] = OptionDisplay;
                    values[collectionIndex] = Value;

                    //搜索当前选项的index。
                    if (refFinder.EqualsRef(currentValue, Value))
                    {
                        index = collectionIndex;
                    }
                    collectionIndex++;
                }

                cache[property.propertyPath] = (options, values);
            }

            var indentOffset = EditorGUI.indentLevel * 4;
            float popupWidth = position.xMax - EditorGUIUtility.labelWidth - 1 - indentOffset;
            var selectPopupPos = position;
            selectPopupPos.width = popupWidth;
            selectPopupPos.x = position.xMax - popupWidth;
            selectPopupPos.height = 18;

            EditorGUI.BeginChangeCheck();
            //绘制选项菜单
            index = EditorGUI.Popup(selectPopupPos, index, options);
            if (EditorGUI.EndChangeCheck())
            {
                //var obj = property.GetValue<object>();

                //Todo 这里Undo会导致编辑器错乱bug
                //Undo.RecordObject(property.serializedObject.targetObject, "Change Ref");
                property.SetValue<object>(values[index]);
            }

            if (attri.CanExpand)
            {
                using (new EditorGUI.DisabledGroupScope(attri.ReadOnly))
                {
                    EditorGUI.PropertyField(position, property, GUIContent.none, true);
                }
            }

            return true;
        }
    }
}

#endif
