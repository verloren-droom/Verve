namespace Verve.AI
{
    public class AINode
    {
        public virtual bool Check() => false;
        public virtual void Run() {}
    }
}