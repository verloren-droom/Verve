namespace Verve.Samples
{
    using Verve.MVC;
    using UnityEngine;


    /// <summary>
    ///   <para>操纵虚拟摇杆玩家</para>
    /// </summary>
    [AddComponentMenu("Verve/Samples/VirtualJoystickPlayer")]
    public class VirtualJoystickPlayer : MonoBehaviour, IController
    {
        [SerializeField, Tooltip("移动速度"), Min(0)] private float m_Speed = 10;
        
        public IActivity GetActivity() => VirtualJoystickActivity.Instance;

        private VirtualJoystickModel m_JoystickModel;
        private Rigidbody m_Rigidbody;
        
        
        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_JoystickModel = this.GetModel<VirtualJoystickModel>();
        }

        // private void Update()
        // {
        //     if (m_JoystickModel != null && m_JoystickModel.Direction.Value != Vector2.zero)
        //     {
        //         gameObject.transform.Translate(new Vector3(m_JoystickModel.Direction.Value.x * m_Speed, 0, m_JoystickModel.Direction.Value.y * m_Speed) * Time.deltaTime, Space.World);
        //     }
        // }
        
        private void FixedUpdate()
        {
            if (m_JoystickModel != null && m_JoystickModel.Direction.Value != Vector2.zero)
            {
                m_Rigidbody.MovePosition(m_Rigidbody.position + new Vector3(m_JoystickModel.Direction.Value.x * m_Speed, 0, m_JoystickModel.Direction.Value.y * m_Speed) * Time.fixedDeltaTime);
            }
        }

    }
}