// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    static class UITKTextSettingsCreationMenu
    {
        [MenuItem("Assets/Create/UI Toolkit/Text Settings", false)]
        public static void CreateUITKTextSettingsAsset()
        {
            // Create new TextSettings asset
            PanelTextSettings textSettings = ScriptableObject.CreateInstance<PanelTextSettings>();
            ProjectWindowUtil.CreateAsset(textSettings, "UITK Text Settings.asset");
        }
    }
}
