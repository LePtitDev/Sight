namespace Sight.IoC
{
    /// <summary>
    /// Placeholders for dependencies in expressions given to
    /// <see cref="IoCExtensions.RegisterExpression{T}(ITypeContainer,System.Linq.Expressions.Expression{System.Func{T}},string?,bool)"/>.
    /// These methods are never executed, they are replaced by the resolved dependency when the service is resolved
    /// </summary>
    public static class Arg
    {
        /// <summary>
        /// Dependency that will be resolved from the container
        /// </summary>
        /// <param name="name">Name of the registration to resolve (optional, must be a constant)</param>
        public static T Of<T>(string? name = null)
        {
            throw new InvalidOperationException($"{nameof(Arg)}.{nameof(Of)} can only be used in an expression registered with RegisterExpression");
        }
    }
}
