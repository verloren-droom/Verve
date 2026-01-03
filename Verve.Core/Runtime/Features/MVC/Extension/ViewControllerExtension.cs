#if UNITY_5_3_OR_NEWER

namespace Verve.MVC
{
    using System;
    using UnityEngine;
    using System.Threading.Tasks;

    
    /// <summary>
    ///   <para>视图控制器扩展</para>
    /// </summary>
    public static class ViewControllerExtension
    {
        /// <summary>
        ///   <para>打开视图</para>
        /// </summary>
        /// <param name="viewPrefab">视图预制体</param>
        /// <param name="isCloseAllOther">是否关闭其他页面</param>
        /// <param name="parent">父物体</param>
        /// <param name="onOpened">打开回调</param>
        /// <param name="args">参数</param>
        public static void OpenView(
            this ViewController self,
            GameObject viewPrefab,
            bool isCloseAllOther = false,
            Transform parent = null,
            Action<ViewBase> onOpened = null,
            params object[] args)
        {
            self.OpenView(viewPrefab, isCloseAllOther, parent, onOpened, args);
        }
    }
}

#endif