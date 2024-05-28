using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DsLauncherService.Extensions
{
    internal static class MemberInfoExtensions
    {
        public static bool TryGetCustomAttribute<T>(this MemberInfo member, [MaybeNullWhen(false)] out T attribute)
            where T : Attribute
        {
            return (attribute = member.GetCustomAttribute<T>()!) is not null;
        }
    }
}
