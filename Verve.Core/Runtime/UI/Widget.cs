#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    ///   <para>部件所有权工具；供页面和局部组件创建、释放部件。</para>
    /// </summary>
    internal static class UIWidgetOwnerUtility
    {
        /// <summary>
        ///   <para>创建实例。</para>
        /// </summary>
        /// <param name="component">组件。</param>
        /// <param name="ownerRoot">所有者根节点。</param>
        /// <param name="boundaryOwner">边界所有者。</param>
        /// <param name="ownerName">变量所属节点名称。</param>
        /// <param name="owner">所有者。</param>
        /// <param name="created">是否已经完成创建。</param>
        /// <typeparam name="TWidget">部件类型。</typeparam>
        internal static TWidget Create<TWidget>(
            UIWidgetComponent component,
            Transform ownerRoot,
            Component boundaryOwner,
            string ownerName,
            object owner,
            out bool created)
            where TWidget : WidgetBase
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (ownerRoot == null) throw new ObjectDisposedException(ownerName);
            if (component.transform != ownerRoot && !component.transform.IsChildOf(ownerRoot))
            {
                throw new ArgumentException(
                    $"Widget 组件必须位于 {ownerName} 的节点内。",
                    nameof(component));
            }

            var bindingError = UICompositionPolicy.GetBindingError(boundaryOwner, component.gameObject);
            if (bindingError != null)
            {
                throw new InvalidOperationException(bindingError);
            }

            var existing = component.RuntimeWidget;
            if (existing != null)
            {
                if (existing is TWidget typedWidget && !existing.IsReleased &&
                    ReferenceEquals(existing.Owner, owner))
                {
                    created = false;
                    return typedWidget;
                }

                var reason = ReferenceEquals(existing.Owner, owner)
                    ? $"当前宿主已经绑定 {existing.GetType().FullName}"
                    : "其他宿主已经绑定该组件";
                throw new InvalidOperationException(
                    $"UIWidgetComponent {reason}，不能在 {ownerName} 中绑定 {typeof(TWidget).FullName}。");
            }

            created = true;
            return WidgetBase.Create<TWidget>(component, owner);
        }

        /// <summary>
        ///   <para>释放全部。</para>
        /// </summary>
        /// <param name="widgets">部件。</param>
        internal static void ReleaseAll(ref List<WidgetBase> widgets)
        {
            var ownedWidgets = widgets;
            widgets = null;
            if (ownedWidgets != null)
                ResourceUtility.ReleaseAll(ownedWidgets, widget => widget.Release());
        }
    }

    /// <summary>
    ///   <para>UI 部件基类；由所属页面或部件创建和释放。</para>
    /// </summary>
    public abstract class WidgetBase
    {
        /// <summary>
        ///   <para>组件。</para>
        /// </summary>
        private UIWidgetComponent m_Component;
        /// <summary>
        ///   <para>所有者。</para>
        /// </summary>
        private object m_Owner;
        /// <summary>
        ///   <para>状态。</para>
        /// </summary>
        private WidgetState m_State;
        /// <summary>
        ///   <para>拥有的部件。</para>
        /// </summary>
        private List<WidgetBase> m_OwnedWidgets;
        /// <summary>
        ///   <para>事件已绑定。</para>
        /// </summary>
        private bool m_EventsBound;
        /// <summary>
        ///   <para>是否释放中。</para>
        /// </summary>
        private bool m_IsReleasing;
        
        /// <summary>
        ///   <para>创建部件基类。</para>
        /// </summary>
        protected WidgetBase()
        {
            if (!UIInstanceCreator.IsCreating(typeof(WidgetBase)))
            {
                throw new InvalidOperationException(
                    $"Widget 类型 {GetType().FullName} 只能由 {nameof(ViewBase)} 或 {nameof(ViewPartBase)} 创建，不能外部创建。");
            }
        }

        /// <summary>
        ///   <para>返回 <see cref="WidgetBase"/> Prefab 上的绑定组件。</para>
        /// </summary>
        public UIWidgetComponent Component => m_Component;

        /// <summary>
        ///   <para>返回绑定组件所在的节点。</para>
        /// </summary>
        public GameObject GameObject => m_Component?.gameObject;

        /// <summary>
        ///   <para>返回当前 <see cref="WidgetBase"/> 的运行时宿主，仅供 <see cref="ViewBase"/> 和 <see cref="ViewPartBase"/> 的所有权检查。</para>
        /// </summary>
        internal object Owner => m_Owner;

        /// <summary>
        ///   <para>返回当前是否已经创建。</para>
        /// </summary>
        public bool IsCreated => m_State == WidgetState.Created && m_Component != null;

        /// <summary>
        ///   <para>返回是否已经释放或绑定节点已被 Unity 销毁。</para>
        /// </summary>
        public bool IsReleased => m_State == WidgetState.Released || m_Component == null;

        /// <summary>
        ///   <para>返回生成代码使用的变量签名。</para>
        /// </summary>
        protected virtual int VariableSignature => 0;

        /// <summary>
        ///   <para><see cref="WidgetBase"/> 创建完成后调用一次。</para>
        /// </summary>
        protected virtual void OnCreated() { }

        /// <summary>
        ///   <para><see cref="WidgetBase"/> 创建完成后绑定控件事件。</para>
        /// </summary>
        protected virtual void OnBindEvents() { }

        /// <summary>
        ///   <para><see cref="WidgetBase"/> 释放前解除控件事件。</para>
        /// </summary>
        protected virtual void OnUnbindEvents() { }

        /// <summary>
        ///   <para><see cref="WidgetBase"/> 释放后调用一次。</para>
        /// </summary>
        protected virtual void OnReleased() { }

        /// <summary>
        ///   <para>创建并绑定指定 <see cref="WidgetBase"/> Prefab 上的组件。</para>
        /// </summary>
        /// <typeparam name="TWidget"><see cref="WidgetBase"/> 类型。</typeparam>
        /// <param name="component"><see cref="WidgetBase"/> Prefab 实例上的组件。</param>
        /// <param name="owner">所有者。</param>
        internal static TWidget Create<TWidget>(UIWidgetComponent component, object owner = null)
            where TWidget : WidgetBase
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            var widget = (TWidget)UIInstanceCreator.Create(
                typeof(TWidget), typeof(WidgetBase), "Widget");
            widget.Attach(component, owner);
            return widget;
        }

        /// <summary>
        ///   <para>由所属 <see cref="ViewBase"/> 或 <see cref="ViewPartBase"/> 释放 <see cref="WidgetBase"/>、解除事件并解除组件绑定。</para>
        /// </summary>
        internal void Release()
        {
            Game.ThrowIfNotOnMainThread($"{GetType().FullName}.{nameof(Release)}");
            if (m_State == WidgetState.Released || m_IsReleasing)
            {
                return;
            }

            if (m_State == WidgetState.Detached)
            {
                m_State = WidgetState.Released;
                return;
            }

            m_IsReleasing = true;
            Exception failure = null;
            try
            {
                try
                {
                    ClearDataInternal();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }

                try
                {
                    UIWidgetOwnerUtility.ReleaseAll(ref m_OwnedWidgets);
                }
                catch (Exception exception)
                {
                    failure = ExceptionUtility.Combine(failure, exception);
                }

                m_State = WidgetState.Released;
                try
                {
                    UnbindEventsInternal();
                }
                catch (Exception exception)
                {
                    failure = ExceptionUtility.Combine(failure, exception);
                }

                try
                {
                    OnReleased();
                }
                catch (Exception exception)
                {
                    failure = ExceptionUtility.Combine(failure, exception);
                }
            }
            finally
            {
                var component = m_Component;
                m_Component = null;
                m_Owner = null;
                if (component != null)
                {
                    component.Destroyed -= OnComponentDestroyed;
                    component.DetachWidget(this);
                }

                m_IsReleasing = false;
            }

            if (failure != null)
            {
                throw failure;
            }
        }

        /// <summary>
        ///   <para>按固定索引读取序列化的节点组件。</para>
        /// </summary>
        /// <param name="index">索引。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        protected T GetVariable<T>(int index) where T : UnityEngine.Object
            => (m_Component ?? throw new ObjectDisposedException(GetType().FullName)).GetVariable<T>(index);

        /// <summary>
        ///   <para>按相对 <see cref="WidgetBase"/> 根节点的路径查找子节点。</para>
        /// </summary>
        /// <param name="path">路径。</param>
        /// <returns>找到的节点；路径不存在时返回 null。</returns>
        protected Transform FindChild(string path)
            => (m_Component ?? throw new ObjectDisposedException(GetType().FullName))
                .transform.Find(path);

        /// <summary>
        ///   <para>按相对 <see cref="WidgetBase"/> 根节点的路径查找子节点组件。</para>
        /// </summary>
        /// <param name="path">路径。</param>
        /// <typeparam name="T">目标组件类型。</typeparam>
        /// <returns>找到的组件；路径不存在或节点未挂载该组件时返回 null。</returns>
        protected T FindChildComponent<T>(string path) where T : Component 
            => FindChild(path)?.GetComponent<T>();

        /// <summary>
        ///   <para>创建并登记当前 <see cref="WidgetBase"/> 节点下的子 <see cref="WidgetBase"/>。</para>
        /// </summary>
        /// <param name="component">子节点上的 <see cref="UIWidgetComponent"/>。</param>
        /// <typeparam name="TWidget">子 <see cref="WidgetBase"/> 类型。</typeparam>
        protected TWidget CreateWidget<TWidget>(UIWidgetComponent component)
            where TWidget : WidgetBase
        {
            ThrowIfUnavailable();
            var widget = UIWidgetOwnerUtility.Create<TWidget>(
                component,
                m_Component?.transform,
                m_Component,
                $"Widget {GetType().FullName}",
                this,
                out var created);
            try
            {
                ThrowIfUnavailable();
                if (created)
                {
                    (m_OwnedWidgets ??= new List<WidgetBase>(2)).Add(widget);
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
        ///   <para>由强类型数据基类在释放前清空数据。</para>
        /// </summary>
        protected virtual void ClearDataInternal() { }

        /// <summary>
        ///   <para>绑定。</para>
        /// </summary>
        /// <param name="component">组件。</param>
        /// <param name="owner">所有者。</param>
        private void Attach(UIWidgetComponent component, object owner)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            if (m_Component != null || m_State == WidgetState.Released)
            {
                throw new InvalidOperationException($"Widget {GetType().FullName} 已绑定组件或已经释放。");
            }

            if (VariableSignature != component.VariableSignature)
            {
                throw new InvalidOperationException(
                    $"{GetType().FullName} 的 UI Widget 变量与 Prefab 不一致，请重新生成变量代码。");
            }

            try
            {
                component.AttachWidget(this);
                m_Component = component;
                m_Owner = owner;
                m_State = WidgetState.Created;
                component.Destroyed += OnComponentDestroyed;
                OnCreated();
                EnsureAlive();
                BindEventsInternal();
                EnsureAlive();
            }
            catch (Exception exception)
            {
                if (m_State != WidgetState.Released)
                {
                    try
                    {
                        Release();
                    }
                    catch (Exception releaseFailure)
                    {
                        // 创建失败仍需暴露释放失败，避免资源问题被原始异常掩盖。
                        throw ExceptionUtility.Combine(exception, releaseFailure);
                    }
                }
                throw;
            }
        }

        /// <summary>
        ///   <para>绑定事件。</para>
        /// </summary>
        private void BindEventsInternal()
        {
            if (m_EventsBound)
            {
                return;
            }

            m_EventsBound = true;
            try
            {
                OnBindEvents();
            }
            catch (Exception exception)
            {
                if (m_EventsBound)
                {
                    try
                    {
                        OnUnbindEvents();
                    }
                    catch (Exception unbindFailure)
                    {
                        exception = ExceptionUtility.Combine(exception, unbindFailure);
                    }
                    finally
                    {
                        m_EventsBound = false;
                    }
                }

                throw exception;
            }
        }

        /// <summary>
        ///   <para>解绑事件。</para>
        /// </summary>
        private void UnbindEventsInternal()
        {
            if (!m_EventsBound)
            {
                return;
            }

            try
            {
                OnUnbindEvents();
            }
            finally
            {
                m_EventsBound = false;
            }
        }

        /// <summary>
        ///   <para>处理组件已销毁。</para>
        /// </summary>
        /// <param name="component">组件。</param>
        private void OnComponentDestroyed(UIWidgetComponent component)
        {
            if (!ReferenceEquals(m_Component, component) || m_State == WidgetState.Released)
            {
                return;
            }

            Release();
        }

        /// <summary>
        ///   <para>检查对象仍然可用。</para>
        /// </summary>
        private void EnsureAlive()
        {
            if (m_State == WidgetState.Released || m_Component == null)
            {
                throw new InvalidOperationException($"Widget {GetType().FullName} 在创建期间被释放。");
            }
        }

        /// <summary>
        ///   <para>拒绝在未创建或释放回调期间使用 <see cref="WidgetBase"/>。</para>
        /// </summary>
        protected void ThrowIfUnavailable()
        {
            if (m_IsReleasing || !IsCreated)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        /// <summary>
        ///   <para>部件状态。</para>
        /// </summary>
        private enum WidgetState : byte
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
    }

    /// <summary>
    ///   <para>UI 数据部件；绑定强类型数据。</para>
    /// </summary>
    /// <typeparam name="TData">数据类型。</typeparam>
    public abstract class WidgetBase<TData> : WidgetBase
    {
        /// <summary>
        ///   <para>数据。</para>
        /// </summary>
        private TData m_Data;
        /// <summary>
        ///   <para>包含数据。</para>
        /// </summary>
        private bool m_HasData;

        /// <summary>
        ///   <para>返回当前数据是否已经设置。</para>
        /// </summary>
        public bool HasData => m_HasData;

        /// <summary>
        ///   <para>返回当前数据；未设置时返回默认值。</para>
        /// </summary>
        public TData Data => m_HasData ? m_Data : default;

        /// <summary>
        ///   <para>替换数据并刷新部件显示；不会先触发一次清空回调。</para>
        /// </summary>
        /// <param name="data">待显示的数据。</param>
        public void SetData(TData data)
        {
            Game.ThrowIfNotOnMainThread(nameof(SetData));
            ThrowIfUnavailable();
            m_Data = data;
            m_HasData = true;
            try
            {
                OnDataSet(data);
                EnsureDataAlive();
            }
            catch
            {
                m_Data = default;
                m_HasData = false;
                throw;
            }
        }

        /// <summary>
        ///   <para>清除当前数据并刷新空状态，不释放部件。</para>
        /// </summary>
        public void ClearData()
        {
            Game.ThrowIfNotOnMainThread(nameof(ClearData));
            if (!m_HasData)
            {
                return;
            }

            ClearDataImpl();
        }

        /// <summary>
        ///   <para>清除绑定数据。</para>
        /// </summary>
        private void ClearDataImpl()
        {
            m_Data = default;
            m_HasData = false;
            OnDataCleared();
        }

        /// <summary>
        ///   <para>设置数据后调用。</para>
        /// </summary>
        /// <param name="data">当前数据。</param>
        protected virtual void OnDataSet(TData data) { }

        /// <summary>
        ///   <para>清除数据或释放部件后调用。</para>
        /// </summary>
        protected virtual void OnDataCleared() { }

        /// <inheritdoc />
        protected override void ClearDataInternal()
        {
            if (m_HasData)
            {
                ClearDataImpl();
            }
        }

        /// <summary>
        ///   <para>检查绑定数据仍然可用。</para>
        /// </summary>
        private void EnsureDataAlive()
        {
            if (IsReleased)
            {
                throw new InvalidOperationException($"Widget {GetType().FullName} 在设置数据期间被释放。");
            }
        }

    }
}

#endif