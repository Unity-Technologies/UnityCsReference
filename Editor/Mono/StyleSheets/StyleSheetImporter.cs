// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine.StyleSheets;

namespace UnityEditor.StyleSheets
{
    [ScriptedImporter(3, "uss", -20)]
    class StyleSheetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var importer = new StyleSheetImporterImpl();
            string contents = File.ReadAllText(ctx.assetPath);
            StyleSheet asset = ScriptableObject.CreateInstance<StyleSheet>();
            asset.hideFlags = HideFlags.NotEditable;

            if (importer.Import(asset, contents))
            {
                ctx.AddObjectToAsset("stylesheet", asset);

                // Force the pre processor to rebuild its list of referenced asset paths
                // as paths may have changed with this import.
                StyleSheetAssetPostprocessor.ClearReferencedAssets();

                // Clear the style cache to force all USS rules to recompute
                // and subsequently reload/reimport all images.
                StyleContext.ClearStyleCache();
            }
            else
            {
                foreach (string importError in importer.errors.FormatErrors())
                {
                    Debug.LogError(importError);
                }
            }
        }
    }
}
