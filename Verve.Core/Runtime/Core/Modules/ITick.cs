namespace Verve
{
    /// <summary>
    ///   <para>早期更新接口；实现该接口的对象会在 <see cref="TickGroup.Early"/> 阶段被调度。</para>
    /// </summary>
    public interface IEarlyTick
    {
        /// <summary>
        ///   <para>执行早期更新逻辑。</para>
        /// </summary>
        /// <param name="deltaTime">当前 <b>Tick</b> 阶段的时间步长</param>
        void EarlyTick(float deltaTime);
    }

    /// <summary>
    ///   <para>物理更新接口；实现该接口的对象会在 <see cref="TickGroup.Physics"/> 阶段被调度。</para>
    /// </summary>
    public interface IPhysicsTick
    {
        /// <summary>
        ///   <para>执行物理更新逻辑。</para>
        /// </summary>
        /// <param name="deltaTime">当前 <b>Tick</b> 阶段的时间步长</param>
        void PhysicsTick(float deltaTime);
    }

    /// <summary>
    ///   <para>游戏逻辑更新接口；实现该接口的对象会在 <see cref="TickGroup.Gameplay"/> 阶段被调度。</para>
    /// </summary>
    public interface IGameplayTick
    {
        /// <summary>
        ///   <para>执行游戏逻辑更新。</para>
        /// </summary>
        /// <param name="deltaTime">当前 <b>Tick</b> 阶段的时间步长</param>
        void GameplayTick(float deltaTime);
    }

    /// <summary>
    ///   <para>后期更新接口；实现该接口的对象会在 <see cref="TickGroup.Late"/> 阶段被调度。</para>
    /// </summary>
    public interface ILateTick
    {
        /// <summary>
        ///   <para>执行后期更新逻辑。</para>
        /// </summary>
        /// <param name="deltaTime">当前 <b>Tick</b> 阶段的时间步长</param>
        void LateTick(float deltaTime);
    }

    /// <summary>
    ///   <para>Tick 顺序接口；同一 Tick 分组内会按照该值从小到大排序执行。</para>
    /// </summary>
    public interface ITickOrder
    {
        /// <summary>
        ///   <para><b>Tick</b> 执行顺序。</para>
        /// </summary>
        int TickOrder { get; }
    }
}