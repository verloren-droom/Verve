#if VERVE_UNIEX_0_0_1_OR_NEWER

namespace Verve.UniEx.Sample
{
    using Verve.MVC;
    using UnityEngine;
    using System.ComponentModel;


    /// <summary>
    ///   <para>操纵虚拟摇杆玩家</para>
    /// </summary>
    [AddComponentMenu("Verve/Sample/VirtualJoystickPlayer")]
    public class VirtualJoystickPlayer : MonoBehaviour, IController
    {
        public IActivity GetActivity() => VirtualJoystickActivity.Instance;
        
        [SerializeField, Tooltip("移动速度"), Min(0)] private float m_Speed = 100;

        private Vector2 m_Dir = Vector2.zero;
        private VirtualJoystickModel m_JoystickModel;
        
        
        private void Awake()
        {
            m_JoystickModel = this.GetModel<VirtualJoystickModel>();
        }

        private void Start()
        {
            m_JoystickModel?.Direction.AddListener(OnJoystickChanged);
        }

        private void Update()
        {
            gameObject.transform.Translate(new Vector3(m_Dir.x * m_Speed, 0, m_Dir.y * m_Speed) * Time.deltaTime, Space.World);
        }

        private void OnJoystickChanged(object sender, PropertyChangedEventArgs e)
        {
            m_Dir = (sender as PropertyProxy<Vector2>).Value;
        }
    }
}

#endif