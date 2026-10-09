using System.Reflection;
using System.Text;

namespace Sight.IoC.Internal;

/// <summary>
/// Explanation of a resolution failure (a message and its causes)
/// </summary>
internal sealed class ResolveFailure
{
    public ResolveFailure(string message, IEnumerable<ResolveFailure>? causes = null)
    {
        Message = message;
        Causes = causes?.ToList() ?? new List<ResolveFailure>();
    }

    public string Message { get; }

    public List<ResolveFailure> Causes { get; }
}

/// <summary>
/// Build detailed messages that explain why a service cannot be resolved.
/// Everything here runs only after a failure, so the resolution path is not impacted
/// </summary>
internal static class ResolveDiagnostics
{
    private const int MaxDepth = 8;

    /// <summary>
    /// Build an exception message with a headline followed by the tree of causes
    /// </summary>
    public static string BuildMessage(string headline, Func<IReadOnlyList<ResolveFailure>> causesProvider)
    {
        IReadOnlyList<ResolveFailure> causes;
        try
        {
            causes = causesProvider();
        }
        catch (Exception ex)
        {
            // Diagnostics must never hide the original failure
            causes = new[] { new ResolveFailure($"(unable to determine the cause: {ex.Message})") };
        }

        var bld = new StringBuilder(headline);
        Append(bld, causes, 1);
        return bld.ToString();
    }

    /// <summary>
    /// Why a service identifier cannot be resolved
    /// </summary>
    public static IReadOnlyList<ResolveFailure> ExplainIdentifier(ITypeResolver resolver, RegistrationId identifier, ResolveOptions options)
    {
        return ExplainIdentifier(resolver, identifier, options, new List<Type>());
    }

    /// <summary>
    /// Why a concrete type cannot be created by calling one of its constructors
    /// </summary>
    public static IReadOnlyList<ResolveFailure> ExplainAutoCreate(ITypeResolver resolver, Type type, ResolveOptions options)
    {
        return ExplainAutoCreate(resolver, type, options, new List<Type>());
    }

    /// <summary>
    /// Why the parameters of a method cannot be injected
    /// </summary>
    public static IReadOnlyList<ResolveFailure> ExplainMethod(ITypeResolver resolver, MethodBase method, ResolveOptions options)
    {
        return ExplainParameters(resolver, method, options, new List<Type>());
    }

    public static string GetTypeName(Type type)
    {
        if (type.IsArray)
            return GetTypeName(type.GetElementType()!) + "[" + new string(',', type.GetArrayRank() - 1) + "]";

        if (!type.IsGenericType)
            return type.FullName ?? type.Name;

        var name = type.GetGenericTypeDefinition().FullName ?? type.Name;
        var tick = name.IndexOf('`');
        if (tick >= 0)
            name = name.Substring(0, tick);

        return type.IsConstructedGenericType
            ? $"{name}<{string.Join(", ", type.GetGenericArguments().Select(GetTypeName))}>"
            : $"{name}<{new string(',', type.GetGenericArguments().Length - 1)}>";
    }

    public static string Describe(RegistrationId identifier)
    {
        return identifier.Name == null ? $"'{GetTypeName(identifier.Type)}'" : $"'{GetTypeName(identifier.Type)}' (name: '{identifier.Name}')";
    }

    private static string Describe(Registration registration)
    {
        var types = string.Join(", ", registration.Types.Select(GetTypeName));
        return registration.Name == null ? $"[{types}]" : $"[{types}] (name: '{registration.Name}')";
    }

    private static string Describe(MethodBase method)
    {
        var name = method is ConstructorInfo ? method.DeclaringType!.Name : method.Name;
        return $"{name}({string.Join(", ", method.GetParameters().Select(x => $"{GetTypeName(x.ParameterType)} {x.Name}"))})";
    }

