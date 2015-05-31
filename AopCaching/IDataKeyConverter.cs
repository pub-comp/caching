namespace PubComp.Caching.AopCaching
{
    public interface IDataKeyConverter<TKey, TData>
    {
        TKey GetKey(TData data);
    }
}
