namespace Verve
{
    using System.ComponentModel;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>UI 模型；提供属性变化通知与一次性释放。</para>
    /// </summary>
    public abstract class ModelBase : DisposableObject, INotifyPropertyChanged
    {
        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
        protected override void OnDispose()
        {
            base.OnDispose();
            PropertyChanged = null;
        }

        /// <summary>
        ///   <para>设置字段，并在值变化时发出通知。</para>
        /// </summary>
        /// <param name="field">字段。</param>
        /// <param name="value">新值。</param>
        /// <param name="propertyName">属性名称。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        protected bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string propertyName = null)
        {
            ThrowIfDisposed();

            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        /// <summary>
        ///   <para>手动通知计算属性变化。</para>
        /// </summary>
        /// <param name="propertyName">发生变化的属性名称。</param>
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            ThrowIfDisposed();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
