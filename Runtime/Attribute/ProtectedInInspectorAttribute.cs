using System;
using Megumin;
using UnityEngine;

namespace Megumin
{
    /// <summary>
    /// 在检视面板中保护,只有先勾选才能修改
    /// </summary>
    /// [Obsolete("与Enable<T> 显示相同，可能引起歧义。TODO换一种显示UI")]
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public sealed class ProtectedInInspectorAttribute : PropertyAttribute
    {

    }

#if UNITY_EDITOR

#if !DISABLE_MEGUMIN_PROPERTYDRWAER
    [UnityEditor.CustomPropertyDrawer(typeof(ProtectedInInspectorAttribute))]
#endif
    public class ProtectedInInspectorAttributeDrawer : UnityEditor.PropertyDrawer
    {
        public bool check = false;

        public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
        {
            return UnityEditor.EditorGUI.GetPropertyHeight(property);
        }

        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            var togglePosition = position;
            togglePosition.x += togglePosition.width - 14;
            togglePosition.width = 16;

            var valuePosition = position;
            valuePosition.width -= 20;

            check = GUI.Toggle(togglePosition, check, GUIContent.none);

            if (check)
            {
                UnityEditor.EditorGUI.PropertyField(valuePosition, property, label, true);
            }
            else
            {
                using (new UnityEditor.EditorGUI.DisabledGroupScope(true))
                {
                    UnityEditor.EditorGUI.PropertyField(valuePosition, property, label, true);
                }
            }
        }
    }

#endif
}


