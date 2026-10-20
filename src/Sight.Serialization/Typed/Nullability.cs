using System;
using System.Linq;
using System.Reflection;

namespace Sight.Serialization.Typed;

/// <summary>
/// Nullable reference type annotations of a member type, as a tree parallel to the type
/// </summary>
internal sealed class NullabilityNode
{
    public static readonly NullabilityNode Unknown = new(null, Array.Empty<NullabilityNode>(), null);

    public NullabilityNode(bool? isNullable, NullabilityNode[] arguments, NullabilityNode? element)
    {
        IsNullable = isNullable;
        Arguments = arguments;
        Element = element;
    }

    /// <summary>
    /// Annotation of a reference type: <c>true</c> nullable, <c>false</c> not null, <c>null</c> oblivious
    /// </summary>
    public bool? IsNullable { get; }

    public NullabilityNode[] Arguments { get; }

    public NullabilityNode? Element { get; }

    public NullabilityNode Argument(int index) => index < Arguments.Length ? Arguments[index] : Unknown;

    public static NullabilityNode Of(MemberInfo member)
    {
#if NET
        var context = new NullabilityInfoContext();
        var info = member switch
        {
            PropertyInfo property => context.Create(property),
            FieldInfo field => context.Create(field),
            _ => null,
        };

        return info == null ? Unknown : Convert(info);
#else
        return LegacyReader.Read(member);
#endif
    }

#if NET
    private static NullabilityNode Convert(NullabilityInfo info)
    {
        bool? state = info.ReadState switch
        {
            NullabilityState.Nullable => true,
            NullabilityState.NotNull => false,
            _ => null,
        };

        return new NullabilityNode(state, info.GenericTypeArguments.Select(Convert).ToArray(), info.ElementType != null ? Convert(info.ElementType) : null);
    }
#else
    /// <summary>
    /// Best-effort reader of the compiler NullableAttribute / NullableContextAttribute metadata
    /// </summary>
    private static class LegacyReader
    {
        public static NullabilityNode Read(MemberInfo member)
        {
            var type = member switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                _ => null,
            };

            if (type == null)
                return Unknown;

            var flags = GetFlags(member) ?? Array.Empty<byte>();
            var context = GetContext(member.DeclaringType);
            var index = 0;
            return Build(type, flags, context, ref index);
        }

        private static NullabilityNode Build(Type type, byte[] flags, byte context, ref int index)
        {
            bool? state = null;
            if (!type.IsValueType)
            {
                var flag = flags.Length == 1 ? flags[0] : index < flags.Length ? flags[index] : context;
                if (flags.Length > 1)
                    index++;

                state = flag switch
                {
                    1 => false,
                    2 => true,
                    _ => null,
                };
            }

            if (type.IsArray)
            {
                var element = Build(type.GetElementType()!, flags, context, ref index);
                return new NullabilityNode(state, Array.Empty<NullabilityNode>(), element);
            }

            if (type.IsGenericType)
            {
                var arguments = type.GetGenericArguments();
                var nodes = new NullabilityNode[arguments.Length];
                for (var i = 0; i < arguments.Length; i++)
                    nodes[i] = Build(arguments[i], flags, context, ref index);

                return new NullabilityNode(state, nodes, null);
            }

            return new NullabilityNode(state, Array.Empty<NullabilityNode>(), null);
        }

        private static byte[]? GetFlags(MemberInfo member)
        {
            foreach (var attribute in member.GetCustomAttributesData())
            {
                if (attribute.AttributeType.FullName != "System.Runtime.CompilerServices.NullableAttribute" || attribute.ConstructorArguments.Count != 1)
                    continue;

                var argument = attribute.ConstructorArguments[0];
                if (argument.Value is byte single)
                    return new[] { single };

                if (argument.Value is System.Collections.Generic.IReadOnlyCollection<CustomAttributeTypedArgument> values)
                    return values.Select(x => (byte)x.Value!).ToArray();
            }

            return null;
        }

        private static byte GetContext(Type? type)
        {
            for (var current = type; current != null; current = current.DeclaringType)
            {
                foreach (var attribute in current.GetCustomAttributesData())
                {
                    if (attribute.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute" && attribute.ConstructorArguments.Count == 1)
                        return (byte)attribute.ConstructorArguments[0].Value!;
                }
            }

            return 0;
        }
    }
#endif
}
