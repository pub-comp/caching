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

            throw new NotSupportedException("Can not read parameter value of type: " + exp.Type.Name);
        }
    }
}
