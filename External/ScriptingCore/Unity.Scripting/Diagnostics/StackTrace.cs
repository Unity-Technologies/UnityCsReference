using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Unity.Scripting;

internal static class StackTrace
{
    const string k_HideInCallstackAttributeTypeName = "UnityEngine.HideInCallstackAttribute";
    static string s_BasePath = string.Empty;

    internal static string BasePath
    {
        get => s_BasePath;
        set
        {
            s_BasePath = value;

            if (!string.IsNullOrEmpty(s_BasePath))
                s_BasePath = s_BasePath.Replace("\\", "/");
        }
    }

    [System.Security.SecuritySafeCritical] // System.Diagnostics.StackTrace cannot be accessed from transparent code (PSM, 2.12)
    internal static void GetMessageAndStackTrace(Exception? exception, out string message, out string stackTrace)
    {
        if (exception == null)
            throw new ArgumentException($"{nameof(GetMessageAndStackTrace)} called with null exception");

        // StackTrace might not be available
        StringBuilder sb = new(exception.StackTrace == null ? 512 : exception.StackTrace.Length * 2);
        message = "";
        var traceString = "";
        while (exception != null)
        {
            if (traceString.Length == 0)
                traceString = exception.StackTrace ?? "";
            else
                traceString = (exception.StackTrace ?? "") + "\n" + traceString;

            var thisMessage = exception.GetType().Name;
            var exceptionMessage = "";
            if (exception.Message != null) exceptionMessage = exception.Message;
            if (exceptionMessage.Trim().Length != 0)
            {
                thisMessage += ": ";
                thisMessage += exceptionMessage;
            }
            message = thisMessage;
            if (exception.InnerException != null)
            {
                traceString = "Rethrow as " + thisMessage + "\n" + traceString;
            }
            exception = exception.InnerException;
        }

        sb.Append(traceString + "\n");

        var trace = new System.Diagnostics.StackTrace(1, true);
        sb.Append(Format(trace));

        stackTrace = sb.ToString();
    }

    // NB if you change this formatting/code there is a separate Mono quick path in MonoManager.cpp that must be updated as well.
    [System.Security.SecuritySafeCritical] // System.Diagnostics.StackTrace cannot be accessed from transparent code (PSM, 2.12)
    internal static string Format(System.Diagnostics.StackTrace stackTrace)
    {
        var basePath = BasePath;
        var sb = new StringBuilder(255);
        int iIndex;

        // need to skip over "n" frames which represent the
        // System.Diagnostics package frames
        for (iIndex = 0; iIndex < stackTrace.FrameCount; iIndex++)
        {
            StackFrame frame = stackTrace.GetFrame(iIndex);

            MethodBase mb = frame.GetMethod();
            if (mb == null)
                continue;

            Type classType = mb.DeclaringType;
            if (classType == null)
                continue;

            // Add namespace.classname:MethodName
            var ns = classType.Namespace;
            if (!string.IsNullOrEmpty(ns))
            {
                sb.Append(ns);
                sb.Append('.');
            }

            sb.Append(classType.Name);
            sb.Append(':');
            sb.Append(mb.Name);
            sb.Append('(');

            // Add parameters
            int j = 0;
            ParameterInfo[] pi = mb.GetParameters();
            bool fFirstParam = true;
            while (j < pi.Length)
            {
                if (fFirstParam == false)
                    sb.Append(", ");
                else
                    fFirstParam = false;

                sb.Append(pi[j].ParameterType.Name);
                j++;
            }
            sb.Append(')');

            // Add path name and line number - unless it is a Debug.Log call, then we are only interested
            // in the calling frame.
            string path = frame.GetFileName();
            if (path != null)
            {
                // Stripping does not exclude line entries entirely but only the
                // part that allows us to generate hyperlinks and code pointers.
                if (!ShouldStripLineNumbers(mb))
                {
                    sb.Append(" (at ");

                    if (!string.IsNullOrEmpty(basePath))
                    {
                        if ((path.Contains('\\') ? path.Replace('\\', '/') : path).StartsWith(basePath))
                        {
                            path = path[basePath.Length..];
                        }
                    }

                    sb.Append(path);
                    sb.Append(':');
                    sb.Append(frame.GetFileLineNumber());
                    sb.Append(')');
                }
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }

    static bool ShouldStripLineNumbers(MethodBase method)
    {
        return HasHideInCallstackAttribute(method) ||
            (method.DeclaringType.Name == "Debug" && method.DeclaringType.Namespace == "UnityEngine") ||
            (method.DeclaringType.Name == "Logger" && method.DeclaringType.Namespace == "UnityEngine") ||
            (method.DeclaringType.Name == "DebugLogHandler" && method.DeclaringType.Namespace == "UnityEngine") ||
            (method.DeclaringType.Name == "Assert" && method.DeclaringType.Namespace == "UnityEngine.Assertions") ||
            (method.Name == "print" && method.DeclaringType.Name == "MonoBehaviour" && method.DeclaringType.Namespace == "UnityEngine");
    }

    static bool HasHideInCallstackAttribute(MethodBase method)
    {
        // GetCustomAttributesData reads raw metadata without instantiating attribute objects,
        // avoiding allocations and any risk of attribute constructor exceptions aborting
        // stack trace formatting.
        if (method is not MethodInfo methodInfo)
        {
            // Constructors are not MethodInfo and cannot be overridden; no chain to walk.
            foreach (var attr in method.GetCustomAttributesData())
            {
                if (attr.AttributeType.FullName == k_HideInCallstackAttributeTypeName)
                    return true;
            }

            return false;
        }

        // Walk the base method chain to replicate Inherited=true behavior, since
        // GetCustomAttributesData only reads attributes declared directly on the method.
        for (MethodInfo? current = methodInfo; current != null;)
        {
            foreach (var attribute in current.GetCustomAttributesData())
            {
                if (attribute.AttributeType.FullName == k_HideInCallstackAttributeTypeName)
                    return true;
            }

            var baseDefinition = current.GetBaseDefinition();
            current = baseDefinition.Equals(current) ? null : baseDefinition;
        }
        return false;
    }
}
