namespace Verve
{
    public abstract class ComponentBase :
#if UNITY_5_3_OR_NEWER
        UnityEngine.MonoBehaviour
#elif GODOT
        Godot.Node
    #endif
    {
        public new string name
        {
            get =>
#if UNITY_5_3_OR_NEWER
                base.name;
#elif GODOT
                GetName();
#endif
            set =>
#if UNITY_5_3_OR_NEWER
                base.name = value;
#elif GODOT
                SetName(vaule);
#endif
        }
        
        public new bool enabled
        {
#if UNITY_5_3_OR_NEWER
            get => base.enabled;
            set => base.enabled = value;
#elif GODOT
            get => GetProcess();
            set => SetProcess(value);
#endif
        }
    
        public new bool isActiveAndEnabled
        {
#if UNITY_5_3_OR_NEWER
            get => base.isActiveAndEnabled;
#elif GODOT
            get => IsInsideTree() && enabled;
#endif
        }

        public new string tag
        {
#if UNITY_5_3_OR_NEWER
            get => base.tag;
            set => base.tag = value;
#elif GODOT
            get => "Default";
            set { }
#endif
        }

        public new void BroadcastMessage(string methodName)
        {
#if UNITY_5_3_OR_NEWER
            base.BroadcastMessage(methodName);
#elif GODOT
            GetTree().CallGroup(methodName);
#endif
        }
    
        public new bool CompareTag(string tag)
        {
#if UNITY_5_3_OR_NEWER
            return base.CompareTag(tag);
#elif GODOT
            return false;
#endif
        }

        protected virtual void Awake()
        {
#if UNITY_5_3_OR_NEWER
            
#elif GODOT
            _enter_tree();
#endif
        }
    
        protected virtual void Start()
        {
#if UNITY_5_3_OR_NEWER
            
#elif GODOT
            _ready();
#endif
        }
    
        protected virtual void Update()
        {
#if UNITY_5_3_OR_NEWER
            
#elif GODOT
            _process(1.0f / 60.0f);
#endif
        }
    
        protected virtual void FixedUpdate()
        {
#if UNITY_5_3_OR_NEWER
            
#elif GODOT
            _physics_process(0.02f);
#endif
        }
    
        protected virtual void LateUpdate()
        {
#if UNITY_5_3_OR_NEWER
           
#elif GODOT
            
#endif
        }
    
        protected virtual void OnDestroy()
        {
#if UNITY_5_3_OR_NEWER
            
#elif GODOT
            _exit_tree();
#endif
        }
    
        protected virtual void OnDisable()
        {
#if UNITY_5_3_OR_NEWER
            
#elif GODOT
            _exit_tree();
#endif
        }
    
        protected virtual void OnEnable()
        {
#if UNITY_5_3_OR_NEWER
            
#elif GODOT
            _enter_tree();
#endif
        }
    }
}