#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;

    /// <summary>
    ///   <para><see cref="ViewPartBase"/> 的生命周期状态。</para>
    /// </summary>
    internal enum ViewPartState : byte
    {
        /// <summary>
        ///   <para>未绑定。</para>
        /// </summary>
        Detached,
        /// <summary>
        ///   <para>已创建。</para>
        /// </summary>
        Created,
        /// <summary>
        ///   <para>已释放。</para>
        /// </summary>
        Released,
    }

    /// <summary>
    ///   <para>页面局部逻辑基类。</para>
    /// </summary>
    public abstract class ViewPartBase
    {
        /// <summary>
        ///   <para>创建页面局部组件基类。</para>
        /// </summary>
        protected ViewPartBase()
        {
            if (!UIInstanceCreator.IsCreating(typeof(ViewPartBase)))
            {
                throw new InvalidOperationException(
                    $"ViewPart 类型 {GetType().FullName} 只能由 View 创建，不能外部创建。");
            }
        }

        /// <summary>
        ///   <para>返回所属页面。</para>
        /// </summary>
        public abstract ViewBase Owner { get; }

        /// <summary>
        ///   <para>返回绑定的节点。</para>
        /// </summary>
        public abstract GameObject GameObject { get; }

        /// <summary>
        ///   <para>返回是否已经完成创建。</para>
        /// </summary>
        public abstract bool IsCreated { get; }

        /// <summary>
        ///   <para>返回是否已经释放或绑定节点已被 Unity 销毁。</para>
        /// </summary>
        public abstract bool IsReleased { get; }

        /// <summary>
        ///   <para>返回当前 <see cref="ViewPartBase"/> 直接持有的 <see cref="WidgetBase"/>，仅供运行时诊断读取。</para>
        /// </summary>
        internal virtual IReadOnlyList<WidgetBase> Widgets => null;

        /// <summary>
        ///   <para>由 <see cref="ViewBase"/> 绑定根组件并执行创建回调。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        /// <param name="component">组件。</param>
        internal abstract void Attach(ViewBase owner, Component component);

        /// <summary>
        ///   <para>由 <see cref="ViewBase"/> 释放局部逻辑及其所有子资源。</para>
        /// </summary>
        internal abstract void ReleaseInternal();
    }

    /// <summary>
    ///   <para>页面局部逻辑基类；使用强类型根组件。</para>
    /// </summary>
    /// <typeparam name="TComponent"><see cref="ViewPartBase"/> 绑定的根组件类型。</typeparam>
    public abstract class ViewPartBase<TComponent> : ViewPartBase
        where TComponent : Component
    {
        /// <summary>
        ///   <para>所有者。</para>
        /// </summary>
        private ViewBase m_Owner;
        /// <summary>
        ///   <para>组件。</para>
        /// </summary>
        private TComponent m_Component;
        /// <summary>
        ///   <para>状态。</para>
        /// </summary>
        private ViewPartState m_State;
        /// <summary>
        ///   <para>拥有的部件。</para>
        /// </summary>
        private List<WidgetBase> m_OwnedWidgets;
        /// <summary>
        ///   <para>拥有的对象。</para>
        /// </summary>
        private List<GameObject> m_OwnedObjects;

        /// <summary>
        ///   <para>返回绑定的强类型根组件。</para>
        /// </summary>
        protected TComponent Component => m_Component;

        /// <inheritdoc />
        public sealed override ViewBase Owner => m_Owner;

        /// <inheritdoc />
        public sealed override GameObject GameObject => m_Component != null ? m_Component.gameObject : null;

        /// <inheritdoc />
        public sealed override bool IsCreated => m_State == ViewPartState.Created && !IsReleased;

        /// <inheritdoc />
        public sealed override bool IsReleased => m_State == ViewPartState.Released || m_Component == null;

        /// <inheritdoc />
        internal sealed override IReadOnlyList<WidgetBase> Widgets => m_OwnedWidgets;

        /// <summary>
        ///   <para><see cref="ViewPartBase"/> 完成节点绑定后调用一次。</para>
        /// </summary>
        protected virtual void OnCreated() { }

        /// <summary>
        ///   <para><see cref="ViewPartBase"/> 进入释放状态后调用一次。</para>
        /// </summary>
        protected virtual void OnReleased() { }

        /// <summary>
        ///   <para>取得由当前 <see cref="ViewPartBase"/> 持有的 <see cref="WidgetBase"/>；同一组件重复请求时返回已有实例。</para>
        /// </summary>
        /// <param name="component"><see cref="WidgetBase"/> 实例上的绑定组件。</param>
        /// <typeparam name="TWidget"><see cref="WidgetBase"/> 类型。</typeparam>
        /// <returns>创建完成的 <see cref="WidgetBase"/>。</returns>
        protected TWidget CreateWidget<TWidget>(UIWidgetComponent component)
            where TWidget : WidgetBase
        {
            if (!IsCreated) throw new ObjectDisposedException(GetType().FullName);
            var widget = UIWidgetOwnerUtility.Create<TWidget>(
                component,
                m_Component?.transform,
                m_Owner?.Component,
                $"ViewPart {GetType().FullName}",
                this,
                out var created);
            try
            {
                if (!IsCreated)
                {
                    throw new InvalidOperationException(
                        $"ViewPart {GetType().FullName} 在创建 Widget 期间已被释放。");
                }

                if (created)
                {
                    var widgets = m_OwnedWidgets ??= new List<WidgetBase>(2);
                    widgets.Add(widget);
                }
                return widget;
            }
            catch (Exception exception)
            {
                if (!created)
                {
                    throw;
                }

                try
                {
                    widget.Release();
                }
                catch (Exception releaseFailure)
                {
                    throw ExceptionUtility.Combine(exception, releaseFailure);
                }

                throw;
            }
        }

        /// <summary>
        ///   <para>从动态 <see cref="WidgetBase"/> 模板创建部件，并由当前 <see cref="ViewPartBase"/> 统一拥有节点和 <see cref="WidgetBase"/>。</para>
        /// </summary>
        /// <param name="template">模板节点上的 <see cref="UIWidgetComponent"/>。</param>
        /// <param name="parent">动态节点的父节点。</param>
        /// <typeparam name="TWidget"><see cref="WidgetBase"/> 类型。</typeparam>
        /// <returns>创建完成的 <see cref="WidgetBase"/>。</returns>
        protected TWidget CreateWidget<TWidget>(UIWidgetComponent template, Transform parent)
            where TWidget : WidgetBase
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (!IsCreated) throw new ObjectDisposedException(GetType().FullName);
            if (!UICompositionPolicy.IsOwnedNode(m_Component?.transform, parent))
            {
                throw new ArgumentException(
                    $"动态 Widget 的父节点必须位于 ViewPart {GetType().FullName} 的节点内。",
                    nameof(parent));
            }

            var instance = UnityEngine.Object.Instantiate(template.gameObject, parent);
            try
            {
                var component = instance.GetComponent<UIWidgetComponent>();
                if (component == null)
                {
                    throw new InvalidOperationException(
                        $"动态 Widget 模板 {template.name} 实例化后缺少 {nameof(UIWidgetComponent)}。");
                }

                var widget = CreateWidget<TWidget>(component);
                (m_OwnedObjects ??= new List<GameObject>(2)).Add(instance);
                return widget;
            }
            catch
            {
                UnityEngine.Object.Destroy(instance);
                throw;
            }
        }

        /// <inheritdoc />
        internal sealed override void Attach(ViewBase owner, Component component)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (m_Owner != null || m_State == ViewPartState.Released)
            {
                throw new InvalidOperationException($"ViewPart {GetType().FullName} 已绑定或已经释放。");
            }

            if (!(component is TComponent typedComponent))
            {
                throw new InvalidOperationException(
                    $"ViewPart {GetType().FullName} 要求根组件 {typeof(TComponent).FullName}，实际传入 {component.GetType().FullName}。");
            }

            var previousOwner = m_Owner;
            var previousComponent = m_Component;
            var previousState = m_State;
            m_Owner = owner;
            m_Component = typedComponent;
            try
            {
                m_State = ViewPartState.Created;
                OnCreated();
                if (!IsCreated || m_Owner == null)
                {
                    throw new InvalidOperationException(
                        $"ViewPart {GetType().FullName} 在创建期间被释放。");
                }
            }
            catch (Exception exception)
            {
                if (m_State != ViewPartState.Released)
                {
                    try
                    {
                        ReleaseWidgets();
                    }
                    catch (Exception releaseFailure)
                    {
                        exception = ExceptionUtility.Combine(exception, releaseFailure);
                    }

                    try
                    {
                        ReleaseOwnedObjects();
                    }
                    catch (Exception releaseFailure)
                    {
                        exception = ExceptionUtility.Combine(exception, releaseFailure);
                    }

                    m_State = previousState;
                    m_Owner = previousOwner;
                    m_Component = previousComponent;
                }

                ExceptionUtility.Rethrow(exception);
                throw;
            }
        }

        /// <inheritdoc />
        internal sealed override void ReleaseInternal()
        {
            if (m_State == ViewPartState.Released)
            {
                return;
            }

            m_State = ViewPartState.Released;
            Exception failure = null;
            try
            {
                ReleaseWidgets();
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            try
            {
                OnReleased();
            }
            catch (Exception exception)
            {
                failure = ExceptionUtility.Combine(failure, exception);
            }

            try
            {
                ReleaseOwnedObjects();
            }
            catch (Exception exception)
            {
                failure = ExceptionUtility.Combine(failure, exception);
            }
            finally
            {
                m_Owner = null;
                m_Component = null;
            }

            if (failure != null)
            {
                throw failure;
            }
        }

        /// <summary>
        ///   <para>释放部件。</para>
        /// </summary>
        private void ReleaseWidgets() => UIWidgetOwnerUtility.ReleaseAll(ref m_OwnedWidgets);

        /// <summary>
        ///   <para>释放拥有的对象。</para>
        /// </summary>
        private void ReleaseOwnedObjects()
        {
            if (m_OwnedObjects == null)
            {
                return;
            }

            for (var i = m_OwnedObjects.Count - 1; i >= 0; i--)
            {
                var gameObject = m_OwnedObjects[i];
                if (gameObject != null)
                {
                    UnityEngine.Object.Destroy(gameObject);
                }
            }

            m_OwnedObjects.Clear();
            m_OwnedObjects = null;
        }

    }

    /// <summary>
    ///   <para><see cref="ViewPartBase"/> 实例创建器。</para>
    /// </summary>
    /// <typeparam name="TPart">局部组件类型。</typeparam>
    internal static class ViewPartCreator<TPart> where TPart : ViewPartBase
    {
        /// <summary>
        ///   <para>创建实例。</para>
        /// </summary>
        internal static TPart Create()
        {
            return (TPart)UIInstanceCreator.Create(
                typeof(TPart), typeof(ViewPartBase), "ViewPart");
        }
    }
}

#endif