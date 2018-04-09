using System;
using System.Collections.Generic;
using PubComp.Caching.Core.Attributes;

namespace PubComp.Caching.AopCaching.UnitTests.Mocks
{
    public class Service1
    {
        [Cache]
        public IEnumerable<string> MethodToCache1()
        {
            return new string[] { "1", "2", "3", "4", "5" };
        }

        [Cache]
        public IEnumerable<string> MethodToCache1(int number)
        {
            for (int cnt = 1; cnt <= number; cnt++)
                yield return cnt.ToString();
        }

        [Cache]
        public IEnumerable<string> MethodToCache1(double number)
        {
            for (double cnt = 0.9; cnt <= number; cnt++)
                yield return cnt.ToString();
        }

        [Cache]
        public IEnumerable<string> MethodToCache1(object obj)
        {
            Func<object, string> f = o => (o ?? "null").ToString();
            var st = f(obj);

            return new[] { st, st, st };
        }

        [Cache]
        public IEnumerable<string> MethodToCache2<TObject>(TObject obj)
        {
            Func<object, string> f = o => (o ?? "null").ToString();
            var st = f(obj);

            return new[] { st, st, st };
        }

        [Cache]
        public string MethodToCache1(int id, [DoNotIncludeInCacheKey]object obj)
        {
            return id.ToString() + (obj != null ? obj.GetHashCode() : 0).ToString();
        }

        [Cache]
        public string MethodToCache1(ClassA a)
        {
            return a.Id + a.Value;
        }

        public readonly double ConstValue = 4.0;
    }

    public class ClassA
    {
        public int Id { get; set; }
        [DoNotIncludeInCacheKey]
        public string Value { get; set; }

        public ClassA(int id, string value)
        {
            Id = id;
            Value = value;
        }
    }
}