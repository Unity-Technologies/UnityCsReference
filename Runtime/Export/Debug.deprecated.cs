// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.ComponentModel;

namespace UnityEngine
{
    partial class Debug
    {
        [Obsolete("Assert(bool, string, params object[]) is obsolete. Use AssertFormat(bool, string, params object[]) (UnityUpgradable) -> AssertFormat(*)", true)]
        [Conditional(Assertions.Assert.UNITY_ASSERTIONS)]
        public static void Assert(bool condition, string format, params object[] args) { if (!condition) unityLogger.LogFormat(LogType.Assert, format, args); }

        // Renamed to avoid autocomplete conflict w/ Debug.Log
        [Obsolete("Debug.logger is obsolete. Please use Debug.unityLogger instead (UnityUpgradable) -> unityLogger")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ILogger logger
        {
            get { return s_Logger; }
        }
    }
}
