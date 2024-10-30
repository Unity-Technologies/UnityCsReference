// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    // This currently needs to be in the UI Builder module in order to access the code to export to UXML
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UIElementsViewImporter))]
    class UIElementsViewImporterEditor : ScriptedImporterEditor
    {
        protected override bool needsApplyRevert => false;

        internal const string k_ApplyButtonName = "apply-fixes";
        static readonly string k_MultipleAssetsInfo = L10n.Tr("{0} of the selected assets can be updated to fix some warnings.\nClick the button below to apply the update to the UXML files.\n\n");
        static readonly string k_SingleAssetsInfo = L10n.Tr("This asset can be updated to fix some warnings.\nClick the button below to apply the update to the UXML file.\n\n");
        static readonly string k_Warning = L10n.Tr("This action will update asset paths to resolve file references and prevent future warnings.\nApplying the fix will replace the entire UXML file.\n<b>Custom comments and formatting may be lost.</b>");
        static readonly string k_ApplyButtonLabel = L10n.Tr("Apply (Overwrites File)");
        static readonly string k_ApplyButtonLabelMultiple = L10n.Tr("Apply (Overwrites Files)");

        public override VisualElement CreateInspectorGUI()
        {
            // Collect all the assets that can be upgraded
            var upgradedAssets = new List<VisualTreeAsset>();
            foreach(var t in targets)
            {
                var importer = (UIElementsViewImporter)t;
                var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(importer.assetPath);
                if (vta != null && vta.importerWithUpdatedUrls)
                    upgradedAssets.Add(vta);
            }

            if (upgradedAssets.Count == 0)
                return null;

            bool multiple = targets.Length != 1;
            var message = multiple ? k_SingleAssetsInfo : string.Format(k_MultipleAssetsInfo, upgradedAssets.Count);
            message += k_Warning;

            var root = new VisualElement();
            root.Add(new HelpBox(message, HelpBoxMessageType.Warning));
            root.Add(new Button(() =>
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var vta in upgradedAssets)
                    {
                        var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(vta)) as UIElementsViewImporter;
                        if (importer == null)
                            continue;

                        var uxml = Unity.UI.Builder.VisualTreeAssetToUXML.GenerateUXML(vta, importer.assetPath, true);
                        File.WriteAllText(importer.assetPath, uxml);
                        AssetDatabase.ImportAsset(importer.assetPath);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            })
            {
                text = multiple ? k_ApplyButtonLabelMultiple : k_ApplyButtonLabel,
                name = k_ApplyButtonName,
                style = { alignSelf = Align.FlexEnd }
            });

            return root;
        }
    }
}
