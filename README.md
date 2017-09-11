# caching

This project enables simple plug and play caching with various implementations and enables you to add your own implemenations.

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
      <add name="localCache" assembly="PubComp.Caching.SystemRuntime" type="PubComp.Caching.SystemRuntime.InMemoryCache"
        policy="{&quot;ExpirationFromAdd&quot;:&quot;01:00:00&quot;}" />
      <add name="localCacheLRU" assembly="PubComp.Caching.SystemRuntime" type="PubComp.Caching.SystemRuntime.InMemoryCache"
        policy="{&quot;SlidingExpiration&quot;:&quot;00:15:00&quot;}" />
    </CacheConfig>
  </PubComp>
~~~

usage:

~~~
var cache = CacheManager.GetCache("localCacheLRU");
~~~

or

~~~
[Cache("localCache")]
private List<Item> MethodToCache()
{
    // data retrieval goes here
}
~~~
