// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using System.IO;
using Unity.Collections;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.TextCore.Text
{
    internal partial class ICUDataAssetUtilities
    {
        private static readonly string k_ICUDataAssetPath = "icudt73l.bytes";

        [OnCodeLoaded]
        static void Initialize()
        {
            TextLib.GetICUAssetEditorDelegate = GetEditorICUAsset;
        }

        internal static UnityEngine.TextAsset GetEditorICUAsset()
        {
            return AssetDatabase.GetBuiltinExtraResource<UnityEngine.TextAsset>(k_ICUDataAssetPath);
        }
    }
}
