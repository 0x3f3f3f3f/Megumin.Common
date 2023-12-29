using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine
{
    public static class UnityExtension_FA687687AC244AA3AFCC94581A3B985D
    {
        /// <summary>
        /// target是不是original的祖先
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="original"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool IsAncestor<A>(this Transform original, A target)
            where A : Component
        {
            if (target == null)
            {
                return false;
            }

            if (original)
            {
                var targetTransform = target.transform;
                var parent = original.transform.parent;
                while (parent != null)
                {
                    if (parent == targetTransform)
                    {
                        return true;
                    }
                    parent = parent.parent;
                }
            }
            return false;
        }

        /// <summary>
        /// target是不是original的祖先，或者是不是original本身
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="original"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool IsAncestorOrSelf<A>(this Transform original, A target)
           where A : Component
        {
            if (target == null)
            {
                return false;
            }

            if (original)
            {
                var targetTransform = target.transform;

                if (original == targetTransform)
                {
                    //Is Self
                    return true;
                }

                var parent = original.transform.parent;
                while (parent != null)
                {
                    if (parent == targetTransform)
                    {
                        return true;
                    }
                    parent = parent.parent;
                }
            }
            return false;
        }

        /// <summary>
        /// target是不是original的后代
        /// </summary>
        /// <typeparam name="D"></typeparam>
        /// <param name="original"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool IsDescendant<D>(this Transform original, D target)
            where D : Component
        {
            if (target == null)
            {
                return false;
            }

            if (original)
            {
                var targetTransform = target.transform;

                var parent = targetTransform.parent;
                while (parent != null)
                {
                    if (parent == original)
                    {
                        return true;
                    }
                    parent = parent.parent;
                }
            }
            return false;
        }

        /// <summary>
        /// target是不是original的后代，或者是不是original本身
        /// </summary>
        /// <typeparam name="D"></typeparam>
        /// <param name="original"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool IsDescendantOrSelf<D>(this Transform original, D target)
            where D : Component
        {
            if (target == null)
            {
                return false;
            }

            if (original)
            {
                var targetTransform = target.transform;

                if (original == targetTransform)
                {
                    //Is Self
                    return true;
                }

                var parent = targetTransform.parent;
                while (parent != null)
                {
                    if (parent == original)
                    {
                        return true;
                    }
                    parent = parent.parent;
                }
            }
            return false;
        }
    }

}




