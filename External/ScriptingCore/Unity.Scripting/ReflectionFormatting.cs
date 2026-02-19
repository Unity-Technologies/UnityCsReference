using System.Reflection;
using System.Text;

namespace Unity.Scripting
{

    /**
     * Mono had a concept for a shortened "Reflection Formatted" string for a method
     */
    internal class ReflectionFormatting
    {
        internal static string FormatMethod(MethodBase method) => $"{method.ReflectedType}.{method}";
    }
}
