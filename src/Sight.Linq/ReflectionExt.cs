using System.Linq;
using System.Reflection;

namespace Sight.Linq
{
    /// <summary>
    /// Extension methods for reflection classes
    /// </summary>
    public static class ReflectionExt
    {
        /// <summary>
        /// Get attribute of member
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
            return (T[])member.GetCustomAttributes(typeof(T), inherit);
        }

        /// <summary>
        /// Indicates if member has an attribute
        /// </summary>
        public static bool HasAttribute(this ICustomAttributeProvider member, Type attributeType, bool inherit = true)
        {
            return member.GetCustomAttributes(attributeType, inherit).Any();
        }

        /// <summary>
        /// Indicates if member has an attribute
        /// </summary>
        public static bool HasAttribute<T>(this ICustomAttributeProvider member, bool inherit = true) where T : Attribute
        {
            return GetAttributes<T>(member, inherit).Any();
        }

        /// <summary>
        /// Get types of assembly that implement base type
        /// </summary>
        public static IEnumerable<Type> GetTypesOf(this Assembly assembly, Type baseType, bool withAbstract = true, bool withGeneric = true)
        {
            var types = assembly.GetTypes().Where(baseType.IsAssignableFrom).Except(baseType);
            if (!withAbstract)
                types = types.Where(x => !x.IsAbstract);

            if (!withGeneric)
                types = types.Where(x => !x.IsGenericType);

            return types;
        }

        /// <inheritdoc cref="GetTypesOf"/>
        public static IEnumerable<Type> GetTypesOf<TBase>(this Assembly assembly, bool withAbstract = true, bool withGeneric = true)
        {
            return GetTypesOf(assembly, typeof(TBase), withAbstract, withGeneric);
        }
    }
}
