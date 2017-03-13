// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    [Flags]
    enum BuildFlags
    {
        None = 0,
        BuildingDevelopmentBuild = (1 << 0),
        BuildingForEditor = (1 << 1)
    };
}
