// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

namespace UnityEditorInternal
{
    // Keep internal and undocumented until we expose more functionality
    //*undocumented*
    public sealed class AssetStore
    {
        //*undocumented*
        public static void Open(string assetStoreURL)
        {
            if (assetStoreURL != "")
                AssetStoreWindow.OpenURL(assetStoreURL);
            else
                AssetStoreWindow.Init();
        }
    }
}
