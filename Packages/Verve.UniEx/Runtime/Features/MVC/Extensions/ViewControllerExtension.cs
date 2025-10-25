namespace Verve.UniEx.MVC
{
    using System;
    using UnityEngine;
    using System.Threading.Tasks;

    
    public static class ViewControllerExtension
    {
        public static bool OpenView(
            this ViewController self,
            GameObject viewPrefab,
            bool isCloseAllOther = false,
            Transform parent = null,
            Action<ViewBase> onOpened = null,
            params object[] args)
        {
            return self.OpenView(viewPrefab, isCloseAllOther, parent, onOpened, args);
        }
    }
}