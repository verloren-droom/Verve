namespace Verve.MVC
{
    using System;


    public abstract partial class ViewBase : IView
    {
        public event Action<IView> OnOpened;
        public event Action<IView> OnClosed;
        
        public abstract IActivity Activity { get; set; }
        
        public abstract string ViewName { get; }

        
        protected virtual void OnOpening(params object[] _) { }
        protected virtual void OnClosing() { }

        void IView.Open(params object[] args)
        {
            OnOpening(args);
            OnOpened?.Invoke(this);
        }

        public void Close()
        {
            OnClosing();
            OnClosed?.Invoke(this);
        }
    }
}