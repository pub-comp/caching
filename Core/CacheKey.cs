using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json;
using PubComp.Caching.Core.Attributes;

namespace PubComp.Caching.Core
{
    public struct CacheKey
    {
        private readonly string className;
        private readonly string methodName;
        private readonly string[] parameterTypeNames;
        private readonly string[] genericArgumentsTypeNames;
        private readonly object[] parmaterValues;

        private static readonly JsonSerializerSettings JsonSerializerSettings =
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ContractResolver = new JsonIgnorePropertiesContractResolver(
                    typeof(DoNotIncludeInCacheKeyAttribute))
            };

        public CacheKey(string className, string methodName, string[] parameterTypeNames, object[] parmaterValues, string[] genericArgumentsTypeNames)
        {
            this.className = className ?? string.Empty;
            this.methodName = methodName ?? string.Empty;
            this.parameterTypeNames = parameterTypeNames ?? new string[0];
            this.parmaterValues = parmaterValues ?? new object[0];
            this.genericArgumentsTypeNames = genericArgumentsTypeNames ?? new string[0];
        }

        public string ClassName
        {
            get
            {
                return this.className;
            }
        }

        public string MethodName
        {
            get
            {
                return this.methodName;
            }
        }

        public string[] ParameterTypeNames
        {
            get
            {
                return this.parameterTypeNames;
            }
        }

        public object[] ParmaterValues
        {
            get
            {
                return this.parmaterValues;
            }
        }

        public string[] GenericArgumentsTypeNames
        {
            get
            {
                return this.genericArgumentsTypeNames;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is CacheKey == false)
                return false;

            var other = (CacheKey)obj;

            if (other.className != this.className)
                return false;

            if (other.methodName != this.methodName)
                return false;

            if (other.className != this.className)
                return false;

            if (other.parameterTypeNames.Length != this.parameterTypeNames.Length)
                return false;

            if (other.parmaterValues.Length != this.parmaterValues.Length)
                return false;

            if (other.genericArgumentsTypeNames.Length != this.genericArgumentsTypeNames.Length)
                return false;

            for (int cnt = 0; cnt < this.parameterTypeNames.Length; cnt++)
                if (other.parameterTypeNames[cnt] != this.parameterTypeNames[cnt])
                    return false;

            for (int cnt = 0; cnt < this.parmaterValues.Length; cnt++)
                if (other.parmaterValues[cnt] != this.parmaterValues[cnt])
                    return false;

            for (int cnt = 0; cnt < this.genericArgumentsTypeNames.Length; cnt++)
                if (other.genericArgumentsTypeNames[cnt] != this.genericArgumentsTypeNames[cnt])
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            var result = this.className.GetHashCode()
                ^ this.methodName.GetHashCode()
                ^ this.parameterTypeNames.Length
                ^ (Int32.MaxValue - this.parmaterValues.Length)
                ^ this.genericArgumentsTypeNames.Length;

            for (int cnt = 0; cnt < this.parameterTypeNames.Length; cnt++)
            {
                string current = this.parameterTypeNames[cnt];
                result ^= (current != null ? this.parameterTypeNames[cnt].GetHashCode() : 0);
            }

            for (int cnt = 0; cnt < this.parmaterValues.Length; cnt++)
            {
                object current = this.parmaterValues[cnt];
                result ^= (current != null ? this.parmaterValues[cnt].GetHashCode() : 0);
            }

            for (int cnt = 0; cnt < this.genericArgumentsTypeNames.Length; cnt++)
            {
                string current = this.genericArgumentsTypeNames[cnt];
                result ^= (current != null ? this.genericArgumentsTypeNames[cnt].GetHashCode() : 0);
            }

            return result;
        }

        public override string ToString()
        {
            var result = JsonConvert.SerializeObject(
                this,
                Formatting.None,
                JsonSerializerSettings);

            return result;
        }

        public static string GetKey(MethodBase method, params object[] parameterValues)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            var classType = method.DeclaringType;
            var className = (classType != null) ? classType.FullName : string.Empty;

            var parameters = method.GetParameters();
            var parameterTypeNames = parameters.Select(p => p.ParameterType.FullName).ToArray();
            var genericArgumentsTypeNames = method.GetGenericArguments().Select(a => a.FullName).ToArray();

            var key = new CacheKey(className, method.Name, parameterTypeNames, parameterValues, genericArgumentsTypeNames);
            return key.ToString();
        }

        public static string GetKey(Expression<Action> expression)
        {
            MethodInfo methodInfo;
            object[] arguments;
            LambdaHelper.GetMethodInfoAndArguments(expression, out methodInfo, out arguments);
            return GetKey(methodInfo, parameterValues: arguments);
        }

        public static string GetKey<T>(Expression<Action<T>> expression)
        {
            MethodInfo methodInfo;
            object[] arguments;
            LambdaHelper.GetMethodInfoAndArguments(expression, out methodInfo, out arguments);
            return GetKey(methodInfo, parameterValues: arguments);
        }

        public static string GetKey<TResult>(Expression<Func<TResult>> expression)
        {
            MethodInfo methodInfo;
            object[] arguments;
            LambdaHelper.GetMethodInfoAndArguments(expression, out methodInfo, out arguments);
            return GetKey(methodInfo, parameterValues: arguments);
        }

        public static string GetKey<T, TResult>(Expression<Func<T, TResult>> expression)
        {
            MethodInfo methodInfo;
            object[] arguments;
            LambdaHelper.GetMethodInfoAndArguments(expression, out methodInfo, out arguments);
            return GetKey(methodInfo, parameterValues: arguments);
        }
    }
}
