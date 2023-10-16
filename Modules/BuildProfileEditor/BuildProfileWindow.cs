// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Build Settings window in 'File > Build Settings'.
    /// Handles creating and editing of <see cref="BuildProfile"/> assets.
    ///
    /// TODO EPIC: https://jira.unity3d.com/browse/PLAT-5878
    /// </summary>
    [EditorWindowTitle(title = "Build Settings")]
    internal class BuildProfileWindow : EditorWindow
    {
        private const string Uxml = "UXML/BuildProfile/BuildProfileWindow.uxml";

        [UsedImplicitly, RequiredByNativeCode]
        public static void ShowBuildProfileWindow()
        {
            // TODO: Defer to old build settings experience.
            // ticket: https://jira.unity3d.com/browse/PLAT-5886
            var window = BuildPlayerWindow.GetWindow<BuildPlayerWindow>("Build Profiles");
            window.minSize = new Vector2(640, 400);
        }

        public void CreateGUI()
        {
            var windowUxml = EditorGUIUtility.Load(Uxml) as VisualTreeAsset;
            if (windowUxml == null)
            {
                Debug.LogError("Build Player window resources not found.");
                return;
            }

            var rootUxml = windowUxml.CloneTree();
            rootVisualElement.Add(rootUxml);
            rootUxml.StretchToParentSize();
        }
    }
}
