namespace VerveUniEx.Sample
{
    
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using UnityEngine;
    using Verve.Event;
    using Verve.MVC;
    
    
    public class ExamplePlayerController : MonoBehaviour, IController
    {
        [SerializeField] private float m_Speed = 100;

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
            gameObject.transform.Translate( new Vector3(m_Dir.x * m_Speed, 0, m_Dir.y * m_Speed) * Time.deltaTime, Space.World);
        }

        private void OnJoystickChanged(object sender, PropertyChangedEventArgs e)
        {
            // if (sender is Vector2 dir)
            // {
            //     m_Dir = dir;
            // }

            m_Dir = (sender as PropertyProxy<Vector2>).Value;
        }

        public IActivity Activity { get; set; } = ExampleActivity.Instance;
    }
}
