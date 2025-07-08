#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Platform
{
    using System;
    using UnityEngine;

    
    /// <summary>
    /// 安卓平台子模块
    /// </summary>
    [Serializable]
    public class AndroidPlatformSubmodule : MobilePlatformSubmodule
    {
        public override string ModuleName => "AndroidPlatform";
    }
}

#endif