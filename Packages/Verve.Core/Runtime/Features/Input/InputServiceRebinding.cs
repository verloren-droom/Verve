namespace Verve.Input
{
    [System.Serializable]
    public struct InputServiceRebinding
    {
        public string path;
        public int bindingIndex;
        public string cancelKey;
        public string filter;
    }
}