using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verve.ECS;

#if VERVE_FRAMEWORK
#endif

namespace Verve.Test
{
    public class VerveTest1 : MonoBehaviour
    {
        [SerializeField] private Entity m_Entity1 = Entity.Create(typeof(PositionComponent), typeof(ScaleComponent));
        [SerializeField] private Entity m_Entity2 = Entity.Create(typeof(PositionComponent), typeof(ScaleComponent));
        
        // Start is called before the first frame update
        void Start()
        {
#if VERVE_FRAMEWORK_ECS
            GameLauncher.Instance.AddUnit<ECSUnit>();
#endif
            GameLauncher.Instance.Initialize();
        }
    
        // Update is called once per frame
        void Update()
        {
            
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
    
}
