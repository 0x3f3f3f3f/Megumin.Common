using System;
using System.Linq;
using System.Threading.Tasks;
using Megumin;
using UnityEngine;

namespace Megumin
{
    public class TypeSetterAttribute : PropertyAttribute
    {
    }
}

#if UNITY_EDITOR

namespace UnityEditor.Megumin
{
    using UnityEditor;

#if !DISABLE_MEGUMIN_PROPERTYDRWAER
    [UnityEditor.CustomPropertyDrawer(typeof(TypeSetterAttribute))]
#endif
    public class TypeSetterAttributeDrawer : UnityEditor.PropertyDrawer
    {
        public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
        {
            return UnityEditor.EditorGUI.GetPropertyHeight(property, true);
        }

        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == UnityEditor.SerializedPropertyType.String)
            {
                var propertyPosition = position;
                propertyPosition.width -= 86;

                var buttonPosition = position;
                buttonPosition.width = 80;
                buttonPosition.x += position.width - 80;

                UnityEditor.EditorGUI.BeginProperty(position, label, property);
                UnityEditor.EditorGUI.PropertyField(propertyPosition, property, label, true);
                UnityEditor.EditorGUI.EndProperty();

                var click = GUI.Button(buttonPosition, "Select");

                if (GetTypeHelper.Instance.TryGetResultOnGUI(property.propertyPath, click, out var type))
                {
                    property.stringValue = type.FullName;
                }
            }
            else
            {
                UnityEditor.EditorGUI.PropertyField(position, property, label, true);
            }
        }
    }


    public class GetTypeHelper : IMGUIAsyncHelper<Type>
    {
        public static readonly GetTypeHelper Instance = new();

        public GenericMenu Menu { get; private set; }
        public TaskCompletionSource<Type> Source { get; private set; }

        public override Task<Type> GetResult(object options = null)
        {
            Source = new();

            if (Menu == null)
            {
                Menu = new GenericMenu();

                var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).ToList();
                foreach (var type in types)
                {
                    Menu.AddItem(new GUIContent(type.FullName), false, Callback, type);
                }
            }

            Menu.ShowAsContext();
            return Source.Task;
        }

        private void Callback(object userData)
        {
            Source?.TrySetResult(userData as Type);
        }
    }
}

#endif

