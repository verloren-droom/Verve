namespace Verve
{
    using System;
    
    
    /// <summary>
    ///   <para>移动能力：使用<see cref="VelocityComponent"/>修改<see cref="PositionComponent"/>，可通过<see cref="InputDirectionComponent"/>操控移动方向</para>
    /// </summary>
    [Serializable]
    public sealed class MovementCapability : Capability
    {
        public override TickGroup TickGroup => TickGroup.Physics;

        protected override void OnSetup()
        {
            Require<PositionComponent>();
            Require<VelocityComponent>();
        }

        protected internal override void TickActive(in float deltaTime)
        {
            ref var position = ref this.GetComponent<PositionComponent>();
            ref var velocity = ref this.GetComponent<VelocityComponent>();

            if (this.TryGetComponent(out InputDirectionComponent input))
            {
                if (input.horizontal != 0f || input.vertical != 0f || input.jump != 0f)
                {
                    velocity.x = input.horizontal * velocity.acceleration * deltaTime;
                    velocity.y = input.jump * velocity.acceleration * deltaTime;
                    velocity.z = input.vertical * velocity.acceleration * deltaTime;
                }
                else if (velocity.deceleration > 0f)
                {
                    ApplyDeceleration(ref velocity, velocity.deceleration * deltaTime);
                }
            }

            ApplyFriction(ref velocity, deltaTime);
            
            ClampVelocity(ref velocity);
            
            position.x += velocity.x * deltaTime;
            position.y += velocity.y * deltaTime;
            position.z += velocity.z * deltaTime;
        }
        
        /// <summary>
        ///   <para>应用摩擦力</para>
        /// </summary>
        private void ApplyFriction(ref VelocityComponent velocity, float deltaTime)
        {
            if (velocity.friction <= 0f) return;
            
            float frictionAmount = velocity.friction * deltaTime;
            float currentMagnitude = velocity.Magnitude;
            
            if (currentMagnitude > frictionAmount)
            {
                float scale = (currentMagnitude - frictionAmount) / currentMagnitude;
                velocity.x *= scale;
                velocity.y *= scale;
                velocity.z *= scale;
            }
            else
            {
                velocity.x = velocity.y = velocity.z = 0f;
            }
        }
        
        /// <summary>
        ///   <para>应用减速</para>
        /// </summary>
        private void ApplyDeceleration(ref VelocityComponent velocity, float decelAmount)
        {
            float currentSpeed = velocity.Magnitude;
            
            if (currentSpeed > 0f)
            {
                float newSpeed = Math.Max(0f, currentSpeed - decelAmount);
                float scale = newSpeed / currentSpeed;
                
                velocity.x *= scale;
                velocity.y *= scale;
                velocity.z *= scale;
            }
        }
        
        /// <summary>
        ///   <para>限制速度</para>
        /// </summary>
        private void ClampVelocity(ref VelocityComponent velocity)
        {
            float currentSpeed = velocity.Magnitude;
            
            if (currentSpeed > velocity.maxSpeed && velocity.maxSpeed > 0f)
            {
                float scale = velocity.maxSpeed / currentSpeed;
                velocity.x *= scale;
                velocity.y *= scale;
                velocity.z *= scale;
            }
        }
    }
}