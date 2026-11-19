// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine.Bindings;

namespace UnityEditor.UIElements
{
    internal static class VisualElementAssetExtensions
    {
        public static List<string> GetStyleSheetPaths(this VisualElementAsset vea)
        {
            List<string> ret = new List<string>();

            foreach (var sheet in vea.stylesheets)
            {
                var path = AssetDatabase.GetAssetPath(sheet);

                if (!string.IsNullOrEmpty(path))
                    ret.Add(path);
            }

            return ret;
        }
    }
}
