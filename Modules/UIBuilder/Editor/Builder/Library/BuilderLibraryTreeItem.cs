// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderLibraryTreeItem
    {
        public string name { get; }
        public Type type { get; }
        public bool isHeader { get; set; }
        public bool hasPreview { get; set; }
        public VisualTreeAsset sourceAsset { get; }
        public string sourceAssetPath { get; }
        public Func<VisualElement> makeVisualElementCallback { get; }
        public Func<VisualTreeAsset, VisualElementAsset, VisualElement, VisualElementAsset> makeElementAssetCallback { get; }
        public Texture2D icon { get; private set; }
        public Texture2D largeIcon { get; private set; }
        public bool isEditorOnly { get; set; }
        public Texture2D darkSkinIcon { get; private set; }
        public Texture2D lightSkinIcon { get; private set; }
        public Texture2D darkSkinLargeIcon { get; private set; }
        public Texture2D lightSkinLargeIcon { get; private set; }

        public BuilderLibraryTreeItem(
            string name, string iconName, Type type, Func<VisualElement> makeVisualElementCallback,
            Func<VisualTreeAsset, VisualElementAsset, VisualElement, VisualElementAsset> makeElementAssetCallback = null, VisualTreeAsset asset = null)
        {
            this.name = name;
            this.makeVisualElementCallback = makeVisualElementCallback;
            this.makeElementAssetCallback = makeElementAssetCallback;
            sourceAsset = asset;
            if (sourceAsset != null)
                sourceAssetPath = AssetDatabase.GetAssetPath(sourceAsset);

            this.type = type;
            if (!string.IsNullOrEmpty(iconName))
            {
                AssignIcon(iconName);
                if (icon == null)
                    AssignIcon("VisualElement");
            }
        }

        internal static int GetItemId(string name, Type type, VisualTreeAsset asset, int id = default)
        {
            if (id != default)
                return id;

            if (asset != null)
                return AssetDatabase.GetAssetPath(asset).GetHashCode();

            return (name + type?.FullName).GetHashCode();
        }

        void AssignIcon(string iconName)
        {
            var darkSkinResourceBasePath = $"{BuilderConstants.IconsResourcesPath}/Dark/Library/";
            var lightSkinResourceBasePath = $"{BuilderConstants.IconsResourcesPath}/Light/Library/";

            darkSkinLargeIcon = LoadLargeIcon(darkSkinResourceBasePath, iconName);
            lightSkinLargeIcon = LoadLargeIcon(lightSkinResourceBasePath, iconName);

            darkSkinIcon = LoadIcon(darkSkinResourceBasePath, iconName);
            lightSkinIcon = LoadIcon(lightSkinResourceBasePath, iconName);

            if (EditorGUIUtility.isProSkin)
            {
                icon = darkSkinIcon;
                largeIcon = darkSkinLargeIcon;
            }
            else
            {
                icon = lightSkinIcon;
                largeIcon = lightSkinLargeIcon;
            }
        }

        Texture2D LoadIcon(string resourceBasePath, string iconName)
        {
            return EditorGUIUtility.Load(EditorGUIUtility.pixelsPerPoint > 1
                ? $"{resourceBasePath}{iconName}@2x.png"
                : $"{resourceBasePath}{iconName}.png") as Texture2D;
        }

        Texture2D LoadLargeIcon(string resourceBasePath, string iconName)
        {
            return EditorGUIUtility.Load(EditorGUIUtility.pixelsPerPoint > 1
                ? $"{resourceBasePath}{iconName}@8x.png"
                : $"{resourceBasePath}{iconName}@4x.png") as Texture2D;
        }

        public void SetIcon(Texture2D icon)
        {
            this.icon = icon;
            largeIcon = icon;
            darkSkinIcon = icon;
            lightSkinIcon = icon;
        }
    }
}
