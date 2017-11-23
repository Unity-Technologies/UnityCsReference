// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    // Keep in sync with OptionalUnityReferences in C++
    [Flags]
    internal enum OptionalUnityReferences
    {
        None = 0,
        TestAssemblies = 1 << 1,
    }
}
