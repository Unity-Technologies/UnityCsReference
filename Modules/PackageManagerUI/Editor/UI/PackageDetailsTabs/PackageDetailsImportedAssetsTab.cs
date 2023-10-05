// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsImportedAssetsTab : PackageDetailsTabElement
    {
        internal const string k_Id = "importedassets";
        internal const int k_LabelColumnId = 0;
        internal const int k_LocationColumnId = 1;
        internal const int k_VersionColumnId = 2;
        private const int k_MinColumnWidth = 70;

        private static readonly string k_LabelColumnTitle = L10n.Tr("Asset name");
        private static readonly string k_LocationColumnTitle = L10n.Tr("Location in project");
        private static readonly string k_VersionColumnTitle = L10n.Tr("Version");

        private MultiColumnListView m_ListView;

        private IList<Asset> assets => m_ListView.itemsSource as IList<Asset>;

        public override bool IsValid(IPackageVersion version)
        {
            return version?.importedAssets?.Any() == true;
        }

        private readonly IIOProxy m_IOProxy;
        private readonly IPackageManagerPrefs m_PackageManagerPrefs;
        public PackageDetailsImportedAssetsTab(IUnityConnectProxy unityConnect, IIOProxy iOProxy,
            IPackageManagerPrefs packageManagerPrefs) : base(unityConnect)
        {
            m_IOProxy = iOProxy;
            m_PackageManagerPrefs = packageManagerPrefs;

            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Imported Assets");

            name = "importedAssetsDetailsContainer";
            m_ListView = new MultiColumnListView
            {
                sortingMode = ColumnSortingMode.Custom,
                name = "ImportedAssetsList",
                scrollView = { verticalScrollerVisibility = ScrollerVisibility.Auto},
                selectionType = SelectionType.None,
                itemsSource = Array.Empty<Asset>(),
                columns = { reorderable = false }
            };
            m_ContentContainer.Add(m_ListView);
            AddColumnsAndRestoreSorting();

            m_ListView.columnSortingChanged += OnColumnSortingChanged;
        }

        protected override void RefreshContent(IPackageVersion version)
        {
            SortAssetsAndRefreshItems(version.importedAssets);
        }

        private void AddColumnsAndRestoreSorting()
        {
            var columns = new Column[3];
            columns[k_LabelColumnId] = new Column
            {
                name = "label",
                makeHeader = () => new Label
                {
                    name = "columnHeader",
                    text = k_LabelColumnTitle,
                    tooltip = k_LabelColumnTitle
                },
                makeCell = () =>
                {
                    var ve = new VisualElement { name = "iconAndLabelContainer" };
                    ve.Add(new Image());
                    ve.Add(new Label());
                    return ve;
                },
                bindCell = (ve, index) =>
                {
                    var image = ve.Q<Image>();
                    image.image = InternalEditorUtility.GetIconForFile(assets[index].importedPath);
                    var label = ve.Q<Label>();
                    var labelText = m_IOProxy.GetFileName(assets[index].importedPath);
                    label.text = labelText;
                    ve.tooltip = labelText;
                },
                stretchable = true,
                minWidth = k_MinColumnWidth
            };

            columns[k_LocationColumnId] = new Column
            {
                name = "location",
                makeHeader = () => new Label
                {
                    name = "columnHeader",
                    text = k_LocationColumnTitle,
                    tooltip = k_LocationColumnTitle
                },
                bindCell = (ve, index) =>
                {
                    ((Label)ve).text = m_IOProxy.GetParentDirectory(assets[index].importedPath);
                },
                stretchable = true,
                minWidth = k_MinColumnWidth
            };

            columns[k_VersionColumnId] = new Column
            {
                name = "version",
                makeHeader = () => new Label
                {
                    name = "columnHeader",
                    text = k_VersionColumnTitle,
                    tooltip = k_VersionColumnTitle
                },
                bindCell = (ve, index) =>
                {
                    var versionString = assets[index].origin.packageVersion;
                    if (!string.IsNullOrEmpty(versionString))
                    {
                        ((Label)ve).text = versionString;
                        ve.tooltip = string.Empty;
                    }
                    else
                    {
                        ((Label)ve).text = "-";
                        ve.tooltip = L10n.Tr("This asset's version is unknown because it was imported with an older version of Unity. For accurate version tracking, import the asset again or update it.");
                    }

                },
                stretchable = true,
                resizable = false,
                minWidth = k_MinColumnWidth
            };

            foreach (var column in columns)
                m_ListView.columns.Add(column);

            foreach (var column in m_PackageManagerPrefs.importedAssetsSortedColumns)
                m_ListView.sortColumnDescriptions.Add(new SortColumnDescription(column.columnIndex, column.sortDirection));
        }

        private void OnColumnSortingChanged()
        {
            m_PackageManagerPrefs.importedAssetsSortedColumns = m_ListView.sortedColumns.Select(c => new SortedColumn(c)).ToArray();
            SortAssetsAndRefreshItems(assets);
        }

        private void SortAssetsAndRefreshItems(IEnumerable<Asset> assets)
        {
            if (m_ListView.sortedColumns?.Any() != true)
                m_ListView.itemsSource = assets != null ? assets.ToArray() : Array.Empty<Asset>();
            else
                m_ListView.itemsSource = assets.OrderBy(a => a, new AssetComparer(m_IOProxy, m_ListView.sortedColumns)).ToArray();

            m_ListView.RefreshItems();
        }

        protected override void DerivedRefreshHeight(float detailHeight, float scrollViewHeight, float detailsHeaderHeight, float tabViewHeaderContainerHeight, float customContainerHeight, float extensionContainerHeight)
        {
            var headerTotalHeight = detailsHeaderHeight + tabViewHeaderContainerHeight + customContainerHeight + extensionContainerHeight;
            var leftOverHeight = detailHeight - headerTotalHeight - layout.height;
            style.height = scrollViewHeight -  headerTotalHeight - leftOverHeight;
        }

        private class AssetComparer : IComparer<Asset>
        {
            private IEnumerable<SortColumnDescription> m_SortColumnDescriptions;
            private IIOProxy m_IOProxy;

            public AssetComparer(IIOProxy ioProxy, IEnumerable<SortColumnDescription> sortColumnDescriptions)
            {
                m_SortColumnDescriptions = sortColumnDescriptions;
                m_IOProxy = ioProxy;
            }

            public int Compare(Asset x, Asset y)
            {
                return m_SortColumnDescriptions.Select(c => CompareByColumn(c, x, y)).FirstOrDefault(result => result != 0);
            }

            private int CompareByColumn(SortColumnDescription description, Asset x, Asset y)
            {
                var result = 0;
                if (description.column.index == k_LabelColumnId)
                    result = string.Compare(m_IOProxy.GetFileName(x.importedPath), m_IOProxy.GetFileName(y.importedPath));
                else if (description.column.index == k_LocationColumnId)
                    result = string.Compare(m_IOProxy.GetParentDirectory(x.importedPath), m_IOProxy.GetParentDirectory(y.importedPath));
                else
                    result = string.Compare(x.origin.packageVersion, y.origin.packageVersion);

                return description.direction == SortDirection.Ascending ? result : -result;
            }
        }
    }
}
