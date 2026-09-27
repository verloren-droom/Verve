namespace Verve
{
    using System;
    using System.ComponentModel;

    /// <summary>
    ///   <para>UI 控制器；绑定页面与模型，统一管理订阅。</para>
    /// </summary>
    /// <typeparam name="TView">页面类型。</typeparam>
    /// <typeparam name="TModel">模型类型。</typeparam>
    public abstract class ControllerBase<TView, TModel> : DisposableObject
        where TView : ViewBase
        where TModel : class
    {
        /// <summary>
        ///   <para>返回当前绑定的页面；未绑定时为 null。</para>
        /// </summary>
        public TView View { get; private set; }

        /// <summary>
        ///   <para>返回当前绑定的模型；未绑定时为 null。</para>
        /// </summary>
        public TModel Model { get; private set; }

        /// <summary>
        ///   <para>返回当前是否同时绑定了 <see cref="ViewBase"/> 和 模型。</para>
        /// </summary>
        public bool IsBound => View != null && Model != null;

        /// <summary>
        ///   <para>绑定一个 模型 和未释放的 <see cref="ViewBase"/>。</para>
        /// </summary>
        /// <param name="view">待绑定的页面。</param>
        /// <param name="model">待绑定的模型。</param>
        /// <remarks>再次绑定会先解除旧绑定，但不会释放旧对象。</remarks>
        public void Bind(TView view, TModel model)
        {
            ThrowIfDisposed();
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (view.IsReleased) throw new ObjectDisposedException(view.GetType().FullName);
            if (ReferenceEquals(View, view) && ReferenceEquals(Model, model))
            {
                return;
            }

            Unbind();
            if (View != null || Model != null)
            {
                // OnUnbound 期间若创建了新绑定，不能继续覆盖它，否则会遗留新绑定的事件订阅。
                throw new InvalidOperationException(
                    $"{GetType().FullName} 不能在解除旧绑定的回调中重新绑定。");
            }

            View = view;
            Model = model;
            view.StateChanged += OnViewStateChanged;
            if (model is INotifyPropertyChanged observable)
            {
                observable.PropertyChanged += HandleModelPropertyChanged;
            }

            try
            {
                OnBound(view, model);
                if (ReferenceEquals(View, view) && ReferenceEquals(Model, model) && view.IsOpen)
                {
                    OnOpened();
                }
            }
            catch (Exception bindFailure)
            {
                var ownsBinding = ReferenceEquals(View, view) && ReferenceEquals(Model, model);
                Exception failure = bindFailure;
                try
                {
                    Unsubscribe(view, model);
                }
                catch (Exception cleanupFailure)
                {
                    failure = ExceptionUtility.Combine(failure, cleanupFailure);
                }

                // 回调可能已经重入 Bind 形成新绑定；只回滚仍属于本次调用的绑定。
                if (!ownsBinding)
                {
                    throw failure;
                }

                View = null;
                Model = null;

                try
                {
                    OnUnbound(view, model);
                }
                catch (Exception unbindFailure)
                {
                    failure = ExceptionUtility.Combine(failure, unbindFailure);
                }

                throw failure;
            }
        }

        /// <summary>
        ///   <para>解除当前绑定，不释放 <see cref="ViewBase"/> 或 模型。</para>
        /// </summary>
        public void Unbind()
        {
            var view = View;
            var model = Model;
            if (view == null && model == null)
            {
                return;
            }

            Exception failure = null;
            try
            {
                Unsubscribe(view, model);
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            View = null;
            Model = null;
            try
            {
                OnUnbound(view, model);
            }
            catch (Exception exception)
            {
                failure = ExceptionUtility.Combine(failure, exception);
            }

            if (failure != null)
            {
                throw failure;
            }
        }

        /// <summary>
        ///   <para>绑定完成后调用一次，可在这里连接控件输入事件。</para>
        /// </summary>
        /// <param name="view">页面。</param>
        /// <param name="model">待绑定的模型。</param>
        protected virtual void OnBound(TView view, TModel model) { }

        /// <summary>
        ///   <para>解除绑定后调用一次，应在这里清理派生类自己的订阅。</para>
        /// </summary>
        /// <param name="view">页面。</param>
        /// <param name="model">待绑定的模型。</param>
        protected virtual void OnUnbound(TView view, TModel model) { }

        /// <summary>
        ///   <para><see cref="ViewBase"/> 打开后调用；<see cref="ViewBase"/> 已打开时执行 <see cref="Bind"/> 也会立即调用。</para>
        /// </summary>
        protected virtual void OnOpened() { }

        /// <summary>
        ///   <para><see cref="ViewBase"/> 关闭后调用。</para>
        /// </summary>
        protected virtual void OnClosed() { }

        /// <summary>
        ///   <para>模型 属性变化后调用。</para>
        /// </summary>
        /// <param name="propertyName">发生变化的属性名称。</param>
        /// <remarks>模型未实现 <see cref="System.ComponentModel.INotifyPropertyChanged"/> 时不会自动触发。</remarks>
        protected virtual void OnModelPropertyChanged(string propertyName) { }

        /// <inheritdoc />
        protected override void OnDispose() => Unbind();

        /// <summary>
        ///   <para>处理页面状态变化。</para>
        /// </summary>
        /// <param name="view">页面。</param>
        /// <param name="state">状态。</param>
        private void OnViewStateChanged(ViewBase view, ViewState state)
        {
            if (!ReferenceEquals(View, view))
            {
                return;
            }

            switch (state)
            {
                case ViewState.Opened:
                    OnOpened();
                    break;
                case ViewState.Closed:
                    OnClosed();
                    break;
                case ViewState.Released:
                    Unbind();
                    break;
            }
        }

        /// <summary>
        ///   <para>处理模型属性变化。</para>
        /// </summary>
        /// <param name="sender">发送方。</param>
        /// <param name="args">日志格式化参数。</param>
        private void HandleModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (ReferenceEquals(Model, sender)) OnModelPropertyChanged(args?.PropertyName);
        }

        /// <summary>
        ///   <para>取消订阅。</para>
        /// </summary>
        /// <param name="view">页面。</param>
        /// <param name="model">待绑定的模型。</param>
        private void Unsubscribe(ViewBase view, object model)
        {
            if (view != null)
            {
                view.StateChanged -= OnViewStateChanged;
            }

            if (model is INotifyPropertyChanged observable)
            {
                observable.PropertyChanged -= HandleModelPropertyChanged;
            }
        }

    }
}
