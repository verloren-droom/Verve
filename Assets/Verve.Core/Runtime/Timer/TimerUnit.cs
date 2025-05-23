namespace Verve.Timer
{
    using Unit;
    using System;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 时间单元
    /// </summary>
    [CustomUnit("Timer"), System.Serializable]
    public partial class TimerUnit : UnitBase<ITimerService>
    {
        protected override void OnStartup(params object[] args)
        {
            base.OnStartup(args);
            CanEverTick = true;
            AddService(new SimpleTimerService());
        }

        public void AddTimer<TTimerService>(float duration, Action onComplete, bool loop = false) where TTimerService : class, ITimerService
        {
            GetService<TTimerService>().AddTimer(duration, onComplete, loop);
        }
        
        public void RemoveTimer<TTimerService>(int id) where TTimerService : class, ITimerService
        {
            GetService<TTimerService>().RemoveTimer(id);
        }
        
        public void RemoveTimer<TTimerService>(Action onComplete) where TTimerService : class, ITimerService
        {
            GetService<TTimerService>().RemoveTimer(onComplete);
        }
        
        public void ClearTimer<TTimerService>() where TTimerService : class, ITimerService
        {
            GetService<TTimerService>().ClearTimer();
        }

        protected override void OnTick(float deltaTime, float unscaledTime)
        {
            base.OnTick(deltaTime, unscaledTime);

            foreach (var service in m_UnitServices.Values)
            {
                service.Update(unscaledTime);
            }
        }
    }
}