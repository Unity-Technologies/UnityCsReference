// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    [Flags]
    internal enum DirtySource
    {
        None = 0,
        DirtyAssembly = (1 << 0),
        DirtyReference = (1 << 1),
    }
}
