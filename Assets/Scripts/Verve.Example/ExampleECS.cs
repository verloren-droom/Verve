namespace Verve.Example
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    
#if VERVE_FRAMEWORK_ECS
    using ECS;
    
    public class ExampleECS : MonoBehaviour
    {
        [SerializeField] private Entity m_Entity1 = Entity.Create(typeof(PositionComponent), typeof(ScaleComponent));
        [SerializeField] private Entity m_Entity2 = Entity.Create(typeof(PositionComponent), typeof(ScaleComponent));
        
        void Start()
        {
            GameLauncher.Instance.AddUnit<ECSUnit>();
            GameLauncher.Instance.Initialize();
        }
        
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
            Entities.ForEach(e =>
            {
                if (e.TryGetComponent(out PositionComponent pos))
                {
                    Debug.Log($"{e.ID} -> {pos.x} : {pos.y} : {pos.z}");
                }
            });
        }
    }

#endif
}
