using System.Reflection;
using System.Text;

namespace Unity.Scripting
{

    /**
     * Mono had a concept for a shortened "Reflection Formatted" string for a method
     */
    internal class ReflectionFormatting
    {
        internal static string FormatMethod(MethodBase method)
        {
            // MethodBase.ToString() is "<ReturnType> <Name>(<params>)". Drop the leading return type so
            // the method portion matches Mono's mono_method_full_name, which omits it.
            //
            // Splitting on the first space is safe: a type rendered by reflection never contains a space
            // (generic arguments are comma-separated with no space, e.g. "Dictionary`2[System.Int32,System.String]",
            // arrays are "T[]"), so the first space always delimits the return type from the name. The
            // ", " between parameters comes later and is preserved. We can't instead strip
            // ReturnType.ToString() because that disagrees with the rendering here for some types (e.g.
            // "System.Void" vs the "Void" produced above).
            var signature = method.ToString();
            int nameStart = signature.IndexOf(' ');
            if (nameStart >= 0)
            {
                signature = signature.Substring(nameStart + 1);
            }
            return $"{method.ReflectedType}.{signature}";
        }
    }
}
