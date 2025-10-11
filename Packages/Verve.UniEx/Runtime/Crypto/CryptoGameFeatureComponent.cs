#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Crypto
{
    using UnityEngine;


    /// <summary>
    /// 加解密游戏功能组件
    /// </summary>
    [System.Serializable, GameFeatureComponentMenu("Verve/Crypto")]
    public sealed class CryptoGameFeatureComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("加密密钥")] private GameFeatureParameter<string> m_EncryptionKey = new GameFeatureParameter<string>("XXXXXXXXXXXXXXXX");


        /// <summary> 加密密钥 </summary>
        public string EncryptionKey => m_EncryptionKey.Value;
    }
}

#endif