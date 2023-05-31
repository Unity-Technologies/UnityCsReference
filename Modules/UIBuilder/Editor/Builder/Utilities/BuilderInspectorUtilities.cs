// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class BuilderInspectorUtilities
    {
        public static bool HasOverriddenField(VisualElement ve)
        {
            return ve.Q(className: BuilderConstants.InspectorLocalStyleOverrideClassName) != null;
        }

        // Useful for loading the Builder inspector's icons
        public static Texture2D LoadIcon(string iconName, string subfolder = "")
        {
            return EditorGUIUtility.Load(EditorGUIUtility.isProSkin
                ? $"{BuilderConstants.IconsResourcesPath}/Dark/Inspector/{subfolder}{iconName}.png"
                : $"{BuilderConstants.IconsResourcesPath}/Light/Inspector/{subfolder}{iconName}.png") as Texture2D;
        }
    }
}
