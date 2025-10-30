#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.MVC
{
    using System;
    using UnityEngine;
    using Verve.MVC;
    
    
    /// <summary>
    ///   <para>MVC命令基类</para>
    /// </summary>
    [Serializable]
    public abstract class CommandBase : ICommand
    {
        void ICommand.Execute() => OnExecute();
        
        void ICommand.Undo() => OnUndo();
        
        public abstract IActivity GetActivity();

        /// <summary>
        ///   <para>命令执行</para>
        /// </summary>
        protected abstract void OnExecute();
        
        /// <summary>
        ///   <para>命令撤销</para>
        /// </summary>
        protected virtual void OnUndo() {}
    }
}

#endif