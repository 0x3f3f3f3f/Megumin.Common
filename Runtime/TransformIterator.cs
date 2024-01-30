using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pool = UnityEngine.Pool.ListPool<UnityEngine.Transform>;

namespace Megumin
{
    //本来想加入过滤未开启节点的设置的。但是会导致复杂性增加。
    //用户根据遍历结果自行过滤更好。

    /// <summary>
    /// Transform Iterator，广度优先（breadth first search BFS）。无GC Alloc。
    /// </summary>
    public struct TransformBFS
    {
        public Transform Root;

        /// <summary>
        /// 遍历结果收包含root本身。默认不包括
        /// </summary>
        public bool IncludeRoot;

        /// <summary>
        /// 无论是否包含root自身，root 深度为第0层。默认值为3，遍历root，子节点，和子节点的子节点。如果层数小于3，使用普通遍历更方便，没必要使用此类。
        /// </summary>
        public int MaxDepth;

        /// <summary>
        /// Transform Iterator，广度优先（breadth first search BFS）。无GC Alloc。
        /// </summary>
        /// <param name="root"></param>
        /// <param name="maxDepth">无论是否包含root自身，root 深度为第0层。默认值为3，遍历root，子节点，和子节点的子节点。如果层数小于3，使用普通遍历更方便，没必要使用此类。</param>
        /// <param name="includeRoot">遍历结果收包含root本身。默认包括</param>
        public TransformBFS(Transform root, int maxDepth = 3, bool includeRoot = true)
        {
            Root = root;
            IncludeRoot = includeRoot;
            MaxDepth = maxDepth;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// 广度优先迭代器
        /// </summary>
        public struct Enumerator : IEnumerator<Transform>
        {
            public TransformBFS Interater;

            public Enumerator(TransformBFS interater)
            {
                this.Interater = interater;

                index = -1;
                depthIndex = 0;
                currentLayer = Pool.Get();
                nextLayer = Pool.Get();

                Reset();
            }

            public Transform Current => currentLayer[index];

            public int index;
            /// <summary>
            /// 层号
            /// </summary>
            public int depthIndex;
            //Queue的实现思路，没办法处理层号。而且Queue没有内置池。
            List<Transform> currentLayer;
            List<Transform> nextLayer;

            public bool MoveNext()
            {
                index++;

                if (index >= currentLayer.Count)
                {
                    //当前层没有元素。切换到下一层。
                    depthIndex++;
                    if (depthIndex >= Interater.MaxDepth)
                    {
                        return false;
                    }

                    if (nextLayer.Count == 0)
                    {
                        return false;
                    }

                    //交换两层,并将索引设置为0
                    (currentLayer, nextLayer) = (nextLayer, currentLayer);
                    index = 0;

                    //清理下一层容器
                    nextLayer.Clear();
                }

                if (depthIndex < Interater.MaxDepth)
                {
                    //当前层；
                    var current = currentLayer[index];

                    //将当前节点的子节点加入下一层
                    for (int i = 0; i < current.childCount; i++)
                    {
                        nextLayer.Add(current.GetChild(i));
                    }
                }

                return true;
            }

            public void Reset()
            {
                currentLayer.Clear();
                nextLayer.Clear();
                currentLayer.Add(Interater.Root);

                if (Interater.IncludeRoot)
                {
                    index = -1;
                }
                else
                {
                    index = 0;
                    //这里会跳过根节点的MoveNext,手动将子节点加入下一层
                    var current = Interater.Root;

                    //将当前节点的子节点加入下一层
                    for (int i = 0; i < current.childCount; i++)
                    {
                        nextLayer.Add(current.GetChild(i));
                    }
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                Pool.Release(currentLayer);
                Pool.Release(nextLayer);
            }
        }

#if UNITY_EDITOR

        [UnityEditor.MenuItem("GameObject/Megumin/Transform BFS Depth3", priority = 30)]
        static void BFSDepth3(UnityEditor.MenuCommand menuCommand)
        {
            if (menuCommand.context is GameObject go)
            {
                foreach (var item in new TransformBFS(go.transform))
                {
                    Debug.Log(item.name);
                }
            }
        }

        [UnityEditor.MenuItem("GameObject/Megumin/Transform BFS Depth All", priority = 31)]
        static void BFSDepthAll(UnityEditor.MenuCommand menuCommand)
        {
            if (menuCommand.context is GameObject go)
            {
                foreach (var item in new TransformBFS(go.transform, 99))
                {
                    Debug.Log(item.name);
                }
            }
        }

#endif

    }

    /// <summary>
    /// Transform Iterator，深度优先（depth first search DFS）。无GC Alloc。
    /// </summary>
    public struct TransformDFS
    {
        public Transform Root;
        /// <summary>
        /// 遍历结果收包含root本身。默认不包括
        /// </summary>
        public bool IncludeRoot;

        /// <summary>
        /// 无论是否包含root自身，root 深度为第0层。默认值为3，遍历root，子节点，和子节点的子节点。如果层数小于3，使用普通遍历更方便，没必要使用此类。
        /// </summary>
        public int MaxDepth;

        /// <summary>
        /// Transform Iterator，深度优先（depth first search DFS）。无GC Alloc。
        /// </summary>
        /// <param name="root"></param>
        /// <param name="maxDepth">无论是否包含root自身，root 深度为第0层。默认值为3，遍历root，子节点，和子节点的子节点。如果层数小于3，使用普通遍历更方便，没必要使用此类。</param>
        /// <param name="includeRoot">遍历结果收包含root本身。默认包括</param>
        public TransformDFS(Transform root, int maxDepth = 3, bool includeRoot = true)
        {
            Root = root;
            IncludeRoot = includeRoot;
            MaxDepth = maxDepth;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// 深度优先迭代器。向下向右搜索
        /// </summary>
        public struct Enumerator : IEnumerator<Transform>
        {
            public TransformDFS Interater;
            public Enumerator(TransformDFS interater)
            {
                this.Interater = interater;
                index = -1;
                depthIndex = 0;
                if (interater.IncludeRoot)
                {
                    currentRoot = null;
                }
                else
                {
                    currentRoot = Interater.Root;
                }
            }

            public int index;
            public int depthIndex;
            public Transform Current
            {
                get
                {
                    if (index == -1)
                    {
                        //-1返回节点本身，0返回第一个子节点。
                        return currentRoot;
                    }
                    return currentRoot.GetChild(index);
                }
            }

            public Transform currentRoot;
            public bool MoveNext()
            {
                if (currentRoot == null)
                {
                    currentRoot = Interater.Root;
                    index = -1;
                    depthIndex = 0;
                    return true;
                }

                //更改根节点

                //尝试向下一层搜索
                if (depthIndex < Interater.MaxDepth - 2)
                {
                    if (index >= 0)//表示节点本身已经是子节点了。当index == -1时，表示节点还没有执行子节点本身，不要向下更换根节点。
                    {
                        var current = Current;
                        if (current.childCount > 0)
                        {
                            //深入一层
                            depthIndex++;
                            //父节点设置为当前节点。
                            currentRoot = current;
                            index = 0;
                            return true;
                        }
                    }
                }

                //无法深入下一层时，向右搜索
                if (currentRoot.childCount > 0)
                {
                    //拥有可遍历子节点
                    index++;
                    if (index < currentRoot.childCount)
                    {
                        return true;
                    }
                }

                //无法向下向右搜索时，循环回退到上一层。
                while (true)
                {
                    if (currentRoot == Interater.Root)
                    {
                        //根节点不能返回上一层
                        return false;
                    }

                    depthIndex--;
                    if (depthIndex < 0)
                    {
                        return false;
                    }

                    index = currentRoot.GetSiblingIndex();

                    //父节点向上移动
                    currentRoot = currentRoot.parent;

                    //向上移动根节点成功，向右搜索。
                    index++;
                    if (index < currentRoot.childCount)
                    {
                        //当前节点拥有兄弟节点，
                        return true;
                    }
                }
            }

            public void Reset()
            {
                index = -1;
                depthIndex = 0;
                if (Interater.IncludeRoot)
                {
                    currentRoot = null;
                }
                else
                {
                    currentRoot = Interater.Root;
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {

            }
        }

#if UNITY_EDITOR

        [UnityEditor.MenuItem("GameObject/Megumin/Transform DFS Depth3", priority = 40)]
        static void DFSDepth3(UnityEditor.MenuCommand menuCommand)
        {
            if (menuCommand.context is GameObject go)
            {
                foreach (var item in new TransformDFS(go.transform))
                {
                    Debug.Log(item.name);
                }
            }
        }

        [UnityEditor.MenuItem("GameObject/Megumin/Transform DFS Depth All", priority = 41)]
        static void DFSDepthAll(UnityEditor.MenuCommand menuCommand)
        {
            if (menuCommand.context is GameObject go)
            {
                foreach (var item in new TransformDFS(go.transform, 99))
                {
                    Debug.Log(item.name);
                }
            }
        }

#endif

    }
}



