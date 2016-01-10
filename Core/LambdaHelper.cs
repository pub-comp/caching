using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace PubComp.Caching.Core
{
    internal static class LambdaHelper
    {
        public static void GetMethodInfoAndArguments(
            Expression<Action> expression, out MethodInfo methodInfo, out object[] arguments)
        {
            GetMethodInfoAndArguments((LambdaExpression)expression, out methodInfo, out arguments);
        }

        public static void GetMethodInfoAndArguments<T>(
            Expression<Action<T>> expression, out MethodInfo methodInfo, out object[] arguments)
        {
            GetMethodInfoAndArguments((LambdaExpression)expression, out methodInfo, out arguments);
        }

        public static void GetMethodInfoAndArguments<TResult>(
            Expression<Func<TResult>> expression, out MethodInfo methodInfo, out object[] arguments)
        {
            GetMethodInfoAndArguments((LambdaExpression)expression, out methodInfo, out arguments);
        }

        public static void GetMethodInfoAndArguments<T, TResult>(
            Expression<Func<T, TResult>> expression, out MethodInfo methodInfo, out object[] arguments)
        {
            GetMethodInfoAndArguments((LambdaExpression)expression, out methodInfo, out arguments);
        }

        private static void GetMethodInfoAndArguments(
            LambdaExpression expression, out MethodInfo methodInfo, out object[] arguments)
        {
            MethodCallExpression outermostExpression = expression.Body as MethodCallExpression;

            if (outermostExpression == null)
            {
                throw new ArgumentException("Invalid Expression. Expression should consist of a Method call only.");
            }

            methodInfo = outermostExpression.Method;
            arguments = outermostExpression.Arguments.Select(GetValue).ToArray();
        }

        private static object GetValue(Expression exp)
        {
            // E.g. 2.0
            var constantExpression = exp as ConstantExpression;
            if (constantExpression != null)
                return constantExpression.Value;

            var memberExpression = exp as MemberExpression;
            if (memberExpression != null)
            {
                var memberInfo = memberExpression.Member;

                var fieldInfo = memberInfo as FieldInfo;
                if (fieldInfo != null)
                {
                    if (fieldInfo.IsStatic)
                    {
                        return fieldInfo.GetValue(null);
                    }
                    else
                    {
                        var obj = GetValue(memberExpression.Expression);
                        return fieldInfo.GetValue(obj);
                    }
                }

                var propertyInfo = memberInfo as PropertyInfo;
                if (propertyInfo != null)
                {
                    if (propertyInfo.GetMethod.IsStatic)
                    {
                        return propertyInfo.GetValue(null);
                    }
                    else
                    {
                        var obj = GetValue(memberExpression.Expression);
                        return propertyInfo.GetValue(obj);
                    }
                }
            }

            throw new NotSupportedException("Can not read parameter value of type: " + exp.Type.Name);
        }
    }
}
