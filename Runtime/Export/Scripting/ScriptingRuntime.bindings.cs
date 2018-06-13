// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/Scripting/ScriptingRuntime.h")]
    [VisibleToOtherModules]
    internal partial class ScriptingRuntime
    {
        public static extern string[] GetAllUserAssemblies();
    }
}
