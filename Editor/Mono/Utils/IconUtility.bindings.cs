// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Utils
{
    [NativeHeader("Editor/Src/Commands/IconUtility.h")]
    static class IconUtility
    {
        [FreeFunction]
        extern public static void AddIconToWindowsExecutable(string path);

        [FreeFunction]
        extern public static bool SaveIcoForPlatform(string path, BuildTargetGroup buildTargetGroup, Vector2Int[] iconSizes);

        [FreeFunction]
        extern public static void SaveTextureToFile(string path, Texture2D texture, uint fileType);
    }
}
