#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Crypto
{
    using UnityEngine;

    
    [System.Serializable, GameFeatureComponentMenu("Verve/Crypto")]
    public class CryptoGameFeatureComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("加密密钥")] private GameFeatureParameter<string> m_EncryptionKey = new GameFeatureParameter<string>("XXXXXXXXXXXXXXXX");
        public string EncryptionKey => m_EncryptionKey.Value;
    }
}

#endif