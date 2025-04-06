namespace Verve.MVC
{
    public interface ICommand
    {
        void Execute();
        void Undo();
    }
    
    public abstract class CommandBase : ICommand
    {
        public abstract void Execute();
        public virtual void Undo() { }
    }
}