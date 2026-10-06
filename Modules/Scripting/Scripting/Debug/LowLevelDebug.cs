// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.Scripting.LowLevel;

// Forwards to the ScriptingCore Debug bridge (CoreModule injects the implementation at
// startup) so these messages go through the full UnityEngine.Debug pipeline with stack traces.
[VisibleToOtherModules]
internal static partial class Debug
{
    [VisibleToOtherModules]
    internal static void LogWarning(string message) => Unity.Scripting.Debug.LogWarning(message);

    [VisibleToOtherModules]
    internal static void LogError(string message) => Unity.Scripting.Debug.LogError(message);
}
