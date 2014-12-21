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
        private string[] parameterTypeNames;
        private object[] parmaterValues;

        public CacheKey(string className, string methodName, string[] parameterTypeNames, object[] parmaterValues)
        {
            this.className = className ?? string.Empty;
            this.methodName = methodName ?? string.Empty;
            this.parameterTypeNames = parameterTypeNames ?? new string[0];
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

            for (int cnt = 0; cnt < this.parameterTypeNames.Length; cnt++)
                if (other.parameterTypeNames[cnt] != this.parameterTypeNames[cnt])
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
                ^ this.parameterTypeNames.Length
                ^ (Int32.MaxValue - this.parmaterValues.Length);

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
