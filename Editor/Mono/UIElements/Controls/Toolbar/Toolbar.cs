// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A toolbar for tool windows. For more information, refer to [[wiki:UIE-uxml-element-Toolbar|UXML element Toolbar]].
    /// </summary>
    [UxmlElement]
    [Icon("UIToolkit/Icons/Toolbar.png")]
    public partial class Toolbar : VisualElement
    {
        private static readonly string s_ToolbarDarkStyleSheetPath = "StyleSheets/Generated/ToolbarDark.uss.asset";
        private static readonly string s_ToolbarLightStyleSheetPath = "StyleSheets/Generated/ToolbarLight.uss.asset";

        [AutoStaticsCleanupOnCodeReload]
        private static StyleSheet s_ToolbarDarkStyleSheet;
        [AutoStaticsCleanupOnCodeReload]
        private static StyleSheet s_ToolbarLightStyleSheet;

        static StyleSheet GetOrLoadToolbarStyleSheet(ref StyleSheet cached, string path)
        {
            if (cached == null && !Application.isBuildingEditorResources)
            {
                cached = EditorGUIUtility.Load(UIElementsEditorUtility.GetStyleSheetPathForCurrentFont(path)) as StyleSheet;
                if (cached != null)
                    cached.isDefaultStyleSheet = true;
            }
            return cached;
        }

        internal static void SetToolbarStyleSheet(VisualElement ve)
        {
            if (EditorGUIUtility.isProSkin)
            {
                ve.styleSheets.Add(GetOrLoadToolbarStyleSheet(ref s_ToolbarDarkStyleSheet, s_ToolbarDarkStyleSheetPath));
            }
            else
            {
                ve.styleSheets.Add(GetOrLoadToolbarStyleSheet(ref s_ToolbarLightStyleSheet, s_ToolbarLightStyleSheetPath));
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-toolbar";

        /// <summary>
        /// Constructor.
        /// </summary>
        public Toolbar()
        {
            AddToClassList(ussClassName);
            SetToolbarStyleSheet(this);
        }
    }
}
