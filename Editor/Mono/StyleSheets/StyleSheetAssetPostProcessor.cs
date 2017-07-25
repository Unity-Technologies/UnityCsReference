// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityStyleSheet = UnityEngine.StyleSheets.StyleSheet;

namespace UnityEditor.StyleSheets
{
    class StyleSheetAssetPostprocessor : AssetPostprocessor
    {
        private static HashSet<string> s_StyleSheetReferencedAssetPaths;

        public static void ClearReferencedAssets()
        {
            s_StyleSheetReferencedAssetPaths = null;
        }

        public static void AddReferencedAssetPath(string assetPath)
        {
            if (s_StyleSheetReferencedAssetPaths == null)
                s_StyleSheetReferencedAssetPaths = new HashSet<string>();

            s_StyleSheetReferencedAssetPaths.Add(assetPath);
        }

        private static void ProcessAssetPath(string assetPath)
        {
            if (s_StyleSheetReferencedAssetPaths == null || !s_StyleSheetReferencedAssetPaths.Contains(assetPath))
                return;

            // Clear the style cache to force all USS rules to recompute
            // and subsequently reload/reimport all images.
            StyleContext.ClearStyleCache();

            // Force all currently active panels to repaint by marking them dirty.
            var iterator = UIElementsUtility.GetPanelsIterator();
            while (iterator.MoveNext())
            {
                var panel = iterator.Current.Value;
                panel.visualTree.Dirty(ChangeType.Styles | ChangeType.Repaint);
            }
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (s_StyleSheetReferencedAssetPaths == null)
                return;

            foreach (var deletedAssetPath in deletedAssets)
            {
                ProcessAssetPath(deletedAssetPath);
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                ProcessAssetPath(movedAssets[i]);
                ProcessAssetPath(movedFromAssetPaths[i]);
            }
        }
    }
}
