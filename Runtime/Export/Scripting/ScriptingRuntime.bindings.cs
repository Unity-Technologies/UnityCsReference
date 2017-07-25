// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/Scripting/ScriptingRuntime.h")]
    internal class ScriptingRuntime
    {
        extern public static string[] GetAllUserAssemblies();
    }
}
