### 1.0.0:
	* Initial

### 1.0.1:
	* Improved CacheAttribute initialization
	* Fixed CacheManager registration of cache with any name ("*" with no-prefix) not working

### 1.0.2:
	* Removed unused field from aspect classes.

### 1.1.0:
	* Added DoNotIncludeInCacheKeyAttribute for marking specific parameters so that their values will not be a part of the key,
	  Example of usages:
	    a. Do not use a dbContext as part of a cache key.
		b. Cache using object ID, not object value.

### 1.2.0:
	* Change policy for InMemoryCache to InMemoryPolicy enabling AbsoluteExpiration, ExpirationFromAdd, SlidingExpiration
	* Added MongoDb based distributed cache with the above policy options
	* Moved CacheKey generator from AopCaching to Core
	* Added option to get auto-generated cache key (using CacheKey.GetKey()) to enable single item clear when using AopCaching

### 1.2.1:
	* Fixed NuGet package of MongoDbCaching - package was empty

### 1.2.2:
	* MongoDbCache - fixed race condition on initial get or add caused by using Add() instead of AddOrUpdate()
	* MongoDbCache - added support for null values
	* SystemRuntime.ObjectCache (InMemoryCache's base) - added support for null values
	* Test initialization fix for AopCaching's tests

### 1.3.0:
	* RedisCache - added
	* MongoDbCache - NuGet dependencies update
	* MongoDbCache - small change for sliding expiration - removed unnecessary updates

### 1.4.0:
	* Core - added LayeredCache

### 1.4.1:
	* MongoDbCache - small code cleanup
	* RedisCache - improved sliding expiration implementation - use GetOrSet() instead of Set() to clear previous expiration time


### 1.5.0:
	* WebApiExtended - added with basic cache controller

### 1.6.0:
	* Core - added CacheControllerUtil for enabling simple register, clear, refresh functionality
	* Revamped WebApiExteneded to use CacheControllerUtil and name change

### 1.6.1:
	* Fixed route for cache controllers
	* Check if parameter is null in GetCache(name)

### 1.6.2:
	* Fixed lifetime of cache registrations via CacheControllerUtil

### 1.7.0:
	* Added RegisterAllCaches functionality

### 1.7.1:
	* Bug fix for LambdaHelper: Add support for fields/properties as parameters in key generation expression

### 1.8.0:
	* Added built in cache notifications & synchronization
	* Changed RedisCache to use StackExchange
	* Implementation of notifier using Redis

### 2.0.0:
	* Revamped configuration for connection strings & notifications / synchronization

### 2.0.1:
	* Fixed unique key issue with AOP on generic methods or classes

### 2.0.2:
	* Fixed synchronizer/notifier + added test that spawns a test process (make sure Firewall/enSilo doesn't block it when running test)

### 2.0.3:
	* In order to enable clearing specific key when using the aspect CacheListAttribute,
	Fixed CacheKey class key generation for methods with array/list parameters
	- see test TestKeyGeneration_CacheList

### 2.1.0:
	* Added Converter options to RedisCache: json, bson, deflate, gzip

### 2.2.0:
	* Integration between WebAPI controller and Notifier to enable out of the box pub-sub

### 2.2.1:
	* Fix for RedisCache debug logs

### 3.0.0:
	* Added support for adding DoNotIncludeInCacheKeyAttribute to properties.
	For this to work, the class DoNotIncludeInCacheKeyAttribute was moved from AopCaching to Core.Attributes,
	which is a breaking change for clients of AopCaching using DoNotIncludeInCacheKeyAttribute
	* Changed RedisConverterDeflate and RedisConverterGZip to compress JSON instead of BSON
	* Updated Newtonsoft.Json (Json.net) from v6 to v11
	* All other packages have not been changed

### 3.1.0:
	* Added async support for ICache and all implementations

### 3.1.1:
	* Fixed performance issue in CacheKey serialization

### 3.1.2:
	* Fixed deserialization issue when using RedisCache and AOP with various primitive types e.g. Int32

### 3.1.3:
	* Fixed connection issue when using RedisCache with a DNS
	
### 3.1.4:
	* Move the the logic of AOP safe casting to runtime (instead of compile time) to avoid a breaking change

### 3.1.5:
	* Fix DoNotIncludeInCacheKeyAttribute not effecting parameters

### 3.1.6:
	* Fix DoNotIncludeInCacheKeyAttribute in CacheListAttribute not working

### 4.0.0:
	* Update RedisCaching to StackExchange.Redis v2
	* Change minimum .NET Framework version from 4.5 to 4.6.1 in all projects
  
### 4.0.1
	* Added TryGetAsync implementation to RedisCache and made CacheContext async.

### 4.1.0:
	* Migrate NuGet projects to .Net standard
	* Migrate all projects to new csproj

### 4.1.1:
	* Fixes due to NuGetPack.exe issues

### 4.2.0:
	* DoNotLock policy for SystemRuntime (InMemoryCache)

### 4.3.0:
	* Improved locking mechanism - using multiple SemaphoreSlim depending on cache and key's hash
	* Improved locking mechanism - enable reentrant (recursive) calls without deadlocks

### 4.4.0:
	* CacheManager enhanced to enable support for .NET standard configuration (Microsoft.Extensions.Configuration)

### 4.5.0:
	* RedisClient
	  - Optimization: Reuse ConnectionMultiplexer when using RedisCache/SyncProvider

	* RedisNotifier
	  - New optional policy: GeneralInvalidationChannel
	  - New method: TryPublish

	* InMemory*Cache
	  - New optional policy: OnSyncProviderFailure
		- Automatic Invalidation on SyncProvider state change
		- Fallback expiry policy

	* Redis*Cache
	  - New property: ICacheState.IsActive => RedisClient.IsConnected
	  - Bug fix: Invalid AbsoluteExpiration handling

	* LayeredCache
	  - New optional policy: InvalidateLevel1OnLevel2Upsert 

	* New IScopedCache (:ICache)
	  - TryGetScoped
	  - GetScoped
	  - SetScoped
	  - New property: ICacheState.IsActive => RedisClient.IsConnected
	* New cache types (implements ICache and IScopedCache)
	  - InMemoryScopedCache
	  - RedisScopedCache
	  - LayeredScopedCache

	* ICacheState for cache types
	  - New behavior: Won't throw an exception is cache is inactive when used by AOP or by by cache implementing IScopedCache (RedisScopedCache)
	* ICacheGetPolicy for cache types
	  - New behevior: Expose state and censored configurations of cache

	* WebApiExtended/.NETCore
	  - New optional parameter: IncludeConfiguration - now can serve state and censored configuration of defined cache
	  - New: CacheableActionFilterAttribute for an easy and consistant way to work with IScopedCache CacheDirectives
	
	* New packages: WebApiExtended.Swashbuckle and  WebApiExtended.NETCore.Swashbuckle
	  - Expose CacheDirectives for endpoints with CacheableActionFilterAttribute

