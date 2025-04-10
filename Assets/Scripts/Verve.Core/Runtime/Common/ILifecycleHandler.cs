namespace Verve
{
    using System.Collections;
    
    
    public interface ILifecycleHandler : IInitializable, System.IDisposable { }

    public interface ILifecycleHandlerAsync
    {
        IEnumerator Initialize();
        IEnumerator Dispose();
    }
}