using System;

namespace Unity.UI.Builder
{
    internal static class TypeExtensions
    {
        public static string GetFullNameWithAssembly(this Type type) => $"{type.FullName}, {type.Assembly.GetName().Name}";
    }
}
