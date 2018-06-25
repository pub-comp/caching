using System;
using System.Linq;
using System.Reflection;

namespace PubComp.Caching.AopCaching
{
    public static class SafeCasting
    {
        public static object CastTo(Type targetType, object value)
        {
            // Null
            if (value == null)
                return null;
            
            var sourceType = value.GetType();

            // Same type or assignable
            if (sourceType == targetType || targetType.IsAssignableFrom(sourceType))
                return value;

            if (value is IConvertible)
            {
                var convertableMethod = typeof(IConvertible).GetMethods().FirstOrDefault(m =>
                    m.ReturnType == targetType
                    && m.Name.StartsWith("To")
                    && m.GetParameters().Length == 1
                    && m.GetParameters().First().ParameterType == typeof(IFormatProvider));

                if (convertableMethod != null)
                    return convertableMethod.Invoke(value, new object[] { null });
            }

            if (targetType.IsEnum)
                return Enum.ToObject(targetType, value);

            // Value has (target) operator
            var fromSource = sourceType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    (m.Name == "op_Implicit" || m.Name == "op_Explicit")
                    && m.ReturnType == targetType
                    && m.GetParameters().Length == 1
                    && m.GetParameters().First().ParameterType == sourceType);

            if (fromSource != null)
                return fromSource.Invoke(null, new[] { value });

            // Target has (source) operator
            var toTarget = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    (m.Name == "op_Implicit" || m.Name == "op_Explicit")
                    && m.ReturnType == targetType
                    && m.GetParameters().Length == 1
                    && m.GetParameters().First().ParameterType == sourceType);

            if (toTarget != null)
                return toTarget.Invoke(null, new[] { value });

            throw new NotSupportedException($"Can not cast value from {sourceType} to {targetType}!");
        }
    }
}
