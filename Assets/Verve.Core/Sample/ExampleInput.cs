using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using VerveUniEx.Input;
using VerveUniEx.Loader;

namespace Verve.Sample
{
    using Input;
    using Loader;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using LoaderUnit = VerveUniEx.Loader.LoaderUnit;
    

    public class ExampleInput : MonoBehaviour
    {
        [SerializeField] private PlayerInput m_PlayerInput;
        
        private InputUnit m_Input;
        private LoaderUnit m_LoaderUnit;
        
        protected void Start()
        {
            VerveExample.Instance.Initialize();
            Launcher.Instance.TryGetUnit(out m_Input);
            Launcher.Instance.TryGetUnit(out m_LoaderUnit);
            
            
            m_Input.AddListener<InputSystemService, float>("Move/Right", OnMoveRightAction);
            m_Input.AddListener<InputSystemService, float>("Move/Right", OnMoveRight1Action);

            m_Input.AddListener<InputSystemService, bool>("Interaction/Key", OnInteractionAction);
            m_Input.Enable<InputSystemService>();


            // GameLauncher.Instance.Debugger.Assert(false, "Debug.Assert");
            // Debug.LogAssertion("LOG DEBUG");

            
            // GameLauncher.Instance.Debugger.Log("[LOG] NEW Debugger! ".ToString());
            // GameLauncher.Instance.Debugger.LogWarning("[WARN] NEW Debugger! ");
            // GameLauncher.Instance.Debugger.LogError("[ERROR] NEW Debugger!");
        }

        private void OnInteractionAction(InputServiceContext<bool> obj)
        {
            Debug.Log("Interaction Value ->" + obj.value + " : " + obj.phase + " : " + obj.deviceType + " : " + obj.binding.path + ":" + obj.actionName);

            StartCoroutine(m_LoaderUnit.LoadSceneAsync<AddressablesLoader>("Assets/Scenes/SampleScene.unity",  async (context) =>
            {
                // context.Activate();
                Debug.Log("Load Scene Complete!");
                DontDestroyOnLoad(gameObject);
                await Task.Delay(2000);
                Debug.Log("2.0s finish!");
                StartCoroutine(m_LoaderUnit.UnloadSceneAsync<AddressablesLoader>("Assets/Scenes/SampleScene.unity",  (callbackContext) =>
                {
                    Debug.Log("Unload Scene Complete");
                }, false));
            }, true, new LoadSceneParameters(LoadSceneMode.Additive), f =>
            {
                Debug.Log("Load Scene ... " + f);
            }));
            
            // m_Input.RemoveListener<InputSystemService>(obj.actionName);
        }

        private void OnJumpAction(InputServiceContext<bool> obj)
        {
            Debug.Log("Jump Value ->" + obj.value);
        }

        private void OnMoveRightAction(InputServiceContext<float> obj)
        {
            Debug.Log("Move Value ->" + obj.value);
            // m_Input.Disable<InputSystemService>();
            
            // m_Input.UnbindAction<InputSystemService, float>("Move/Right", OnMoveRightAction);
        }
        
        private void OnMoveRight1Action(InputServiceContext<float> obj)
        {
            Debug.Log("Move Value 1 ->" + obj.value);
            // m_Input.Disable<InputSystemService>();
            
            // m_Input.UnbindAction<InputSystemService, float>("Move/Right", OnMoveRightAction);
        }
        
        protected void Update()
        {
            // if (UnityEngine.Input.GetAxis("Horizontal") > 0.0001f)
            // {
            //     Debug.Log("Axis ->" + UnityEngine.Input.GetAxis("Horizontal"));
            // }
        }
    }
}