// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine.Bindings;

namespace UnityEngine.DedicatedServer
{
    [NativeHeader("Runtime/Export/DedicatedServer/Arguments.bindings.h")]
    [StaticAccessor("DedicatedServerBindings", StaticAccessorType.DoubleColon)]
    public static class Arguments
    {
        [NativeConditional("PLATFORM_SERVER")]
        [FreeFunction("DedicatedServerBindings::GetBoolArgument")]
        extern internal static bool GetBoolArgument(string arg);
        [NativeConditional("PLATFORM_SERVER")]
        [FreeFunction("DedicatedServerBindings::GetIntArgument")]
        extern internal static bool GetIntArgument(string arg, out int intValue);
        [NativeConditional("PLATFORM_SERVER")]
        [FreeFunction("DedicatedServerBindings::GetStringArgument")]
        extern internal static bool GetStringArgument(string arg, out string stringValue);
        [NativeConditional("PLATFORM_SERVER")]
        [FreeFunction("DedicatedServerBindings::SetBoolArgument")]
        extern internal static void SetBoolArgument(string arg);
        [NativeConditional("PLATFORM_SERVER")]
        [FreeFunction("DedicatedServerBindings::SetIntArgument")]
        extern internal static void SetIntArgument(string arg, int intValue);
        [NativeConditional("PLATFORM_SERVER")]
        [FreeFunction("DedicatedServerBindings::SetStringArgument")]
        extern internal static void SetStringArgument(string arg, string stringValue);

        public static int? Port
        {
            get {
                if (GetIntArgument("port", out var intValue))
                    return intValue;
                return null;
            }
            set {
                int intValue = value ?? default(int);
                SetIntArgument("port", intValue);
            }
        }

        public static int? TargetFramerate
        {
            get {
                if (GetIntArgument("framerate", out var intValue))
                    return intValue;
                return null;
            }
            set {
                int intValue = value ?? default(int);
                SetIntArgument("framerate", intValue);
            }
        }

        public static int? LogLevel
        {
            get {
                if (GetIntArgument("loglevel", out var intValue))
                    return intValue;
                return null;
            }
            set {
                int intValue = value ?? default(int);
                SetIntArgument("loglevel", intValue);
            }
        }

        public static string LogPath
        {
            get {
                if (GetStringArgument("logpath", out var stringValue))
                    return stringValue;
                else if (GetStringArgument("logfile", out stringValue))
                    return Path.GetDirectoryName(stringValue);
                return null;
            }
            set {
                SetStringArgument("logpath", value);
            }
        }

        public static int? QueryPort
        {
            get {
                if (GetIntArgument("queryport", out var intValue))
                    return intValue;
                return null;
            }
            set {
                int intValue = value ?? default(int);
                SetIntArgument("queryport", intValue);
            }
        }

        public static string QueryType
        {
            get {
                if (GetStringArgument("querytype", out var stringValue))
                    return stringValue;
                return null;
            }
            set {
                SetStringArgument("querytype", value);
            }
        }

        public enum ArgumentErrorPolicy {
            Ignore,
            Warn,
            Fatal
        }

        [NativeConditional("PLATFORM_SERVER")]
        [FreeFunction("DedicatedServerBindings::SetArgumentErrorPolicy")]
        extern internal static void SetArgumentErrorPolicy(ArgumentErrorPolicy policy);
        [NativeConditional("PLATFORM_SERVER")]
        [FreeFunction("DedicatedServerBindings::GetArgumentErrorPolicy")]
        extern internal static ArgumentErrorPolicy GetArgumentErrorPolicy();

        public static ArgumentErrorPolicy ErrorPolicy {
            get {
                return GetArgumentErrorPolicy();
            }
            set {
                SetArgumentErrorPolicy(value);
            }
        }
    }
}
