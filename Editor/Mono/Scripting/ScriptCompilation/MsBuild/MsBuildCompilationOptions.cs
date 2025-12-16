// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation.MsBuild
{
    [Flags]
    enum MSBuildCompilationOptions
    {
        BuildingEmpty = 0,
        BuildingForIl2Cpp = 1 << 0,
        BuildingWithAsserts = 1 << 1,
        BuildingWithInstrumentation = 1 << 2,
        BuildingWithDebug = 1 << 3,
    }
}
