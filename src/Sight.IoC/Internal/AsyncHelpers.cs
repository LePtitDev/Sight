using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Sight.IoC.Internal
{
    internal static class AsyncHelpers
    {
        public static bool IsTaskType(Type type)
        {
            return IsTaskType(type, out _);
        }

        public static bool IsTaskType(Type type, [NotNullWhen(true)] out Type? resultType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                resultType = type.GetGenericArguments()[0];
                return true;
            }

            if (type.BaseType is { IsConstructedGenericType: true } parentType && parentType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                resultType = parentType.GetGenericArguments()[0];
                return true;
            }

            resultType = null;
            return false;
        }

        public static bool IsTaskOfType(object instance, Type resultType)
        {
            return IsTaskType(instance.GetType(), out var type) && resultType.IsAssignableFrom(type);
        }

        public static Type GetTaskType(Type resultType)
        {
            return typeof(Task<>).MakeGenericType(resultType);
        }

        public static object? GetTaskResult(object task)
        {
#if DEBUG
            if (task is not Task)
                throw new ArgumentException($"Cannot get task result from '{task.GetType()}'");
#endif
            return task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task);
        }

        public static Task GetTaskFromResult(Type resultType, object? result)
        {
            var method = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType);
            return (Task)method.Invoke(null, new[] { result })!;
        }
    }
}
