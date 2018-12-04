// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine.UIElements;

namespace UnityEditor.StyleSheets
{
    // Make sure style sheets importer after allowed dependent assets: textures and fonts
    [ScriptedImporter(version: 6, ext: "uss", importQueueOffset: 1000)]
    class StyleSheetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            string contents = File.ReadAllText(ctx.assetPath);

            StyleSheet asset = ScriptableObject.CreateInstance<StyleSheet>();
            asset.hideFlags = HideFlags.NotEditable;

            var importer = new StyleSheetImporterImpl(ctx);
            importer.Import(asset, contents);
        }
    }
}
