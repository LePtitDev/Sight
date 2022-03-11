using System.Linq;
using System.Reflection;

namespace Sight.Linq
{
    /// <summary>
    /// Extension methods for <see cref="MemberInfo"/>
    /// </summary>
    public static class ReflectionExt
    {
        /// <summary>
        /// Get attribute of member
        /// </summary>
        public static T? GetAttribute<T>(this MemberInfo member, bool inherit = true) where T : Attribute
        {
            return GetAttributes<T>(member, inherit).FirstOrDefault();
        }

        /// <summary>
        /// Get attributes of member
        /// </summary>
        public static T[] GetAttributes<T>(this MemberInfo member, bool inherit = true) where T : Attribute
        {
            return (T[])member.GetCustomAttributes(typeof(T), inherit);
        }

        /// <summary>
        /// Indicates if member has an attribute
        /// </summary>
        public static bool HasAttribute(this MemberInfo member, Type attributeType, bool inherit = true)
        {
            return member.GetCustomAttributes(attributeType, inherit).Any();
        }

        /// <summary>
        /// Indicates if member has an attribute
        /// </summary>
        public static bool HasAttribute<T>(this MemberInfo member, bool inherit = true) where T : Attribute
        {
            return GetAttributes<T>(member, inherit).Any();
        }
    }
}
