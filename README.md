# caching

This project enables simple plug and play caching with various implementations and enables you to add your own implemenations.

NuGets (available at [nuget.org](http://nuget.org)):

The core - `ICache`, `CacheManager`, `LayeredCache`:
  [PubComp.Caching.Core](https://www.nuget.org/packages/PubComp.Caching.Core/)

`InMemoryCache` based on `System.Runtime.Caching.ObjectCache`:
  [PubComp.Caching.SystemRuntime](https://www.nuget.org/packages/PubComp.Caching.SystemRuntime/)

`RedisCache` based on `StackExchange.Redis`:
  [PubComp.Caching.RedisCaching](https://www.nuget.org/packages/PubComp.Caching.RedisCaching/)

AOP (Aspect Oriented Programming) caching based on `PostSharp`:
  [PubComp.Caching.AopCaching](https://www.nuget.org/packages/PubComp.Caching.AopCaching/)

You can access the cache directly via its interface:

~~~
ICache cache = new InMemoryCache("myLocalCache", new InMemoryPolicy());
MyData data = cache.Get("myKey", () => FallbackMethodToRun());
~~~

or 

~~~
ICache cache = new RedisCache("myRemoteCache", new RedisCachePolicy());
MyData data;
if (!cache.TryGet("myKey", out data))
{
    data = FallbackMethodToRun();
    cache.Set("myKey", data);
}
~~~

or use a PostSharp based aspect to wrap a method with cache:

~~~
[Cache]
MyData AMethodThatNeedsCaching(string parameter1, int parameter2)
{
    // Some long running operation
}
~~~

The cache can be created programatically:

~~~
var cache = new InMemoryCache("myOtherLocalCache", new InMemoryPolicy());
~~~

or received from the `CacheManager` which can be configured via code:

~~~
CacheManager.SetCache("noCache*", new NoCache("noCache"));
var cache = CacheManager.GetCache("noCache1");
~~~

or via config file:

~~~
  <configSections>
    <sectionGroup name="PubComp">
      <section
        name="CacheConfig"
        type="PubComp.Caching.Core.CacheConfigurationHandler, PubComp.Caching.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
        allowLocation="true"
        allowDefinition="Everywhere"
      />
  </configSections>
  
  <PubComp>
    <CacheConfig>
      <!-- You can either use ' or &quot; in the policy JSON -->
      <!-- Each cache can be of a different type e.g. local (PubComp.Caching.SystemRuntime) or Redis (PubComp.Caching.RedisCaching.RedisCache) and with a different policy -->
      <!-- Each cache type can have various policies e.g. ExpirationFromAdd, SlidingExpiration -->
      <!-- You can create your own cache type, just direct the config to your assembly and class -->
      <add name="localCache" assembly="PubComp.Caching.SystemRuntime" type="PubComp.Caching.SystemRuntime.InMemoryCache"
        policy="{'ExpirationFromAdd':'01:00:00'}" />
      <add name="localCacheLRU" assembly="PubComp.Caching.SystemRuntime" type="PubComp.Caching.SystemRuntime.InMemoryCache"
        policy="{&quot;SlidingExpiration&quot;:&quot;00:15:00&quot;}" />
    </CacheConfig>
  </PubComp>
~~~

usage:

~~~
var cache = CacheManager.GetCache("localCacheLRU");
~~~

or via AOP (Aspect Oriented Programming) - wrap a method with cache, so that the underlaying method is only called on cache misses:

~~~
[Cache("localCache")]
private List<Item> MethodToCache()
{
    // data retrieval goes here
}
~~~

You can also cache an IList of items, each time running the underlaying method only for the missing keys:

~~~
[CacheList(typeof(MockDataKeyConverter))]
public IList<MockData> GetItems(IList<string> keys)
{
	return keys.Select(k => new MockData { Id = k }).ToList();
}

public class MockData
{
    public string Id { get; set; }
    public string Value { get; set; }
}

public class MockDataKeyConverter : IDataKeyConverter<string, MockData>
{
    public string GetKey(MockData data)
    {
        return data.Id;
    }
}
~~~
