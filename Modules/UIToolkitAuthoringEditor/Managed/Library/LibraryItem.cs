// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class LibraryItem
    {
        public const string DragDataKey = "LibraryItem";

        public string name { get; }
        public LibraryTypeKey libraryType { get; }
        public string libraryPath { get; }
        public Background icon { get; set; }
        public Background largeIcon { get; set; }

        public LibraryItem(string name, LibraryTypeKey typeKey, Background icon, Background largeIcon, string path)
        {
            this.name = name;
            libraryType = typeKey;
            libraryPath = path;
            this.icon = icon;
            this.largeIcon = largeIcon;
        }

        public LibraryItem(string name, LibraryTypeKey typeKey) : this(name, typeKey, null)
        {
        }

        public LibraryItem(string name, LibraryTypeKey typeKey, string path)
        {
            this.name = name;
            libraryType = typeKey;
            libraryPath = path;
            AssignIcon();
        }

        void AssignIcon()
        {
            var type = libraryType.type;
            if (EditorGUIUtility.isProSkin)
            {
                icon = UIResources.GetIconForType(type, UIResources.RequestSize.Px16, 1.0f, UIResources.EditorTheme.Dark);
                largeIcon = UIResources.GetIconForType(type, UIResources.RequestSize.Px32, 1.0f, UIResources.EditorTheme.Dark);
            }
            else
            {
                icon = UIResources.GetIconForType(type, UIResources.RequestSize.Px16, 1.0f, UIResources.EditorTheme.Light);
                largeIcon = UIResources.GetIconForType(type, UIResources.RequestSize.Px32, 1.0f, UIResources.EditorTheme.Light);
            }
        }
    }
}
