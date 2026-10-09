using System.Linq.Expressions;
using System.Reflection;

namespace Sight.IoC.Internal
{
    internal static class ExpressionHelpers
    {
        private static readonly MethodInfo ArgOfMethod = typeof(Arg).GetMethod(nameof(Arg.Of))!;

        /// <summary>
        /// Replace every <see cref="Arg.Of{T}"/> call by an item of a parameter array and compile the expression
        /// </summary>
        public static (Func<object?[], T> Factory, RegistrationId[] Dependencies) Compile<T>(Expression<Func<T>> expression)
        {
            var visitor = new ArgVisitor();
            var body = visitor.Visit(expression.Body)!;
            if (body.Type != typeof(T))
                body = Expression.Convert(body, typeof(T));

            var factory = Expression.Lambda<Func<object?[], T>>(body, visitor.Parameter).Compile();
            return (factory, visitor.Dependencies.ToArray());
        }

        private sealed class ArgVisitor : ExpressionVisitor
        {
            public ParameterExpression Parameter { get; } = Expression.Parameter(typeof(object?[]), "args");

            public List<RegistrationId> Dependencies { get; } = new List<RegistrationId>();

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (!node.Method.IsGenericMethod || node.Method.GetGenericMethodDefinition() != ArgOfMethod)
                    return base.VisitMethodCall(node);

                if (node.Arguments[0] is not ConstantExpression nameExpression)
                    throw new IoCException($"The name given to {nameof(Arg)}.{nameof(Arg.Of)} must be a constant ({node})");

                var type = node.Method.GetGenericArguments()[0];
                var index = Dependencies.Count;
                Dependencies.Add(new RegistrationId(type) { Name = (string?)nameExpression.Value });

                return Expression.Convert(Expression.ArrayIndex(Parameter, Expression.Constant(index)), type);
            }
        }
    }
}
