using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Megumin;
using System.IO;

namespace Megumin
{
    public class UnityPackageNameAttribute : PropertyAttribute
    {
        /// <summary>
        /// 设置包含的 <see cref=" UnityEditor.PackageManager.PackageSource"/>
        /// </summary>
        public string[] Source { get; set; }
    }
}

#if UNITY_EDITOR

namespace UnityEditor.Megumin
{
    using System.Linq;
    using UnityEditor;
    using static UnityEditor.EditorGUI;

#if !DISABLE_MEGUMIN_PROPERTYDRWAER
    [CustomPropertyDrawer(typeof(UnityPackageNameAttribute))]
#endif
    internal sealed class UnityPackageNameAttributeDrawer : PropertyDrawer
    {
        GUIContent[] displayedOptions = null;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                UnityPackageNameAttribute attri = attribute as UnityPackageNameAttribute;
                if (property.propertyType == SerializedPropertyType.String)
                {
                    //绘制选项菜单
                    var index = 0;
                    var currentValue = property.stringValue;

                    if (displayedOptions == null)
                    {
                        var infos = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();

                        if (attri.Source != null)
                        {
                            infos = infos.Where(elem => attri.Source.Contains(elem.source.ToString())).ToArray();
                        }

                        displayedOptions = new GUIContent[infos.Count() + 1];
                        displayedOptions[0] = new("string.Empty");
                        for (int i = 0; i < displayedOptions.Length - 1; i++)
                        {
                            string name = infos[i].name;
                            displayedOptions[i + 1] = new(name);
                            if (currentValue == name)
                            {
                                index = i;
                            }
                        }
                    }
                    else
                    {
                        for (var i = 0; i < displayedOptions.Length; i++)
                        {
                            if (currentValue == displayedOptions[i]?.text)
                            {
                                index = i;
                            }
                        }
                    }

                    EditorGUI.BeginChangeCheck();
                    //绘制选项菜单
                    index = EditorGUI.Popup(position, label, index, displayedOptions);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (index == 0)
                        {
                            property.stringValue = "";
                        }
                        else
                        {
                            property.stringValue = displayedOptions[index].text;
                        }
                    }
                }
                else
                {
                    //fallback 绘制
                    EditorGUI.PropertyField(position, property, label, true);
                }
            }
        }
    }
}

#endif

