using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Sight.Linq.Internal;

namespace Sight.Linq;

/// <summary>
/// Extension methods for reflection classes
/// </summary>
public static class ReflectionExt
{
    /// <summary>
    /// Get attribute of member. If several attributes of this type exist, the first one is returned (unlike <c>GetCustomAttribute</c> that throws an <see cref="AmbiguousMatchException"/>)
    /// </summary>
    public static T? GetAttribute<T>(this ICustomAttributeProvider member, bool inherit = true) where T : Attribute
    {
        return GetAttributes<T>(member, inherit).FirstOrDefault();
    }

    /// <summary>
    /// Get attributes of member
    /// </summary>
    public static T[] GetAttributes<T>(this ICustomAttributeProvider member, bool inherit = true) where T : Attribute
    {
        Guard.NotNull(member, nameof(member));

        // Attribute.GetCustomAttributes honors "inherit" for properties and events, unlike MemberInfo.GetCustomAttributes
        var attributes = member switch
        {
            MemberInfo m => Attribute.GetCustomAttributes(m, typeof(T), inherit),
            ParameterInfo p => Attribute.GetCustomAttributes(p, typeof(T), inherit),
            Module m => Attribute.GetCustomAttributes(m, typeof(T), inherit),
            Assembly a => Attribute.GetCustomAttributes(a, typeof(T), inherit),
            _ => (object[])member.GetCustomAttributes(typeof(T), inherit),
        };

        return attributes.OfType<T>().ToArray();
    }

    /// <summary>
    /// Indicates if member has an attribute
    /// </summary>
    public static bool HasAttribute(this ICustomAttributeProvider member, Type attributeType, bool inherit = true)
    {
        Guard.NotNull(member, nameof(member));
        Guard.NotNull(attributeType, nameof(attributeType));

        return member switch
        {
            MemberInfo m => Attribute.IsDefined(m, attributeType, inherit),
            ParameterInfo p => Attribute.IsDefined(p, attributeType, inherit),
            Module m => Attribute.IsDefined(m, attributeType, inherit),
            Assembly a => Attribute.IsDefined(a, attributeType, inherit),
            _ => member.IsDefined(attributeType, inherit),
        };
    }

    /// <summary>
    /// Indicates if member has an attribute
    /// </summary>
    public static bool HasAttribute<T>(this ICustomAttributeProvider member, bool inherit = true) where T : Attribute
    {
        return HasAttribute(member, typeof(T), inherit);
    }

    /// <summary>
    /// Get types of assembly that implement base type.
    /// Types that cannot be loaded (missing dependency) are ignored
    /// </summary>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("Enumerating the types of an assembly is not trim-safe: types that are not referenced elsewhere may be removed")]
#endif
    public static IEnumerable<Type> GetTypesOf(this Assembly assembly, Type baseType, bool withAbstract = true, bool withGeneric = true)
    {
        Guard.NotNull(assembly, nameof(assembly));
        Guard.NotNull(baseType, nameof(baseType));

        var types = GetLoadableTypes(assembly).Where(x => x != baseType && IsAssignable(baseType, x));
        if (!withAbstract)
            types = types.Where(x => !x.IsAbstract);

        if (!withGeneric)
            types = types.Where(x => !x.IsGenericType);

        return types;
    }

    /// <inheritdoc cref="GetTypesOf"/>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("Enumerating the types of an assembly is not trim-safe: types that are not referenced elsewhere may be removed")]
#endif
    public static IEnumerable<Type> GetTypesOf<TBase>(this Assembly assembly, bool withAbstract = true, bool withGeneric = true)
    {
        return GetTypesOf(assembly, typeof(TBase), withAbstract, withGeneric);
    }

#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("Enumerating the types of an assembly is not trim-safe")]
#endif
    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(x => x != null).Select(x => x!);
        }
    }

#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("Enumerating the interfaces of a type is not trim-safe")]
#endif
    private static bool IsAssignable(Type baseType, Type type)
    {
        if (!baseType.IsGenericTypeDefinition)
            return baseType.IsAssignableFrom(type);

        // Open generic base type (like typeof(IRepository<>)): look for a closed form in the hierarchy
        if (baseType.IsInterface)
            return type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == baseType);

        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == baseType)
                return true;
        }

        return false;
    }
}
