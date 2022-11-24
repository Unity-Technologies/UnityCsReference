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
        private IOProxy m_IOProxy;
        private PackageManagerPrefs m_PackageManagerPrefs;

        private IList<Asset> assets => m_ListView.itemsSource as IList<Asset>;

        public override bool IsValid(IPackageVersion version)
        {
            return version?.importedAssets?.Any() == true;
        }

        public PackageDetailsImportedAssetsTab(IOProxy iOProxy, PackageManagerPrefs packageManagerPrefs)
        {
            m_IOProxy = iOProxy;
            m_PackageManagerPrefs = packageManagerPrefs;

            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Imported Assets");

            name = "importedAssetsDetailsContainer";
            m_ListView = new MultiColumnListView
            {
                sortingEnabled = true,
                name = "InstalledAssetsList",
                scrollView = { verticalScrollerVisibility = ScrollerVisibility.Auto},
                selectionType = SelectionType.None,
                itemsSource = Array.Empty<Asset>(),
                columns = { reorderable = false }
            };
            Add(m_ListView);
            AddColumnsAndRestoreSorting();

            m_ListView.columnSortingChanged += OnColumnSortingChanged;
        }

        public override void Refresh(IPackageVersion version)
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
                    (ve as Label).text = m_IOProxy.GetParentDirectory(assets[index].importedPath);
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
                    (ve as Label).text = assets[index].origin.packageVersion;
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

        public void RecalculateTabHeight(float detailHeight, float scrollViewHeight, float detailsHeaderHeight, float tabViewHeaderContainerHeight, float customContainerHeight, float extensionContainerHeight)
        {
            if (!UIUtils.IsElementVisible(this))
                return;
            var headerTotalHeight = detailsHeaderHeight + tabViewHeaderContainerHeight + customContainerHeight + extensionContainerHeight;
            var leftOverHeight = detailHeight - headerTotalHeight - layout.height;
            style.height = scrollViewHeight -  headerTotalHeight - leftOverHeight;
        }

        private class AssetComparer : IComparer<Asset>
        {
            private IEnumerable<SortColumnDescription> m_SortColumnDescriptions;
            private IOProxy m_IOProxy;

            public AssetComparer(IOProxy ioProxy, IEnumerable<SortColumnDescription> sortColumnDescriptions)
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
