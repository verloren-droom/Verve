namespace Verve
{
    using System;
    
    
    /// <summary>
    ///   <para>生命值组件</para>
    /// </summary>
    [Serializable]
    public struct HealthComponent : IComponent
    {
        /// <summary>
        ///   <para>当前生命值</para>
        /// </summary>
        public int health;
        
        /// <summary>
        ///   <para>最大生命值</para>
        /// </summary>
        public int maxHealth;
        
        /// <summary>
        ///   <para>是否死亡</para>
        /// </summary>
        public bool dead;
        
        /// <summary>
        ///   <para>是否无敌</para>
        /// </summary>
        public bool invincible;
    }
}