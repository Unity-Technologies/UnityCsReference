// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class UxmlObjectAssetExtensions
    {
        internal static void RemoveAssetAndFieldParentIfEmpty(this UxmlObjectAsset uxmlObjectAsset,
            bool onlyIfIsField = false)
        {
            if (onlyIfIsField && !uxmlObjectAsset.isField)
                return;

            var parentAsset = uxmlObjectAsset.parentAsset;
            uxmlObjectAsset.RemoveFromHierarchy();

            // Remove parent field if empty
            if (parentAsset is UxmlObjectAsset parentUxmlObjectAsset && parentAsset.childCount == 0)
            {
                parentUxmlObjectAsset.RemoveAssetAndFieldParentIfEmpty(true);
            }
        }
    }
}
