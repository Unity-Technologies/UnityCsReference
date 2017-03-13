// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEditor
{
    public sealed partial class EditorApplication
    {
        internal static UnityAction projectWasLoaded;
        internal static UnityAction editorApplicationQuit;

        private static void Internal_ProjectWasLoaded()
        {
            if (projectWasLoaded != null)
                projectWasLoaded();
        }

        private static void Internal_EditorApplicationQuit()
        {
            if (editorApplicationQuit != null)
                editorApplicationQuit();
        }

        internal static bool supportsHiDPI { get { return Application.platform == RuntimePlatform.OSXEditor; } }
    }
}
