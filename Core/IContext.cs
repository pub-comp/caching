namespace PubComp.Caching.Core
{
    public interface IContext<TContext>
    {
        TContext Clone();
    }
}
