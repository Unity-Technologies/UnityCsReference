// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.UIToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderLibraryTreeItem
    {
        public string name { get; }
        public Type type => item?.libraryType.type;
        public bool isHeader { get; set; }
        public bool hasPreview { get; set; }
        public VisualTreeAsset sourceAsset { get; }
        public string sourceAssetPath { get; }
        public Func<VisualElement> makeVisualElementCallback { get; }
        public Func<VisualTreeAsset, VisualElementAsset, VisualElement, VisualElementAsset> makeElementAssetCallback { get; }
        public Background icon { get; private set; }
        public Background largeIcon { get; private set; }
        public bool isEditorOnly { get; set; }
        public Background darkSkinIcon { get; private set; }
        public Background lightSkinIcon { get; private set; }
        public Background darkSkinLargeIcon { get; private set; }
        public Background lightSkinLargeIcon { get; private set; }
        internal LibraryItem item { get; set; }

        public BuilderLibraryTreeItem(
            string name,
            string iconName,
            Type type,
            Func<VisualElement> makeVisualElementCallback,
            Func<VisualTreeAsset, VisualElementAsset, VisualElement, VisualElementAsset> makeElementAssetCallback = null,
            VisualTreeAsset asset = null)
        {
            this.name = name;
            this.makeVisualElementCallback = makeVisualElementCallback;
            this.makeElementAssetCallback = makeElementAssetCallback;
            sourceAsset = asset;
            if (sourceAsset != null)
                sourceAssetPath = AssetDatabase.GetAssetPath(sourceAsset);

            if (type != null)
            {
                item = LibraryContent.GetDefaultLibraryItem(type);
                // Cache the builder icons.
                if (item != null)
                {
                    icon = item.icon;
                    largeIcon = item.largeIcon;
                    AssignIcon();
                }
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

        void AssignIcon()
        {
            var itemType = type.DeclaringType ?? type;

            darkSkinLargeIcon = UIResources.GetIconForType(itemType, UIResources.RequestSize.Px32, 1.0f, UIResources.EditorTheme.Dark);
            lightSkinLargeIcon = UIResources.GetIconForType(itemType, UIResources.RequestSize.Px32, 1.0f, UIResources.EditorTheme.Light);

            darkSkinIcon = UIResources.GetIconForType(itemType, UIResources.RequestSize.Px16, 1.0f, UIResources.EditorTheme.Dark);
            lightSkinIcon = UIResources.GetIconForType(itemType, UIResources.RequestSize.Px16, 1.0f, UIResources.EditorTheme.Light);
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
