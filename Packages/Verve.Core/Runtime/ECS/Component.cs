namespace Verve.ECS
{
    /// <summary>
    /// ECS组件，建议仅用于数据存储不做逻辑处理
    /// </summary>
    public interface IComponentBase { }
    
    public interface ISharedComponentBase : IComponentBase {}
}