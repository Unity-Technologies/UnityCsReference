// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class TemplateAssetExtensions
    {
        public static string[] GetPathToTemplateAsset(this TemplateAsset ta, VisualElement element)
        {
            var path = new List<string> { element.name };
            var parent = element.parent;
            var parentAsset = parent?.GetVisualElementAsset();

            while (parent != null && parentAsset != ta)
            {
                if (!string.IsNullOrEmpty(parent.name) && parent is TemplateContainer)
                {
                    path.Insert(0, parent.name);
                }

                parent = parent.parent;
                parentAsset = parent.GetVisualElementAsset();
            }

            return parentAsset != ta ? null : path.ToArray();
        }
    }
}
