// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal enum DirtySource
    {
        None = 0,
        DirtyScript = 1,
        DirtyAssembly = 2,
        DirtyReference = 3,
    }
}
