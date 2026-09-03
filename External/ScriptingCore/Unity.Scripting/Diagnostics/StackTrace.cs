using System.Diagnostics;
using System.Reflection;
using System.Text;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Scripting;

internal static class StackTrace
{
    const string k_HideInCallstackAttributeTypeName = "UnityEngine.HideInCallstackAttribute";
    const string k_InteropNamespace = "Unity.Private.Scripting.Interop";
    const string k_InvokeWrapperPrefix = "runtime_invoke_wrapper";

    // Host-installed project base path, set once at initialization; not tied to any scope.
    [NoAutoStaticsCleanup]
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

    // This code is shared between Mono and CoreCLR, so we cannot conditionally compile the frame format.
    // We use by default the Mono format as long as it's supported. This value is overridden on CoreCLR
    // initialization. To be removed once Mono support is fully removed.
    // Host-installed backend trace format, set once at initialization; not tied to any scope.
    [NoAutoStaticsCleanup]
    internal static bool UseMonoFormat { get; set; } = true;

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
            // Strip the interop/marshalling shim frames.
            var exceptionTrace = NormalizeManagedExceptionTrace(exception.StackTrace);
            if (traceString.Length == 0)
                traceString = exceptionTrace;
            else
                traceString = exceptionTrace + "\n" + traceString;

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

    static string NormalizeManagedExceptionTrace(string? rawTrace)
    {
        if (string.IsNullOrEmpty(rawTrace))
            return "";

        var sb = new StringBuilder(rawTrace.Length);
        var traceSpan = rawTrace.AsSpan();
        while (ExtractNextLine(traceSpan, out var line, out traceSpan))
        {
            if (line.Contains(k_InteropNamespace, StringComparison.Ordinal) ||
                line.Contains(k_InvokeWrapperPrefix, StringComparison.Ordinal))
                continue;

            if (sb.Length > 0)
                sb.Append('\n');
            sb.Append(line);
        }

        return sb.ToString();
    }

    static bool ExtractNextLine(ReadOnlySpan<char> trace, out ReadOnlySpan<char> line, out ReadOnlySpan<char> remaining)
    {
        if (trace.IsEmpty)
        {
            line = ReadOnlySpan<char>.Empty;
            remaining = ReadOnlySpan<char>.Empty;
            return false;
        }

        var end = trace.IndexOf('\n');
        if (end < 0)
        {
            line = trace;
            remaining = ReadOnlySpan<char>.Empty;
        }
        else
        {
            line = trace[..end];
            remaining = trace[(end + 1)..];
        }

        // CoreCLR joins frames with Environment.NewLine ("\r\n" on Windows); drop the '\r'
        // so the rebuilt trace is normalized to '\n'.
        if (line.Length > 0 && line[^1] == '\r')
            line = line[..^1];

        return true;
    }

    // Renders a managed stack trace. The frame shape depends on UseMonoFormat:
    // Mono keeps the historical "Type:Method(...) (at path:N)"; CoreCLR emits the native .NET
    // "Type.Method(...) in path:line N". Mono's log path normally uses the native quick path in
    // MonoManager.cpp, but this method still runs on Mono via the exception path, so it must stay backend-aware.
    [System.Security.SecuritySafeCritical] // System.Diagnostics.StackTrace cannot be accessed from transparent code (PSM, 2.12)
    internal static string Format(System.Diagnostics.StackTrace stackTrace)
    {
        const char k_TypeMethodSeparator = '.';
        const string k_LocationPrefix = " in ";
        const string k_LineNumberSeparator = ":line ";
        const string k_LocationSuffix = "";

        const char k_TypeMethodSeparatorMono = ':';
        const string k_LocationPrefixMono = " (at ";
        const string k_LineNumberSeparatorMono = ":";
        const string k_LocationSuffixMono = ")";

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

            if (classType.Namespace == k_InteropNamespace)
                continue;

            // Add namespace.classname:MethodName
            var ns = classType.Namespace;
            if (!string.IsNullOrEmpty(ns))
            {
                sb.Append(ns);
                sb.Append('.');
            }

            sb.Append(classType.Name);
            sb.Append(UseMonoFormat ? k_TypeMethodSeparatorMono : k_TypeMethodSeparator);
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
                    sb.Append(UseMonoFormat ? k_LocationPrefixMono : k_LocationPrefix);

                    if (!string.IsNullOrEmpty(basePath))
                    {
                        if ((path.Contains('\\') ? path.Replace('\\', '/') : path).StartsWith(basePath))
                        {
                            path = path[basePath.Length..];
                        }
                    }

                    sb.Append(path);
                    sb.Append(UseMonoFormat ? k_LineNumberSeparatorMono : k_LineNumberSeparator);
                    sb.Append(frame.GetFileLineNumber());
                    sb.Append(UseMonoFormat ? k_LocationSuffixMono : k_LocationSuffix);
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
