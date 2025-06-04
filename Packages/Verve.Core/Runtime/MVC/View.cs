namespace Verve.MVC
{
    using System;


    public abstract partial class ViewBase : IView
    {
        public abstract IActivity Activity { get; set; }
        
        public string ViewName { get; }
        
        protected virtual void OnOpening() { }
        protected virtual void OnClosing() { }

        public void Open()
        {
            OnOpening();
            OnOpened?.Invoke(this);
        }

        public void Close()
        {
            OnClosing();
            OnClosed?.Invoke(this);
        }

        public event Action<IView> OnOpened;
        public event Action<IView> OnClosed;
    }
}