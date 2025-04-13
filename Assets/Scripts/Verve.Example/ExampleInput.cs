using UnityEngine;
using UnityEngine.InputSystem;
using Verve.Input;

namespace Verve.Example
{
    public class ExampleInput : ComponentBase
    {
        [SerializeField]
        private PlayerInput m_PlayerInput;

        private InputSystemService input;
        
        protected override void Start()
        {
            base.Start();

            input = new InputSystemService(m_PlayerInput);
            
        }

        protected override void Update()
        {
            base.Update();
            // Debug.Log("INPUT ->" + input.ActiveDevice);
        }
    }
}