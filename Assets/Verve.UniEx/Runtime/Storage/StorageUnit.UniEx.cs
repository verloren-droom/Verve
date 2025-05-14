namespace VerveUniEx.Storage
{
    
#if UNITY_5_3_OR_NEWER
    using Verve.Serializable;
    using Verve.Unit;
    
    
    /// <summary>
    /// 存储单元
    /// </summary>
    [CustomUnit("Storage", dependencyUnits: typeof(SerializableUnit)), System.Serializable]
    public partial class StorageUnit : Verve.Storage.StorageUnit
    {
        protected override void OnStartup(UnitRules parent, params object[] args)
        {
            base.OnStartup(parent, args);
            parent.onInitialized += rules =>
            {
                Register((() => new BuiltInStorage(m_Serializable)));
            };
        }
    }
#endif
    
}