# pub-comp/caching (PubComp.Caching)

This project enables simple plug and play caching with various implementations and enables you to add your own implemenations.

## Topics

- [Version History](/HISTORY.md)
- [NuGets](#nugets)
- [Basic Usage](#basic-usage)
  - [TryGet](#tryget)
  - [Get](#get)
  - [AOP Wrapper](#aop-wrapper)
  - [Clear Item](#clear-item)
  - [Clear All](#clear-all)
- [Cache Types And Policies](#cache-types-and-policies)
- [Cache Management](#cache-management)
  - [Using A Defined Cache](#using-a-defined-cache)
- [AOP Caching](#aop-caching)
  - [Getting The Key](#getting-the-key)
  - [Caching Multiple Items By Key](#caching-multiple-items-by-key)
  - [Ignoring Specific Parameters](#ignoring-specific-parameters)
- [Extending With Your Own Classes](#extending-with-your-own-classes)
  - [Adding Your Own Cache Type](#adding-your-own-cache-type)

## NuGets

(available at [nuget.org](http://nuget.org))

The core - `ICache`, `CacheManager`, `LayeredCache`:
  [PubComp.Caching.Core](https://www.nuget.org/packages/PubComp.Caching.Core/)

`InMemoryCache` based on `System.Runtime.Caching.ObjectCache`:
  [PubComp.Caching.SystemRuntime](https://www.nuget.org/packages/PubComp.Caching.SystemRuntime/)

`RedisCache` based on `StackExchange.Redis`:
  [PubComp.Caching.RedisCaching](https://www.nuget.org/packages/PubComp.Caching.RedisCaching/)

AOP (Aspect Oriented Programming) caching based on `PostSharp`:
  [PubComp.Caching.AopCaching](https://www.nuget.org/packages/PubComp.Caching.AopCaching/)

## Basic Usage

You can access the cache directly via its interface:

### TryGet

~~~csharp
ICache cache = new RedisCache("myRemoteCache", new RedisCachePolicy());
MyData data;
if (!cache.TryGet("myKey", out data))
{
    data = FallbackMethodToRun();
    cache.Set("myKey", data);
}
~~~

### Get

or 

~~~csharp
MyData data = cache.Get("myKey", () => FallbackMethodToRun());
~~~

this single line of code above is equivalent to the mulitple lines of code in the above [TryGet](#tryget) example
in the above usage, the method will be run only if the requested key is not in the cache (on cache miss).

### AOP Wrapper

or use a PostSharp based aspect to wrap a method with cache:

~~~csharp
[Cache]
MyData AMethodThatNeedsCaching(string parameter1, int parameter2)
{
    // Some long running operation
}
~~~

calling the above method (e.g. with parameter values "one", 2) will be equivalent to calling a non aspect declared method with:

~~~csharp
var key = "{"ClassName":"MyNameSpace.MyClassName","MethodName":"MyMethod","ParameterTypeNames":["System.String","System.Int32"],"ParmaterValues":["one",2]}";
MyData result = myCache.Get(key, () => MyMethod("one", 2));
~~~

### Clear Item

You can clear a specific item from the cache:

~~~csharp
myCache.Clear("myKey");
~~~

### Clear All

or clear the entire cache (all items):

~~~csharp
myCache.ClearAll();
~~~

If you have multiple caches, clearing one will NOT affect the other e.g.

~~~csharp
ICache cache1 = new InMemoryCache("myLocalCache", new InMemoryPolicy());
ICache cache2 = new InMemoryCache("myLocalCache", new InMemoryPolicy());

cache1.ClearAll(); // Only cache1 is cleared
~~~

## Cache Types And Policies

You can create instances of cache from different types cache e.g. InMemoryCache, RedisCache
and instances of the same type with different policies e.g. no automatic expiration, expiration from add, sliding expiration (LRU - Least Recently Used).

When you create a cache, you pass a name and the policy.

You can use the `CacheManager` to get a cache with a specific name e.g.

~~~csharp
var cache = CacheManager.GetCache("myLocalCache");
~~~

## Cache Management

A cache can be created programatically:

~~~csharp
var cache = new InMemoryCache("myOtherLocalCache", new InMemoryPolicy());
~~~

or received from the `CacheManager` which can be configured via code:

~~~csharp
CacheManager.SetCache("noCache*", new NoCache("noCache"));
var cache = CacheManager.GetCache("noCache1");
~~~

or via config file:

~~~xml
  <configSections>
    <sectionGroup name="PubComp">
      <section
        name="CacheConfig"
        type="PubComp.Caching.Core.CacheConfigurationHandler, PubComp.Caching.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
        allowLocation="true"
        allowDefinition="Everywhere"
      />
    </sectionGroup>
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
        policy="{'SlidingExpiration':'00:15:00'}" />
      <!-- policy="{&quot;ExpirationFromAdd&quot;:&quot;'01:00:00&quot;}" would work too -->
    </CacheConfig>
  </PubComp>
~~~

### Using A Defined Cache

You can request a cache, by name, from the `CacheManager`:

~~~csharp
var cache = CacheManager.GetCache("localCacheLRU");
~~~

or via AOP (Aspect Oriented Programming)...

Wrap a method with cache, so that the underlaying method is only called on cache misses:

~~~csharp
[Cache("localCache")]
private List<Item> MethodToCache()
{
    // data retrieval goes here
}
~~~

(*) When using AOP, the aspect only requests the cache from `CacheManager` once, for optimization, therefore changing the registeration of the cache (using `SetCache` won't have an effect later on).

You can also cache an IList of items, each time running the underlaying method only for the missing keys:

## AOP Caching

Using AOP, you can easily wrap a method with a lazy-loading cache:

~~~csharp
[Cache("localCache")]
private List<Item> MethodToCache()
{
    // data retrieval goes here
}
~~~

The return value will be cached under a unique key, built from the class' full name (namespace and name), the method name, the parameter types and the parameter value.

so that the following methods will not have the same key:

~~~csharp
[Cache("localCache")]
private List<Item> MethodToCache(string x)
{
    // data retrieval goes here
}

[Cache("localCache")]
private List<Item> MethodToCache(int x)
{
    // data retrieval goes here
}

[Cache("localCache")]
private List<Item> MethodToCache(byte x)
{
    // data retrieval goes here
}
~~~

### Getting The Key

You can get the key used by the `Cache` aspect (`CacheAttribute`), in order to enable manual clearing of the data like so:

~~~csharp
var key = CacheKey.GetKey(() => service.MethodToCache("parameterValue"));
~~~

and then clear as usual:

~~~csharp
CacheManager.GetCache("localCache").Clear(key);
~~~

### Caching Multiple Items By Key

You can use the `CacheList` aspect (`CacheListAttribute`) to cache data item by item.
When using this way, the underlaying method (e.g. DB access) is called only for keys missing in the cache (and only if any are missing).

Example:

~~~csharp
// Both IDs parameter and return type have to be of type IList<>
[CacheList(typeof(MockDataKeyConverter))]
public IList<MockData> GetItems(IList<string> keys)
{
    using (var context = new MyDbContext())
    {
        return content.MyData.Where(d => keys.Contains(d.Id)).ToList();
    }
}

// Example data type
public class MockData
{
    public string Id { get; set; }
    public string Value { get; set; }
}

// This class enables the aspect, given a result what its key (ID) is
public class MockDataKeyConverter : IDataKeyConverter<string, MockData>
{
    public string GetKey(MockData data)
    {
        return data.Id;
    }
}
~~~

If the keys are not the first parameter of the method, use `keyParameterNumber` to instruct the aspect which parameter to use (0-based):

~~~csharp
[CacheList(typeof(MockDataKeyConverter), keyParameterNumber = 1)]
public IList<MockData> GetItems(string parameter0, IList<string> keys)
{
    using (var context = new MyDbContext())
    {
        return content.MyData.Where(d => keys.Contains(d.Id)).ToList();
    }
}
~~~

### Ignoring Specific Parameters

You can instruct the aspects (`Cache` and `CacheList`) to ignore a specific parameter when generating the cache-key
using the `DoNotIncludeInCacheKey` attribute (`DoNotIncludeInCacheKeyAttribute`):

~~~csharp
[Cache]
public string MethodToCache1(int id, [DoNotIncludeInCacheKey]object obj)
{
}
~~~

## Extending With Your Own Classes

You can extend this project with you own classes by creating an assembly (a class library project) with the following classes:

### Adding Your Own Cache Type

* YourCache - the actual cache's code. Must implement `ICache`
* YourCacheConfig - a factory for creating an instance of your cache from a given policy. Must inherit from `CacheConfig`.
* YourCachePolicy - the policy for your cache - any parameters you need for constructing your cache e.g. eviction policy, connection details. Must be serializable to from JSON.

You will then be able to configure usage of your cache like so:

~~~xml
<PubComp>
  <CacheConfig>
    <add name="cacheName" assembly="YourAssembly" type="YourType"
      policy="JSON goes here" />
  </CacheConfig>
</PubComp>
~~~
