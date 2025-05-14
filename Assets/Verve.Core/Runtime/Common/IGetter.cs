namespace Verve
{
    
    public interface IGetter<T>
    {
        T Get();
        bool TryGet(out T outValue);
    }
    
    public interface IGetter
    {
        T Get<T>();
        bool TryGet<T>(out T outValue);
    }
    
}