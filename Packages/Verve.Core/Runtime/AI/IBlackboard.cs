namespace Verve.AI
{
    using System;
    
    
    public interface IBlackboard : IDisposable
    {
        void SetValue<T>(string key, in T value);
        T GetValue<T>(string key, T defaultValue = default);
        void RemoveValue(string key);
        bool HasValue(string key);
    }
}