namespace Verve
{
    using System;
    
    
    public sealed class ActionDisposable : IDisposable
    {
        private Action m_DisposeAction;
        private bool m_IsDisposed;

        public ActionDisposable(Action disposeAction)
        {
            m_DisposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
        }

        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                m_DisposeAction?.Invoke();
                m_DisposeAction = null;
                m_IsDisposed = true;
            }
        }
    }
}