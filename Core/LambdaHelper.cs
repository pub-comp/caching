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
            if (exp is ConstantExpression constantExpression)
                return constantExpression.Value;

            if (exp is NewArrayExpression arrayExpression)
            {
                var lambda = Expression.Lambda<Func<object>>(arrayExpression);
                var value = lambda.Compile()();
                return value;
            }

            if (exp is ListInitExpression listExpression)
            {
                var lambda = Expression.Lambda<Func<object>>(listExpression);
                var value = lambda.Compile()();
                return value;
            }

            if (exp is MemberExpression memberExpression)
            {
                var memberInfo = memberExpression.Member;

                if (memberInfo is FieldInfo fieldInfo)
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

                if (memberInfo is PropertyInfo propertyInfo)
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
