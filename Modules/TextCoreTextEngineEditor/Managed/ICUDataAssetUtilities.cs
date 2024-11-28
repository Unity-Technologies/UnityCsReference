// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using System.IO;
using Unity.Collections;

namespace UnityEngine.TextCore.Text
{
    [InitializeOnLoad]
    internal class ICUDataAssetUtilities
    {
        private static string k_ICUDataAssetPath = "icudt73l.bytes";

        static ICUDataAssetUtilities()
        {
            TextLib.GetICUAssetEditorDelegate = GetEditorICUAsset;
        }

        internal static UnityEngine.TextAsset GetEditorICUAsset()
        {
            return AssetDatabase.GetBuiltinExtraResource<UnityEngine.TextAsset>(k_ICUDataAssetPath);
        }
    }
}
