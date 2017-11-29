// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEditor;

namespace UnityEditorInternal
{
    [NativeHeader("Editor/Src/EditorResources.h")]
    internal class EditorResourcesUtility
    {
        [NativeProperty("EditorResources::kLightSkinSourcePath", true, TargetType.Field)]
        public static extern string lightSkinSourcePath { get; }
        [NativeProperty("EditorResources::kDarkSkinSourcePath", true, TargetType.Field)]
        public static extern string darkSkinSourcePath { get; }

        [NativeProperty("EditorResources::kFontsPath", true, TargetType.Field)]
        public static extern string fontsPath { get; }
        [NativeProperty("EditorResources::kBrushesPath", true, TargetType.Field)]
        public static extern string brushesPath { get; }
        [NativeProperty("EditorResources::kIconsPath", true, TargetType.Field)]
        public static extern string iconsPath { get; }
        [NativeProperty("EditorResources::kGeneratedIconsPath", true, TargetType.Field)]
        public static extern string generatedIconsPath { get; }
        [NativeProperty("EditorResources::kFolderIconName", true, TargetType.Field)]
        public static extern string folderIconName { get; }
        [NativeProperty("EditorResources::kEmptyFolderIconName", true, TargetType.Field)]
        public static extern string emptyFolderIconName { get; }
    }
}
