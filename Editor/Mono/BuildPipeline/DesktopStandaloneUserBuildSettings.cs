// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Modules;
using System.Collections.Generic;
using System.Text;

internal static class DesktopStandaloneUserBuildSettings
{
    internal static string PlatformName
    {
        get
        {
            return "Standalone";
        }
    }
}
