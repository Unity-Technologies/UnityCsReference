// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    static class GraphElementHelper_Internal
    {
        internal static void LoadTemplateAndStylesheet_Internal(VisualElement container, string name, string rootClassName, IEnumerable<string> additionalStylesheets = null)
        {
            if (name != null && container != null)
            {
                var tpl = LoadUxml(name + ".uxml");
                tpl.CloneTree(container);

                if (additionalStylesheets != null)
                {
                    foreach (var additionalStylesheet in additionalStylesheets)
                    {
                        container.AddStylesheet_Internal(additionalStylesheet + ".uss");
                    }
                }

                container.AddStylesheet_Internal(name + ".uss");
            }
        }

        internal static void AddStylesheet_Internal(this VisualElement ve, string stylesheetName)
        {
            if (ve == null || stylesheetName == null)
                return;
            ve.AddStyleSheetPath($"StyleSheets/GraphToolsFoundation/{stylesheetName}");
        }

        /// <summary>
        /// Loads a stylesheet and the appropriate variant for the current skin.
        /// </summary>
        /// <remarks>If the stylesheet name is Common.uss and <see cref="EditorGUIUtility.isProSkin"/> is true,
        /// this method will load "Common.uss" and "Common_dark.uss". If <see cref="EditorGUIUtility.isProSkin"/> is false,
        /// this method will load "Common.uss" and "Common_light.uss".
        /// </remarks>
        /// <param name="ve">The visual element onto which to attach the stylesheets.</param>
        /// <param name="stylesheetName">The name of the common stylesheet.</param>
        internal static void AddStylesheetWithSkinVariants_Internal(this VisualElement ve, string stylesheetName)
        {
            var extension = Path.GetExtension(stylesheetName);
            var baseName = Path.ChangeExtension(stylesheetName, null);
            if (EditorGUIUtility.isProSkin)
            {
                AddStylesheet_Internal(ve, baseName + "_dark" + extension);
            }
            else
            {
                AddStylesheet_Internal(ve, baseName + "_light" + extension);
            }
            AddStylesheet_Internal(ve, stylesheetName);
        }


        static VisualTreeAsset LoadUxml(string uxmlName)
        {
            var tpl = EditorGUIUtility.Load($"UXML/GraphToolsFoundation/{uxmlName}") as VisualTreeAsset;
            if (tpl == null)
            {
                Debug.Log("Failed to load template " + uxmlName);
            }

            return tpl;
        }
    }
}
