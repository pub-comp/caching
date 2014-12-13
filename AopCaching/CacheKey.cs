using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubComp.Caching.AopCaching
{
    public struct CacheKey
    {
        private string className;
        private string methodName;
        private Type[] parameterTypes;
        private object[] parmaterValues;

        public CacheKey(string className, string methodName, Type[] parameterTypes, object[] parmaterValues)
        {
            this.className = className ?? string.Empty;
            this.methodName = methodName ?? string.Empty;
            this.parameterTypes = parameterTypes ?? new Type[0];
            this.parmaterValues = parmaterValues ?? new object[0];
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

        public string[] ParameterTypes
        {
            get
            {
                return this.parameterTypes.Select(t => t.FullName).ToArray();
            }
        }

        public object[] ParmaterValues
        {
            get
            {
                return this.parmaterValues;
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

            if (other.parameterTypes.Length != this.parameterTypes.Length)
                return false;

            if (other.parmaterValues.Length != this.parmaterValues.Length)
                return false;

            for (int cnt = 0; cnt < this.parameterTypes.Length; cnt++)
                if (other.parameterTypes[cnt] != this.parameterTypes[cnt])
                    return false;

            for (int cnt = 0; cnt < this.parmaterValues.Length; cnt++)
                if (other.parmaterValues[cnt] != this.parmaterValues[cnt])
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            var result = this.className.GetHashCode()
                ^ this.methodName.GetHashCode()
                ^ this.parameterTypes.Length
                ^ (Int32.MaxValue - this.parmaterValues.Length);

            for (int cnt = 0; cnt < this.parameterTypes.Length; cnt++)
            {
                Type current = this.parameterTypes[cnt];
                result ^= (current != null ? this.parameterTypes[cnt].GetHashCode() : 0);
            }

            for (int cnt = 0; cnt < this.parmaterValues.Length; cnt++)
            {
                object current = this.parmaterValues[cnt];
                result ^= (current != null ? this.parmaterValues[cnt].GetHashCode() : 0);
            }

            return result;
        }

        public override string ToString()
        {
            var result = Newtonsoft.Json.JsonConvert.SerializeObject(
                this,
                Newtonsoft.Json.Formatting.None,
                new Newtonsoft.Json.JsonSerializerSettings { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto });

            return result;
        }
    }
}