    private static IReadOnlyList<ResolveFailure> ExplainIdentifier(ITypeResolver resolver, RegistrationId identifier, ResolveOptions options, List<Type> stack)
    {
        var type = identifier.Type;
        if (stack.Contains(type))
            return new[] { new ResolveFailure($"Circular dependency on '{GetTypeName(type)}'") };

        if (stack.Count >= MaxDepth)
            return new[] { new ResolveFailure("...") };

        stack.Add(type);
        try
        {
            var causes = new List<ResolveFailure>();
            if (!options.NewInstance)
            {
                var registrations = resolver.SafeGetRegistrations();
                var candidates = registrations.Where(x => TypeResolver.IsRegistrationFor(resolver, x, identifier)).ToList();
                if (type.IsConstructedGenericType)
                {
                    var genericIdentifier = new RegistrationId(type.GetGenericTypeDefinition()) { Name = identifier.Name };
                    candidates.AddRange(registrations.Where(x => TypeResolver.IsRegistrationFor(resolver, x, genericIdentifier)));
                }

                foreach (var registration in candidates)
                {
                    var failure = new ResolveFailure($"Registration {Describe(registration)} cannot be resolved");
                    if (registration.Explainer != null)
                        failure.Causes.AddRange(registration.Explainer(type, options, stack));
                    else
                        failure.Causes.Add(new ResolveFailure("Its resolution predicate returned false"));

                    causes.Add(failure);
                }

                if (candidates.Count > 0)
                    return causes;

                if (resolver.Fallback != null)
                    causes.Add(new ResolveFailure("The resolution fallback predicate returned false"));

                if (!options.AutoResolve)
                {
                    causes.Insert(0, new ResolveFailure($"No registration found for {Describe(identifier)} (unregistered concrete types are resolved only with ResolveOptions.AutoResolve or AutoWiring)"));
                    return causes;
                }

                causes.Insert(0, new ResolveFailure($"No registration found for {Describe(identifier)}"));
            }

            causes.AddRange(ExplainAutoCreate(resolver, type, options, stack));
            return causes;
        }
        finally
        {
            stack.RemoveAt(stack.Count - 1);
        }
    }

    public static IReadOnlyList<ResolveFailure> ExplainAutoCreate(ITypeResolver resolver, Type type, ResolveOptions options, List<Type> stack)
    {
        var name = GetTypeName(type);
        if (type.IsInterface)
            return new[] { new ResolveFailure($"'{name}' is an interface, it must be registered to be resolved") };

        if (type.IsAbstract)
            return new[] { new ResolveFailure($"'{name}' is an abstract class, it must be registered to be resolved") };

        if (type.IsGenericTypeDefinition)
            return new[] { new ResolveFailure($"'{name}' is an open generic type, it cannot be created without generic arguments") };

        if (type.IsValueType || type == typeof(string))
            return new[] { new ResolveFailure($"'{name}' is a value type or a string, it cannot be created automatically and must be registered or given as a parameter") };

        if (!type.IsClass)
            return new[] { new ResolveFailure($"'{name}' cannot be created automatically") };

        var constructors = type.GetConstructors();
        if (constructors.Length == 0)
            return new[] { new ResolveFailure($"'{name}' has no public constructor") };

        var causes = new List<ResolveFailure>();
        foreach (var constructor in constructors)
        {
            causes.Add(new ResolveFailure($"Constructor {Describe(constructor)} cannot be called", ExplainParameters(resolver, constructor, options, stack)));
        }

        return causes;
    }

    private static IReadOnlyList<ResolveFailure> ExplainParameters(ITypeResolver resolver, MethodBase method, ResolveOptions options, List<Type> stack)
    {
        var failures = new List<ResolveFailure>();
        var parameterOptions = TypeResolver.CreateParameterOptions(options);
        foreach (var parameter in method.GetParameters())
        {
            var parameterType = parameter.ParameterType;
            if (options.NamedParameters.TryGetValue(parameter.Name!, out var value) || options.TypedParameters.TryGetValue(parameterType, out value))
            {
                if (!(value == null ? !parameterType.IsValueType : parameterType.IsInstanceOfType(value)))
                    failures.Add(new ResolveFailure($"Parameter '{parameter.Name}' ({GetTypeName(parameterType)}) was given a value of an incompatible type ({(value == null ? "null" : GetTypeName(value.GetType()))})"));

                continue;
            }

            if (options.AdditionalParameters.Any(x => parameterType.IsInstanceOfType(x)))
                continue;

            if (options.IsAsync)
                parameterType = AsyncHelpers.GetTaskType(parameterType);

            var identifier = new RegistrationId(parameterType);
            if (resolver.TryResolveActivator(identifier, parameterOptions, out _) || parameter.DefaultValue != DBNull.Value)
                continue;

            failures.Add(new ResolveFailure($"Parameter '{parameter.Name}' ({GetTypeName(parameter.ParameterType)}) cannot be resolved", ExplainIdentifier(resolver, identifier, parameterOptions, stack)));
        }

        return failures;
    }

    private static void Append(StringBuilder bld, IEnumerable<ResolveFailure> failures, int depth)
    {
        foreach (var failure in failures)
        {
            bld.AppendLine();
            bld.Append(' ', depth * 2).Append("- ").Append(failure.Message);
            Append(bld, failure.Causes, depth + 1);
        }
    }
}
