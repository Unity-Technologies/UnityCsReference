// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal static class GraphElementHelper
    {
        // ** USS modifier names ** //

        /// <summary>
        /// The USS modifier added when the element is not connected.
        /// </summary>
        public static readonly string notConnectedUssModifier = "not-connected";

        /// <summary>
        /// The USS modifier added when the element is connected.
        /// </summary>
        public static readonly string connectedUssModifier = "connected";

        /// <summary>
        /// The USS modifier added when the element is empty.
        /// </summary>
        public static readonly string emptyUssModifier = "empty";

        /// <summary>
        /// The USS modifier added when the element is disabled.
        /// </summary>
        public static readonly string disabledUssModifier = "disabled";

        /// <summary>
        /// The USS modifier added when the element is not in use.
        /// </summary>
        public static readonly string unusedUssModifier = "unused";

        /// <summary>
        /// The USS modifier added when the element is read-only.
        /// </summary>
        public static readonly string readOnlyUssModifier = "read-only";

        /// <summary>
        /// The USS modifier added when the element is write-only.
        /// </summary>
        public static readonly string writeOnlyUssModifier = "write-only";

        /// <summary>
        /// The USS modifier added when the element is a placeholder.
        /// </summary>
        public static readonly string placeholderUssModifier = "placeholder";

        /// <summary>
        /// The USS modifier added when the element is highlighted.
        /// </summary>
        public static readonly string highlightedUssModifier = "highlighted";

        /// <summary>
        /// The USS modifier added when the element is collapsed.
        /// </summary>
        public static readonly string collapsedUssModifier = "collapsed";

        /// <summary>
        /// The USS modifier added when the element is expanded.
        /// </summary>
        public static readonly string expandedUssModifier = "expanded";

        /// <summary>
        /// The USS modifier added when the element is hidden.
        /// </summary>
        public static readonly string hiddenUssModifier = "hidden";

        /// <summary>
        /// The USS modifier added when the element is visible.
        /// </summary>
        public static readonly string visibleUssModifier = "visible";

        /// <summary>
        /// The USS modifier added when the element is part of a vertical flow.
        /// </summary>
        public static readonly string verticalUssModifier = "vertical";

        /// <summary>
        /// The USS modifier added when the element's text should be formatted to allow multiline.
        /// </summary>
        public static readonly string multilineUssModifier = "multiline";

        /// <summary>
        /// The USS modifier added when the element can be selected.
        /// </summary>
        public static readonly string selectableUssModifier = "selectable";

        /// <summary>
        /// The USS modifier added when the element is selected.
        /// </summary>
        public static readonly string selectedUssModifier = "selected";

        /// <summary>
        /// The USS modifier added to an element when the graph view is zoomed out to the very small level.
        /// </summary>
        public static readonly string verySmallUssModifier = "very-small";

        /// <summary>
        /// The USS modifier added to an element when the graph view is zoomed out to the small level.
        /// </summary>
        public static readonly string smallUssModifier = "small";

        /// <summary>
        /// The USS modifier added to an element when the graph view is at 100% or zoomed in;
        /// </summary>
        public static readonly string mediumUssModifier = "medium";

        /// <summary>
        /// The USS modifier prefix added to specify the data type of the element.
        /// </summary>
        public static readonly string dataTypeClassUssModifierPrefix = "data-type-";

        // ** USS element names ** //

        /// <summary>
        /// The name for an icon element.
        /// </summary>
        public static readonly string iconName = "icon";

        /// <summary>
        /// The name for an input element.
        /// </summary>
        public static readonly string inputName = "input";

        /// <summary>
        /// The name for a text area element.
        /// </summary>
        public static readonly string textAreaName = "text-area";

        /// <summary>
        /// The name for a title container element.
        /// </summary>
        public static readonly string titleContainerName = "title-container";

        /// <summary>
        /// The name for a content container element.
        /// </summary>
        public static readonly string contentContainerName = "content-container";

        /// <summary>
        /// The name for a container.
        /// </summary>
        public static readonly string containerName = "container";

        /// <summary>
        /// The name for a header element.
        /// </summary>
        public static readonly string headerName = "header";

        /// <summary>
        /// The name of a label element.
        /// </summary>
        public static readonly string labelName = "label";

        /// <summary>
        /// The name of a title element.
        /// </summary>
        public static readonly string titleName = "title";

        // ** USS class names ** //

        /// <summary>
        /// The generic USS class name for icons.
        /// </summary>
        public static readonly string iconUssClassName = "ge-icon";

        /// <summary>
        /// The prefix for the data type USS class name to add on icons.
        /// </summary>
        public static readonly string iconDataTypeClassPrefix = iconUssClassName.WithUssModifier(dataTypeClassUssModifierPrefix);

        /// <summary>
        /// Adds a stylesheet from the package to a visual element.
        /// </summary>
        /// <param name="ve">The visual element onto which to attach the stylesheet.</param>
        /// <param name="stylesheetName">The name of the stylesheet to attach.</param>
        public static void AddPackageStylesheet(this VisualElement ve, string stylesheetName)
        {
            AddStylesheet(ve, stylesheetName, null);
        }

        internal static string k_StyleSheetFolder = "StyleSheets/GraphToolkit/";
        internal static string k_IconFolder = "Icons/GraphToolkit/";

        internal static void AddStylesheet(this VisualElement ve, string stylesheetName, string path)
        {
            if (ve == null || stylesheetName == null)
                return;

            path ??= k_StyleSheetFolder;
            ve.AddStyleSheetPath($"{path}{stylesheetName}");
        }

        internal static void AddStyleSheetPath(this VisualElement ve, string stylesheetPath)
        {
            if (ve == null)
                return;

            var sheetAsset = EditorGUIUtility.Load(stylesheetPath) as StyleSheet;

            if (sheetAsset == null)
            {
                Debug.LogWarning(string.Format(CultureInfo.InvariantCulture, "Style sheet not found for path \"{0}\"", stylesheetPath));
                return;
            }

            ve.styleSheets.Add(sheetAsset);
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
        public static void AddStylesheetWithSkinVariants(this VisualElement ve, string stylesheetName)
        {
            var extension = Path.GetExtension(stylesheetName);
            var baseName = Path.ChangeExtension(stylesheetName, null);
            if (EditorGUIUtility.isProSkin)
            {
                AddPackageStylesheet(ve, baseName + "_dark" + extension);
            }
            else
            {
                AddPackageStylesheet(ve, baseName + "_light" + extension);
            }
            AddPackageStylesheet(ve, stylesheetName);
        }
    }
}
