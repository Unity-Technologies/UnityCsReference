// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AssetImporters;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    // This currently needs to be in the UI Builder module in order to access the code to export to UXML
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UIElementsViewImporter))]
    class UIElementsViewImporterEditor : ScriptedImporterEditor
    {
        protected override bool needsApplyRevert => false;

        internal const string k_ApplyUpgradesButtonName = "apply-upgrades";
        static readonly string k_UpgradeWarning = L10n.Tr("Applying upgrades will replace the entire UXML file.\n<b>Custom comments and formatting may be lost.</b>");
        static readonly string k_ApplyUpgradesButtonLabel = L10n.Tr("Apply Upgrades (Overwrites File)");
        static readonly string k_ApplyUpgradesButtonLabelMultiple = L10n.Tr("Apply Upgrades (Overwrites Files)");

        static UxmlUpgradeService s_UpgradeService = new UxmlUpgradeService();

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // Add UXML upgrade section
            var uxmlUpgradeSection = CreateUxmlUpgradeSection();
            if (uxmlUpgradeSection != null)
                root.Add(uxmlUpgradeSection);

            return root.childCount > 0 ? root : null;
        }

        private VisualElement CreateUxmlUpgradeSection()
        {
            // Collect all assets
            using var _assets = ListPool<VisualTreeAsset>.Get(out var upgradeableAssets);
            foreach (var t in targets)
            {
                var importer = (UIElementsViewImporter)t;
                var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(importer.assetPath);
                if (vta != null)
                    upgradeableAssets.Add(vta);
            }

            var section = new Foldout 
            { 
                text = L10n.Tr("UXML Upgrades"), 
                viewDataKey = "uxml-upgrades",
                tooltip = L10n.Tr("Apply upgrades to maintain or modernize UXML assets.")
            };

            // Show informational message about upgrades
            section.Add(new HelpBox(L10n.Tr(
                "Selected UXML assets may contain patterns that can be upgraded.\n" +
                "Some upgrades detect issues ahead of time, while others report changes after running. " +
                "Use the options below to review and apply upgrades based on your target version and use cases."),
                HelpBoxMessageType.Info));

            // Check if any assets have broken URL references
            foreach (var vta in upgradeableAssets)
            {
                if (vta.importerWithUpdatedUrls)
                {
                    section.Add(new HelpBox(L10n.Tr("Broken URL references detected in some selected assets. Apply URL Fixes to resolve file references and prevent import warnings."),
                    HelpBoxMessageType.Warning));
                    break;
                }
            }

            // Show all upgraders in a multi-column list view
            var nameColumn = new Column
            {
                name = "name",
                title = L10n.Tr("Upgrader"),
                stretchable = true,
                sortable = false
            };
            var enabledColumn = new Column
            {
                name = "enabled",
                title = L10n.Tr("Enabled"),
                width = 80,
                stretchable = false,
                sortable = false
            };

            var columns = new Columns
            {
                nameColumn,
                enabledColumn
            };

            var listView = new MultiColumnListView(columns)
            {
                showBorder = true,
                itemsSource = s_UpgradeService.m_Upgraders,
                fixedItemHeight = 20,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            };

            nameColumn.makeCell = () => new Label();
            nameColumn.bindCell = (element, index) =>
            {
                var label = (Label)element;
                var upgrader = s_UpgradeService.upgraders[index];
                label.text = upgrader.name;
                label.tooltip = upgrader.description;
            };

            enabledColumn.makeCell = () => new Toggle();
            enabledColumn.bindCell = (element, index)  =>
            {
                var toggle = (Toggle)element;
                var upgrader = s_UpgradeService.upgraders[index];

                toggle.SetValueWithoutNotify(s_UpgradeService.IsUpgraderEnabled(upgrader));
                toggle.RegisterValueChangedCallback(evt =>
                {
                    s_UpgradeService.SetUpgraderEnabled(upgrader, evt.newValue);
                });
            };

            section.Add(listView);

            section.Add(new Button(() =>
            {
                using var _ = ListPool<VisualTreeAsset>.Get(out var assets);
                foreach (var t in targets)
                {
                    var importer = (UIElementsViewImporter)t;
                    var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(importer.assetPath);
                    if (vta != null)
                        assets.Add(vta);
                }

                s_UpgradeService.ApplyUpgrades(assets);
            })
            {
                text = targets.Length > 1 ? k_ApplyUpgradesButtonLabelMultiple : k_ApplyUpgradesButtonLabel,
                name = k_ApplyUpgradesButtonName,
                tooltip = k_UpgradeWarning
            });

            return section;
        }
    }
}
