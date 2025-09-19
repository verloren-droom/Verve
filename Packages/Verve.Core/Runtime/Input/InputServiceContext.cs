namespace Verve.Input
{
    [System.Serializable]
    public struct InputServiceContext<T> where T : struct
    {
        public T value;
        public string actionName;
        public InputServicePhase phase;
        public InputServiceDeviceType deviceType;
        public InputServiceBinding binding;
    }
}