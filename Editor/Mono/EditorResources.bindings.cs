// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    [NativeHeader("Editor/Src/EditorResources.h"), StaticAccessor("EditorResources", StaticAccessorType.DoubleColon)]
    partial class EditorResources
    {
        [NativeProperty("k_NormalSkinIndex", true, TargetType.Field)] public static extern int normalSkinIndex { get; }
        [NativeProperty("k_DarkSkinIndex", true, TargetType.Field)] public static extern int darkSkinIndex { get; }

        [NativeProperty("k_LightSkinSourcePath", true, TargetType.Field)] public static extern string lightSkinSourcePath { get; }
        [NativeProperty("k_DarkSkinSourcePath", true, TargetType.Field)] public static extern string darkSkinSourcePath { get; }
        [NativeProperty("k_FontsPath", true, TargetType.Field)] public static extern string fontsPath { get; }
        [NativeProperty("k_BrushesPath", true, TargetType.Field)] public static extern string brushesPath { get; }
        [NativeProperty("k_IconsPath", true, TargetType.Field)] public static extern string iconsPath { get; }
        [NativeProperty("k_GeneratedIconsPath", true, TargetType.Field)] public static extern string generatedIconsPath { get; }
        [NativeProperty("k_FolderIconName", true, TargetType.Field)] public static extern string folderIconName { get; }
        [NativeProperty("k_EmptyFolderIconName", true, TargetType.Field)] public static extern string emptyFolderIconName { get; }
        [NativeProperty("k_EditorDefaultResourcesPath", true, TargetType.Field)] public static extern string editorDefaultResourcesPath { get; }
        [NativeProperty("k_LibraryBundlePath", true, TargetType.Field)] public static extern string libraryBundlePath { get; }

        public static extern Object Load(string assetPath, Type type);
        public static extern string GetAssetPath(Object obj);
    }

    [Obsolete("EditorResourcesUtility is obsolete, please use EditorResources instead.", false)]
    internal sealed class EditorResourcesUtility : EditorResources
    {
    }
}
