#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Platform
{
    using System;
    using UnityEngine;

    
    /// <summary>
    /// 安卓平台
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(PlatformGameFeature), Description = "安卓平台")]
    public class AndroidPlatform : MobilePlatform
    {
        // TODO: 实现安卓平台子模块
    }
}

#endif