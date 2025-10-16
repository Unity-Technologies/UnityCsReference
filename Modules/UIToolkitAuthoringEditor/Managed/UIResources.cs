// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEditor;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Utility class to retrieve UI Toolkit-related resources.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal static class UIResources
    {
        /// <summary>
        /// Allows to request for a specific theme when querying a resource.
        /// </summary>
        public enum EditorTheme
        {
            Current,
            Light,
            Dark
        }

        /// <summary>
        /// Allows to request a resource for a specific target size.
        /// </summary>
        public enum RequestSize
        {
            /// <summary>
            /// Target size of 16 pixels.
            /// </summary>
            Px16,
            /// <summary>
            /// Target size of 32 pixels.
            /// </summary>
            Px32,
            /// <summary>
            /// Target size of 64 pixels.
            /// </summary>
            Px64,
            /// <summary>
            /// Target size of 128 pixels.
            /// </summary>
            Px128
        }

        [NoAutoStaticsCleanup]
        private static readonly HashSet<string> k_SupportedVectorImageTypes = new HashSet<string> { ".svg" };

        /// <summary>
        /// Returns the icon for the provided <see cref="VisualElement"/> instance.
        /// </summary>
        /// <param name="element">The requested <see cref="VisualElement"/>.</param>
        /// <param name="requestSize">Indicates the requested size so the correct "@_x" version is chosen.</param>
        /// /// <param name="scaledPixelsPerPoint">The scaled pixel per point to use.</param>
        /// <param name="theme">The theme to request the icon for.</param>
        /// <returns>A <see cref="Background"/> of the requested resources or <see langword="default"/>.</returns>
        public static Background GetIconForElement(VisualElement element, RequestSize requestSize, float scaledPixelsPerPoint = 1.0f, EditorTheme theme = EditorTheme.Current)
        {
            return GetIconForType(element.GetType(), requestSize, scaledPixelsPerPoint, theme);
        }

        /// <summary>
        /// Returns the icon for the provided type.
        /// </summary>
        /// <param name="type">The requested type.</param>
        /// <param name="requestSize">Indicates the requested size so the correct "@_x" version is chosen.</param>
        /// <param name="scaledPixelsPerPoint">The scaled pixel per point to use.</param>
        /// <param name="theme">The theme to request the icon for.</param>
        /// <returns>A <see cref="Background"/> of the requested resources or <see langword="default"/>.</returns>
        public static Background GetIconForType(Type type, RequestSize requestSize, float scaledPixelsPerPoint = 1.0f, EditorTheme theme = EditorTheme.Current)
        {
            var iconPath = EditorGUIUtility.GetIconPathFromAttribute(type, inherit: false);

            var background = GetIconForSkin(iconPath, requestSize, scaledPixelsPerPoint, theme);
            return background.GetSelectedImage()
                ? background
                : GetIconForSkin("UIToolkit/Icons/CustomCSharpElement.png", requestSize, scaledPixelsPerPoint, theme);
        }

        /// <summary>
        /// Returns the icon at the provided path.
        /// </summary>
        /// <param name="path">The path of the icon</param>
        /// <param name="requestSize">Indicates the requested size so the correct "@_x" version is chosen.</param>
        /// <param name="scaledPixelsPerPoint">The scaled pixel per point to use.</param>
        /// <returns>A <see cref="Background"/> of the requested icon at the path provided or <see langword="default"/>.</returns>
        public static Background LoadIcon(string path, RequestSize requestSize = RequestSize.Px16,  float scaledPixelsPerPoint = 1.0f, EditorTheme theme = EditorTheme.Current)
        {
            var background = GetIconForSkin(path, requestSize, scaledPixelsPerPoint, theme);
            return background.GetSelectedImage()
                ? background
                : GetIconForSkin("UIToolkit/Icons/CustomCSharpElement.png", requestSize, scaledPixelsPerPoint, theme);

        }

        private static string GetResolution(RequestSize requestSize, bool isVectorImage, float scaledPixelsPerPoint = 1.0f)
        {
            if (isVectorImage)
                return string.Empty;

            return requestSize switch
            {
                RequestSize.Px16 => scaledPixelsPerPoint > 1.0f ? "@2x" : string.Empty,
                RequestSize.Px32 => scaledPixelsPerPoint > 1.0f ? "@4x" : "@2x",
                RequestSize.Px64 => scaledPixelsPerPoint > 1.0f ? "@8x" : "@4x",
                _ => "@8x"
            };
        }

        private static Background GetIconForSkin(string iconPath, RequestSize requestSize, float scaledPixelsPerPoint,
            EditorTheme theme = EditorTheme.Current)
        {
            if (string.IsNullOrEmpty(iconPath))
                return default;

            var useDarkTheme = theme switch
            {
                EditorTheme.Current => EditorGUIUtility.isProSkin,
                EditorTheme.Light => false,
                EditorTheme.Dark => true,
                _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, null)
            };

            var extension = Path.GetExtension(iconPath);
            var name = Path.GetFileNameWithoutExtension(iconPath);
            var directory = Path.GetDirectoryName(iconPath);
            if (!string.IsNullOrEmpty(directory))
                directory += "/";

            var isVectorImage = k_SupportedVectorImageTypes.Contains(extension);

            var resolution = GetResolution(requestSize, isVectorImage, scaledPixelsPerPoint);
            var path = $"{directory}{(useDarkTheme ? "d_" : "")}{name}{resolution}{extension}";
            var iconObject = EditorGUIUtility.Load(path);
            return iconObject
                ? Background.FromObject(iconObject)
                : default;
        }
    }
}
