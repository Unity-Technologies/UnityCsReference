// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.Scripting.ScriptCompilation
{
    // Keep in sync with ManagedAssemblyFlags in C++
    [Flags]
    enum AssemblyFlags
    {
        None = 0,
        EditorOnly = (1 << 0),
        UseForMono = (1 << 1),
        UseForDotNet = (1 << 2),
        FirstPass = (1 << 3),
        ExcludedForRuntimeCode = (1 << 4),
        UserAssembly = (1 << 5),
        ExplicitlyReferenced = (1 << 6),
        ExplicitReferences = (1 << 7),
        UnityModule = (1 << 8)
    };
}
