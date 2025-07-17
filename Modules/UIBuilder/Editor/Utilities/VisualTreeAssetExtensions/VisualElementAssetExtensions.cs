// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class VisualElementAssetExtensions
    {
        public static bool IsSelected(this VisualElementAsset vea)
        {
            var value = vea.GetAttributeValue(BuilderConstants.SelectedVisualElementAssetAttributeName);
            return value == BuilderConstants.SelectedVisualElementAssetAttributeValue;
        }

        public static void Select(this VisualElementAsset vea)
        {
            vea.SetAttribute(
                BuilderConstants.SelectedVisualElementAssetAttributeName,
                BuilderConstants.SelectedVisualElementAssetAttributeValue);
        }

        public static void Deselect(this VisualElementAsset vea)
        {
            vea.SetAttribute(
                BuilderConstants.SelectedVisualElementAssetAttributeName,
                string.Empty);
        }
    }
}
