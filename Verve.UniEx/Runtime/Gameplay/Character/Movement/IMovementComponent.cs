namespace Verve.UniEx.Gameplay
{
    using UnityEngine;
    
    
    /// <summary>
    /// 移动组件接口
    /// </summary>
    public interface IMovementComponent
    {
        /// <summary>
        /// 移动
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="speed"></param>
        void Move(Vector3 direction, float speed);
        /// <summary>
        /// 跳跃
        /// </summary>
        /// <param name="jumpStrength"></param>
        void Jump(float jumpStrength);
        /// <summary>
        /// 获取是否在地面
        /// </summary>
        bool IsGrounded { get; }
        /// <summary>
        /// 获取速度
        /// </summary>
        Vector3 Velocity { get; }
    }
}