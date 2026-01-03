namespace Verve
{
    using System;
    
    
    /// <summary>
    ///   <para>生命值组件</para>
    /// </summary>
    [Serializable, NetworkSyncComponent(NetworkSyncDirection.SendAndReceive)]
    public struct HealthComponent : IComponent
    {
        /// <summary>
        ///   <para>当前生命值</para>
        /// </summary>
        [NetworkSyncField] public int health;
        
        /// <summary>
        ///   <para>最大生命值</para>
        /// </summary>
        [NetworkSyncField] public int maxHealth;
        
        /// <summary>
        ///   <para>是否死亡</para>
        /// </summary>
        [NetworkSyncField] public bool dead;
        
        /// <summary>
        ///   <para>是否无敌</para>
        /// </summary>
        [NetworkSyncField] public bool invincible;
    }
}