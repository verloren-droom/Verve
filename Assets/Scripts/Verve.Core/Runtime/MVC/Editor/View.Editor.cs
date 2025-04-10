namespace Verve.MVC
{
#if UNITY_EDITOR && UNITY_5_3_OR_NEWER
    public partial class ViewBase
    {
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(m_ViewName))
            {
                m_ViewName = gameObject.name;
            }
        }
    }
#endif
}