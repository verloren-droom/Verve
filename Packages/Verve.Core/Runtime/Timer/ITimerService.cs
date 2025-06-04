namespace Verve.Timer
{
    using System;
    
    
    public interface ITimerService : Unit.IUnitService
    {
        float TimeScale { get; set; }
        int AddTimer(float duration, Action onComplete, bool loop = false);
        void RemoveTimer(int id);
        void RemoveTimer(Action onComplete);
        void ClearTimer();
        void Update(float deltaTime);
    }
}