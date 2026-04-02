// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.UIToolkit.Editor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class TemplateAssetExtensions
    {
        public static string[] GetPathToTemplateAsset(this TemplateAsset ta, VisualElement element)
        {
            return UxmlAssetUtilities.GetPathToTemplateAsset(ta, element, VisualElementExtensions.GetVisualElementAsset);
        }
    }
}
