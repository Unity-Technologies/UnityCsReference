using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.StyleSheets
{
    // Make sure style sheets importer after allowed dependent assets: textures, fonts, json and uss.
    // Has to be higher then AssetImportOrder.kImportOrderLate
    [ScriptedImporter(version: 1, ext: "tss", importQueueOffset: 1101)]
    [ExcludeFromPreset]
    class ThemeStyleSheetImporter : StyleSheetImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var contents = string.Empty;
            try
            {
                contents = File.ReadAllText(ctx.assetPath);
            }
            catch (IOException exc)
            {
                ctx.LogImportError($"IOException : {exc.Message}");
            }
            finally
            {
                var theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
                theme.hideFlags = HideFlags.NotEditable;

                if (!string.IsNullOrEmpty(contents))
                {
                    var importer = new StyleSheetImporterImpl(ctx);
                    importer.disableValidation = disableValidation | isWhitelisted;
                    importer.Import(theme, contents);
                }

                var icon = EditorGUIUtility.FindTexture("GUISkin Icon");
                ctx.AddObjectToAsset("themeStyleSheet", theme, icon);
                ctx.SetMainObject(theme);
            }
        }
    }
}
