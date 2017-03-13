// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Reflection;
using UnityEditorInternal;

namespace UnityEditor
{
    // Class used by Tools/build_resources to assemble the skins from all images and source skins.
    // Main entry point is DoIt, but there's also some UI that's part of developer builds. That UI is mainly for tweaking, etc.
    // If you find values using the UI, you need to enter them into the constants below (as build_resources just uses those).
    class AssembleEditorSkin : EditorWindow
    {
        // Called from c++
        public static void DoIt()
        {
            // This menu item is created by a script in the editor_resources project
            EditorApplication.ExecuteMenuItem("Tools/Regenerate Editor Skins Now");
        }

        // Called from c++
        static void RegenerateAllIconsWithMipLevels()
        {
            GenerateIconsWithMipLevels.GenerateAllIconsWithMipLevels();
        }

        // Called from c++
        static void RegenerateSelectedIconsWithMipLevels()
        {
            GenerateIconsWithMipLevels.GenerateSelectedIconsWithMips();
        }
    }
}
