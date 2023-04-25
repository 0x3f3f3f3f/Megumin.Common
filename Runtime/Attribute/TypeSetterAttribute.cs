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
    using System.Collections.Generic;
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

        class TypeItem : IComparable<TypeItem>
        {
            public Type Type { get; internal set; }
            public GUIContent GUIContent { get; internal set; }
            public string FirstC { get; internal set; }

            public int CompareTo(TypeItem other)
            {
                return GUIContent.text.CompareTo(other.GUIContent.text);
            }
        }

        public override Task<Type> GetResult(object options = null)
        {
            Source = new();

            if (Menu == null)
            {
                Menu = new GenericMenu();
                var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).ToList();
                List<TypeItem> typeItems = new();
                System.Random random = new();

                Task GTypeItem(int start, int count)
                {
                    return Task.Run(() =>
                    {
                        for (int i = start; i < start + count; i++)
                        {
                            if (i < types.Count)
                            {
                                var type = types[i];
                                TypeItem typeItem = new();
                                typeItem.Type = type;
                                typeItem.FirstC = type.Name[0].ToString().ToUpper();
                                //typeItem.GUIContent = new($"{typeItem.FirstC}/{type.Namespace}/{type.Name}");
                                typeItem.GUIContent = new($"{typeItem.FirstC}/{type.FullName?.Replace('.', '/')}");
                                typeItems.Add(typeItem);
                            }
                            else
                            {
                                break;
                            }
                        }
                    });
                }

                List<Task> tasks = new();

                const int batchCount = 2000;
                var batch = types.Count / batchCount + 1;
                for (int i = 0; i < batch; i++)
                {
                    tasks.Add(GTypeItem(i * batchCount, batchCount));
                }

                while (tasks.Any(elem => elem.IsCompleted == false))
                {
                    EditorUtility.DisplayProgressBar("CacheMenuItem", "Wait....", (float)typeItems.Count / types.Count);
                }

                //Task.WaitAll(tasks.ToArray());

                typeItems.Sort();
                for (int i = 0; i < typeItems.Count; i++)
                {
                    TypeItem item = typeItems[i];
                    if (item != null)
                    {
                        EditorUtility.DisplayProgressBar("CacheMenuItem", "AddItem....", (float)i / typeItems.Count);
                        Menu.AddItem(item.GUIContent, false, Callback, item.Type);
                    }
                }
            }

            EditorUtility.ClearProgressBar();
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

