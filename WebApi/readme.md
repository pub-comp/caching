# Purpose
The purpose of this project is to allow hosting a simple WebApi service to use for testing performances of parallel cache access using conventional load test tools.

# Usage
## ab
[Apache Benchmark](https://httpd.apache.org/docs/2.4/programs/ab.html) is a powerful tool that can be used to give performance statistics for concurrent load tests and displays the results right in your console. 

For example, you can compare the results from these two commands to see the performance difference between the sync and async implementations under load (500 requests at a concurrency rate of 64):

```
.\ab.exe -n 500 -c 64 http://localhost:53328/api/example/v1/99
.\ab.exe -n 500 -c 64 http://localhost:53328/api/example/v1/async/99
````

## Other tools
You can test the API with any tool capable of making HTTP calls to a REST API (i.e. [JMeter](https://jmeter.apache.org/), [Visual Studio load test](https://docs.microsoft.com/en-us/vsts/test/load-test/run-performance-tests-app-before-release?view=vsts), etc.)