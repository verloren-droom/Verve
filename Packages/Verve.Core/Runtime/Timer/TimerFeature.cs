namespace Verve.Timer
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 计时器功能
    /// </summary>
    [System.Serializable]
    public class TimerFeature : ModularGameFeature
    {
        protected override void OnLoad()
        {
            base.OnLoad();
            RegisterSubmodule(new SimpleTimerSubmodule());
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            for (int i = 0; i < GetAllSubmodules().Count(); i++)
            {
                ((ITimerSubmodule)GetAllSubmodules().ElementAt(i)).IsRunning = true;
            }
        }
        
        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            for (int i = 0; i < GetAllSubmodules().Count(); i++)
            {
                ((ITimerSubmodule)GetAllSubmodules().ElementAt(i)).IsRunning = false;
            }
        }

        public void AddTimer<TTimerService>(float duration, Action onComplete, bool loop = false) where TTimerService : class, ITimerSubmodule
        {
            GetSubmodule<TTimerService>().AddTimer(duration, onComplete, loop);
        }
        
        public void RemoveTimer<TTimerService>(int id) where TTimerService : class, ITimerSubmodule
        {
            GetSubmodule<TTimerService>().RemoveTimer(id);
        }
        
        public void RemoveTimer<TTimerService>(Action onComplete) where TTimerService : class, ITimerSubmodule
        {
            GetSubmodule<TTimerService>().RemoveTimer(onComplete);
        }
        
        public void ClearTimer<TTimerService>() where TTimerService : class, ITimerSubmodule
        {
            GetSubmodule<TTimerService>().ClearTimer();
        }
        
        public void OnUpdate(float deltaTime)
        {
            foreach (var service in GetAllSubmodules())
            {
                ((ITimerSubmodule)service).Update(deltaTime);
            }
        }
    }
}