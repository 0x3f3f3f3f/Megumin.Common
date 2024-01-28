using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pool = UnityEngine.Pool.ListPool<UnityEngine.Transform>;

namespace Megumin
{
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
        /// 无论是否包含root自身，root 深度为第0层。默认值为2，遍历root和他的子元素
        /// </summary>
        public int DeepCount;

        /// <summary>
        /// Transform Iterator，广度优先（breadth first search BFS）。无GC Alloc。
        /// </summary>
        /// <param name="root"></param>
        /// <param name="deepCount">无论是否包含root自身，root 深度为第0层。默认值为2，遍历root和他的子元素</param>
        /// <param name="includeRoot">遍历结果收包含root本身。默认包括</param>
        public TransformBFS(Transform root, int deepCount = 2, bool includeRoot = true)
        {
            Root = root;
            IncludeRoot = includeRoot;
            DeepCount = deepCount;
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
                deepIndex = 0;
                currentLayer = Pool.Get();
                nextLayer = Pool.Get();

                Reset();
            }

            public Transform Current => currentLayer[index];

            public int index;
            /// <summary>
            /// 层号
            /// </summary>
            public int deepIndex;
            //Queue的实现思路，没办法处理层号。而且Queue没有内置池。
            List<Transform> currentLayer;
            List<Transform> nextLayer;

            public bool MoveNext()
            {
                index++;

                if (index >= currentLayer.Count)
                {
                    //当前层没有元素。切换到下一层。
                    deepIndex++;
                    if (deepIndex >= Interater.DeepCount)
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

                if (deepIndex < Interater.DeepCount)
                {
                    //当前层；
                    var current = currentLayer[index];

                    //将当前对象的子对象加入下一层
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

                    //将当前对象的子对象加入下一层
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
        /// 无论是否包含root自身，root 深度为第0层。默认值为2，遍历root和他的子元素
        /// </summary>
        public int DeepCount;

        /// <summary>
        /// Transform Iterator，深度优先（depth first search DFS）。无GC Alloc。
        /// </summary>
        /// <param name="root"></param>
        /// <param name="deepCount">无论是否包含root自身，root 深度为第0层。默认值为2，遍历root和他的子元素</param>
        /// <param name="includeRoot">遍历结果收包含root本身。默认包括</param>
        public TransformDFS(Transform root, int deepCount = 2, bool includeRoot = true)
        {
            Root = root;
            IncludeRoot = includeRoot;
            DeepCount = deepCount;
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
                deepIndex = 0;
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
            public int deepIndex;
            public Transform Current
            {
                get
                {
                    if (index == -1)
                    {
                        //-1返回节点本身，0返回第一个子元素。
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
                    deepIndex = 0;
                    return true;
                }

                //更改根节点

                //尝试向下一层搜索
                if (deepIndex < Interater.DeepCount - 2)
                {
                    if (index >= 0)//表示节点本身已经是子节点了。当index == -1时，表示节点还没有执行子节点本身，不要向下更换根节点。
                    {
                        var current = Current;
                        if (current.childCount > 0)
                        {
                            //深入一层
                            deepIndex++;
                            //父对象设置为当前对象。
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

                    deepIndex--;
                    if (deepIndex < 0)
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
                deepIndex = 0;
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
    }
}



