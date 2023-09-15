using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Megumin
{
    /// <summary>
    /// StringBuilder 无法判断是否含有宏。string每个宏替换后都会生成新的string。
    /// </summary>
    public static class UnityMacro
    {
        //常用UnityGameObject宏
        public const string name = "$(name)";

        public const string layer = "$(layer)";
        public const string tag = "$(tag)";

        public const string position = "$(position)";
        public const string rotation = "$(rotation)";
        public const string eulerAngles = "$(eulerAngles)";
        public const string lossyScale = "$(lossyScale)";

        public const string localPosition = "$(localPosition)";
        public const string localRotation = "$(localRotation)";
        public const string localEulerAngles = "$(localEulerAngles)";
        public const string localScale = "$(localScale)";

        public const string parent = "$(parent)";

        /// <summary>
        /// 替换字符串中的unity对象宏
        /// </summary>
        /// <param name="original"></param>
        /// <param name="obj"></param>
        /// <param name="stringBuilder"></param>
        /// <returns></returns>
        public static StringBuilder MacroUnityObject(this string original, UnityEngine.Object obj, StringBuilder stringBuilder = null)
        {
            if (stringBuilder == null)
            {
                stringBuilder = new StringBuilder();
                stringBuilder.Append(original);
            }

            return MacroUnityObject(stringBuilder, obj, original);
        }

        public static StringBuilder MacroUnityObject(this StringBuilder stringBuilder, UnityEngine.Object obj, string original = null)
        {
            //先判断是否含有宏，避免不必要的生成新字符串
            if (string.IsNullOrEmpty(original))
            {
                original = stringBuilder.ToString();
            }

            if (string.IsNullOrEmpty(original) == false && obj)
            {
                if (original.Contains(name))
                {
                    stringBuilder = stringBuilder.Replace(name, obj.name);
                }

                GameObject targetGameObject = null;
                Transform targetTransform = null;
                if (obj is Component component)
                {
                    targetGameObject = component.gameObject;
                    targetTransform = targetGameObject.transform;

                }
                else if (obj is GameObject gameObject)
                {
                    targetGameObject = gameObject;
                    targetTransform = gameObject.transform;
                }
                else if (obj is Transform transform)
                {
                    targetGameObject = transform.gameObject;
                    targetTransform = transform;
                }

                if (targetTransform)
                {
                    stringBuilder = MacroTransform(stringBuilder, targetTransform, original);
                }

                if (targetGameObject)
                {
                    stringBuilder = MacroGameObject(stringBuilder, targetGameObject, original);
                }
            }

            return stringBuilder;
        }

        /// <summary>
        /// 建议使用 <see cref="MacroUnityObject(StringBuilder, UnityEngine.Object, string)"/>
        /// </summary>
        /// <param name="stringBuilder"></param>
        /// <param name="transform"></param>
        /// <param name="original"></param>
        /// <returns></returns>
        public static StringBuilder MacroTransform(this StringBuilder stringBuilder, Transform transform, string original = null)
        {
            //先判断是否含有宏，避免不必要的生成新字符串
            if (string.IsNullOrEmpty(original))
            {
                original = stringBuilder.ToString();
            }

            // world space 
            if (original.Contains(position))
            {
                stringBuilder = stringBuilder.Replace(position, transform.position.ToString());
            }

            if (original.Contains(rotation))
            {
                stringBuilder = stringBuilder.Replace(rotation, transform.rotation.ToString());
            }

            if (original.Contains(eulerAngles))
            {
                stringBuilder = stringBuilder.Replace(eulerAngles, transform.eulerAngles.ToString());
            }

            if (original.Contains(lossyScale))
            {
                stringBuilder = stringBuilder.Replace(lossyScale, transform.lossyScale.ToString());
            }

            //relative to the GameObjects parent.
            if (original.Contains(localPosition))
            {
                stringBuilder = stringBuilder.Replace(localPosition, transform.localPosition.ToString());
            }

            if (original.Contains(localRotation))
            {
                stringBuilder = stringBuilder.Replace(localRotation, transform.localRotation.ToString());
            }

            if (original.Contains(localEulerAngles))
            {
                stringBuilder = stringBuilder.Replace(localEulerAngles, transform.localEulerAngles.ToString());
            }

            if (original.Contains(localScale))
            {
                stringBuilder = stringBuilder.Replace(localScale, transform.localScale.ToString());
            }

            if (original.Contains(parent))
            {
                stringBuilder = stringBuilder.Replace(parent, transform.parent?.name);
            }

            return stringBuilder;
        }

        /// <summary>
        /// 建议使用 <see cref="MacroUnityObject(StringBuilder, UnityEngine.Object, string)"/>
        /// </summary>
        /// <param name="stringBuilder"></param>
        /// <param name="gameObject"></param>
        /// <param name="original"></param>
        /// <returns></returns>
        public static StringBuilder MacroGameObject(this StringBuilder stringBuilder, GameObject gameObject, string original = null)
        {
            //先判断是否含有宏，避免不必要的生成新字符串
            if (string.IsNullOrEmpty(original))
            {
                original = stringBuilder.ToString();
            }

            if (original.Contains(layer))
            {
                stringBuilder = stringBuilder.Replace(layer, LayerMask.LayerToName(gameObject.layer));
            }

            if (original.Contains(tag))
            {
                stringBuilder = stringBuilder.Replace(tag, gameObject.tag);
            }

            return stringBuilder;
        }

        static void Test()
        {
            GameObject gameObject = new GameObject();
            string a = "a";
            StringBuilder stringBuilder = new StringBuilder();

            a = stringBuilder.MacroGameObject(gameObject, a).MacroTransform(gameObject.transform).ToString();

            a = a.MacroUnityObject(gameObject).ToString();
        }
    }
}



