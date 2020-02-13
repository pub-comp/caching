namespace PubComp.Caching.Core
{
    public interface ICacheV2 : ICache
    {
        bool IsActive { get; }
        object GetDetails();
    }
}