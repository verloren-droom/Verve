#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.MVC
{
    using System;
    using UnityEngine;
    using Verve.MVC;
    
    
    /// <summary>
    /// MVC命令基类
    /// </summary>
    [Serializable]
    public abstract class CommandBase : ICommand
    {
        void ICommand.Execute() => OnExecute();
        void ICommand.Undo() => OnUndo();
        
        public abstract IActivity GetActivity();

        protected abstract void OnExecute();
        protected virtual void OnUndo() {}
    }
}

#endif