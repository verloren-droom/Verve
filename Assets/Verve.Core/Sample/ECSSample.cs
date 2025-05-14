namespace Verve.Sample
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    
    using ECS;
    
    public class ECSSample : MonoBehaviour
    {
        [SerializeField] private Entity m_Entity1 = Entity.Create(typeof(PositionComponent), typeof(ScaleComponent));
        [SerializeField] private Entity m_Entity2 = Entity.Create(typeof(PositionComponent), typeof(ScaleComponent));
    }
    
    public struct PositionComponent : IComponentBase
    {
        public float x;
        public float y;
        public float z;
    }

    public struct ScaleComponent : IComponentBase
    {
        public float x;
        public float y;
        public float z;
    }

    [ECSSystem(typeof(TransformSystem))]
    public class TransformSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            base.OnUpdate();
            // Entities.ForEach(e =>
            // {
            //     if (e.TryGetComponent(out PositionComponent pos))
            //     {
            //         Debug.Log($"{e.ID} -> {pos.x} : {pos.y} : {pos.z}");
            //     }
            // });
        }
    }
}
