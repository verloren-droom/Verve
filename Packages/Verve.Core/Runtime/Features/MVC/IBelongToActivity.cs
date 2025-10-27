namespace Verve.MVC
{
    /// <summary>
    ///  <para>从属于哪个活动接口</para>
    /// </summary>
    public interface IBelongToActivity
    {
        IActivity GetActivity();
    }
}