using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Megumin
{
    /// <summary>
    /// 选中对象黑板
    /// </summary>
    public interface ISelection
    {
        /// <summary>
        /// 清理，防止内存泄露
        /// </summary>
        void Clear();

        /// <summary>
        /// 取消选中
        /// </summary>
        /// <param name="raiseEvent"></param>
        void UnSelect(bool raiseEvent = true);
    }

    public delegate void SelectedChangedDelegate<T>(T newSelected, T oldSelected);
    /// <summary>
    /// 选中对象黑板
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISelection<T> : ISelection
    {
        /// <summary>
        /// 当Current被赋值时触发，相同对象重新被选中也会触发。
        /// </summary>
        event Action<T> Selected;

        /// <summary>
        /// 仅当选中对象改变时才被触发
        /// </summary>
        event SelectedChangedDelegate<T> SelectedChanged;

        /// <summary>
        /// 仅当选中对象改变时才被触发,传递选中堆栈本身
        /// </summary>
        event Action<ISelection<T>> SelectedChanged2;

        /// <summary>
        /// 当前选中对象
        /// </summary>
        T Current { get; set; }

        /// <summary>
        /// 当前多选对象
        /// </summary>
        List<T> CurrentList { get; }

        /// <summary>
        /// 上次选中对象
        /// </summary>
        T Last { get; }

        /// <summary>
        /// 上次多选对象
        /// </summary>
        List<T> LastList { get; }

        /// <summary>
        /// 将current复制到last
        /// </summary>
        void CopyCurrent2Last();
    }

    public interface IGenericSelector : ISelection
    {
        /// <summary>
        /// 当选中类型不匹配时，保存当前选中对象不变
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="gSelected"></param>
        /// <param name="raiseEvent"></param>
        void SelectKeep<V>(V gSelected, bool raiseEvent);

        /// <summary>
        /// 当选中类型不匹配时，不保存当前选中对象
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="gSelected"></param>
        /// <param name="raiseEvent"></param>
        void Select<V>(V gSelected, bool raiseEvent);
    }

    public class SelectedStack<T> : ISelection, ISelection<T>, IGenericSelector
    {
        public event Action<T> Selected;
        public event SelectedChangedDelegate<T> SelectedChanged;
        public event Action<ISelection<T>> SelectedChanged2;

        private T current;

        public T Current
        {
            get => current;
            set
            {
                Select(value, true);
            }
        }

        public List<T> CurrentList { get; } = new();

        private T last;
        public T Last { get => last; }

        public List<T> LastList { get; } = new();

        public string LogInfo => $"Current:{Current} ---- Last:{Last}";

        protected virtual bool EqualsValue(T x, T y)
        {
            bool flag = EqualityComparer<T>.Default.Equals(x, y);
            return flag;
        }

        public virtual void CopyCurrent2Last()
        {
            last = current;
            LastList.Clear();
            LastList.AddRange(CurrentList);
        }

        protected virtual void RaiseEvent(bool isChanged)
        {
            Selected?.Invoke(current);

            if (isChanged)
            {
                SelectedChanged?.Invoke(current, last);
                SelectedChanged2?.Invoke(this);
            }
        }

        /// <summary>
        /// 检测新的输入是否改变了当前选中对象，同时检测单选多选。
        /// </summary>
        /// <param name="selected"></param>
        /// <returns></returns>
        public virtual bool CheckChange(T selected)
        {
            bool isEqual = EqualsValue(current, selected);
            if (isEqual == false)
            {
                return true;
            }

            if (CurrentList.Count > 1)
            {
                //由多选变为单选
                //注意当选中对象为空时，Count为0。
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检测新的输入是否改变了当前选中对象，同时检测单选多选。
        /// </summary>
        /// <param name="selectedList"></param>
        /// <returns></returns>
        public virtual bool CheckChange(List<T> selectedList)
        {
            if (selectedList == null && CurrentList.Count == 0)
            {
                return false;
            }

            var count = selectedList.Count;
            if (count != CurrentList.Count)
            {
                //多选数量不相等时，则选中对象改变
                return true;
            }

            for (var i = 0; i < count; i++)
            {
                //任意位置元素不相等，则选中对象改变
                var isEuqal = EqualsValue(selectedList[i], CurrentList[i]);
                if (isEuqal == false)
                {
                    return true;
                }
            }

            return false;
        }

        public virtual void Select(T selected, bool raiseEvent = true)
        {
            bool isChanged = CheckChange(selected);
            if (isChanged)
            {
                //只有改变时，才更新值
                CopyCurrent2Last();

                //赋值新的current
                current = selected;
                CurrentList.Clear();
                CurrentList.Add(selected);
            }

            if (raiseEvent)
            {
                RaiseEvent(isChanged);
            }
        }

        public virtual void Select(List<T> selectedList, bool raiseEvent = true)
        {
            bool isChanged = CheckChange(selectedList);

            if (isChanged)
            {
                //只有改变时，才更新值
                CopyCurrent2Last();

                //赋值新的current
                if (selectedList == null)
                {
                    current = default;
                    CurrentList.Clear();
                }
                else
                {
                    if (selectedList.Count > 0)
                    {
                        current = selectedList[0];
                    }
                    else
                    {
                        current = default;
                    }
                    CurrentList.Clear();
                    CurrentList.AddRange(selectedList);
                }
            }

            if (raiseEvent)
            {
                RaiseEvent(isChanged);
            }
        }

        public virtual void UnSelect(bool raiseEvent = true)
        {
            Select(selected: default, raiseEvent);
        }

        public void SelectKeep<V>(V gSelected, bool raiseEvent)
        {
            if (gSelected is T selected)
            {
                Select(selected, raiseEvent);
            }
        }

        public void Select<V>(V gSelected, bool raiseEvent)
        {
            if (gSelected is T selected)
            {
                Select(selected, raiseEvent);
            }
            else
            {
                UnSelect(raiseEvent);
            }
        }


        protected T Get(int index)
        {
            throw new NotImplementedException();
        }

        protected T[] GetStack()
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            current = default;
            last = default;
            CurrentList.Clear();
            LastList.Clear();
        }
    }

    public class ObjectSelectedStack<T> : SelectedStack<T>
        where T : class
    {
        protected override bool EqualsValue(T x, T y)
        {
            return x == y;
        }
    }

    public class Selected : ISelection, IGenericSelector
    {
        /// <summary>
        /// 全局唯一，用于跨模块调用。尽量使用Custom。
        /// </summary>
        public static Selected Shared { get; } = new();

        /// <summary>
        /// 通常用于定义重写
        /// </summary>
        public static Selected Custom { get; protected set; } = new();

        public ObjectSelectedStack<object> Object = new();
        public ObjectSelectedStack<UnityEngine.Object> UnityObject = new();
        public ObjectSelectedStack<UnityEngine.GameObject> GameObject = new();
        public ObjectSelectedStack<UnityEngine.Component> Component = new();

        /// <summary>
        /// 选中Component式默认认为选中所属GameObject
        /// </summary>
        public bool AutoSetComponent2GameObject { get; set; } = true;

        public object Current => Object.Current;
        public object Last => Object.Last;

        public UnityEngine.Object CurrentUnityObject => UnityObject.Current;
        public UnityEngine.Object LastUnityObject => UnityObject.Last;

        public UnityEngine.GameObject CurrentGameObject => GameObject.Current;
        public UnityEngine.GameObject LastGameObject => GameObject.Last;

        public UnityEngine.Component CurrentComponent => Component.Current;
        public UnityEngine.Component LastComponent => Component.Last;

        public string LogInfo => $"Object: {Object.LogInfo} \nUnityObject: {UnityObject.LogInfo} \nGameObject: {GameObject.LogInfo} \nComponent: {Component.LogInfo}";

        /// <summary>
        /// 当选中类型不匹配时，保存当前选中对象不变
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gSelected"></param>
        /// <param name="raiseEvent"></param>
        public virtual void SelectKeep<T>(T gSelected, bool raiseEvent = true)
        {
            if (Shared != this)
            {
                //同时调用全局唯一选中堆栈
                Shared.SelectKeep(gSelected, raiseEvent);
            }

            if (gSelected is object obj)
            {
                Object.Select(obj, raiseEvent);
            }

            if (gSelected is UnityEngine.Object uobj)
            {
                UnityObject.Select(uobj, raiseEvent);
            }

            if (gSelected is GameObject go)
            {
                GameObject.Select(go, raiseEvent);
            }

            if (gSelected is Component component)
            {
                Component.Select(component, raiseEvent);
                if (AutoSetComponent2GameObject)
                {
                    GameObject.Select(component.gameObject, raiseEvent);
                }
            }
        }

        /// <summary>
        /// 当选中类型不匹配时，不保存当前选中对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gSelected"></param>
        /// <param name="raiseEvent"></param>
        public virtual void Select<T>(T gSelected, bool raiseEvent = true)
        {
            if (Shared != this)
            {
                //同时调用全局唯一选中堆栈
                Shared.Select(gSelected, raiseEvent);
            }

            //这种写法可能导致转换失败，使用UnSelect传入default更好。
            //Object.Select(gSelected as object, raiseEvent);
            //UnityObject.Select(gSelected as UnityEngine.Object, raiseEvent);
            //GameObject.Select(gSelected as UnityEngine.GameObject, raiseEvent);
            //Component.Select(gSelected as UnityEngine.Component, raiseEvent);

            if (gSelected is object obj)
            {
                Object.Select(obj, raiseEvent);
            }
            else
            {
                Object.UnSelect(raiseEvent);
            }

            if (gSelected is UnityEngine.Object uobj)
            {
                UnityObject.Select(uobj, raiseEvent);
            }
            else
            {
                UnityObject.UnSelect(raiseEvent);
            }

            if (gSelected is GameObject go)
            {
                GameObject.Select(go, raiseEvent);
            }
            else
            {
                GameObject.UnSelect(raiseEvent);
            }

            if (gSelected is Component component)
            {
                Component.Select(component, raiseEvent);
                if (AutoSetComponent2GameObject)
                {
                    GameObject.Select(component.gameObject, raiseEvent);
                }
            }
            else
            {
                Component.UnSelect(raiseEvent);
            }
        }

        public virtual void UnSelect(bool raiseEvent = true)
        {
            if (Shared != this)
            {
                //同时调用全局唯一选中堆栈
                Shared.UnSelect(raiseEvent);
            }

            Object.UnSelect();
            UnityObject.UnSelect();
            GameObject.UnSelect();
            Component.UnSelect();
        }

        public virtual void Clear()
        {
            if (Shared != this)
            {
                //同时调用全局唯一选中堆栈
                Shared.Clear();
            }

            Object.Clear();
            UnityObject.Clear();
            GameObject.Clear();
            Component.Clear();
        }
    }

    /// <summary>
    /// 自定义选中扩展
    /// </summary>
    public abstract class CustomSelected : Selected
    {
        public List<IGenericSelector> CustomStack { get; } = new();

        public override void Select<T>(T gSelected, bool raiseEvent = true)
        {
            base.Select(gSelected, raiseEvent);

            foreach (var selection in CustomStack)
            {
                selection.Select(gSelected, raiseEvent);
            }
        }

        public override void SelectKeep<T>(T gSelected, bool raiseEvent = true)
        {
            base.SelectKeep(gSelected, raiseEvent);

            foreach (var selection in CustomStack)
            {
                selection.SelectKeep(gSelected, raiseEvent);
            }
        }

        public override void UnSelect(bool raiseEvent = true)
        {
            base.UnSelect(raiseEvent);

            foreach (var selection in CustomStack)
            {
                selection.UnSelect(raiseEvent);
            }
        }

        public override void Clear()
        {
            base.Clear();

            foreach (var selection in CustomStack)
            {
                selection.Clear();
            }
        }
    }

    /// <summary>
    /// 自定义选中扩展示例
    /// </summary>
    internal class MyCustomSelected : CustomSelected
    {
        public ObjectSelectedStack<UnityEngine.Object> UnityObjectTest = new();
        public static new MyCustomSelected Custom { get; } = new();

        public MyCustomSelected()
        {
            CustomStack.Add(UnityObjectTest);
        }

        void Test()
        {
            MyCustomSelected.Custom.GameObject.ToString();
            MyCustomSelected.Custom.UnityObjectTest.ToString();
            MyCustomSelected.Custom.Select(new object());
        }
    }
}


