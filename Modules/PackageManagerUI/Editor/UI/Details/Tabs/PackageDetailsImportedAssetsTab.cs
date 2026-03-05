// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsImportedAssetsTab : PackageDetailsTabElement
    {
        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
        internal const string k_Id = "importedassets";
        internal const int k_LabelColumnId = 0;
        internal const int k_LocationColumnId = 1;
        internal const int k_VersionColumnId = 2;
        internal const int k_NumColumns = 3;
        private const int k_MinColumnWidth = 70;

        private static readonly string k_LabelColumnTitle = L10n.Tr("Asset name");
        private static readonly string k_LocationColumnTitle = L10n.Tr("Location in project");
        private static readonly string k_VersionColumnTitle = L10n.Tr("Version");

        private MultiColumnListView m_ListView;

        private readonly List<Asset> m_Assets = new ();

        public override bool IsValid(IPackageVersion version)
        {
            return version?.importedAssets?.Count > 0;
        }

        private readonly IPackageManagerPrefs m_PackageManagerPrefs;
        public PackageDetailsImportedAssetsTab(IUnityConnectProxy unityConnect, IPackageManagerPrefs packageManagerPrefs) : base(unityConnect)
        {
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

            m_ListView.itemsSource = m_Assets;
            m_ListView.columnSortingChanged += OnColumnSortingChanged;
        }

        protected override void RefreshContent(IPackageVersion version)
        {
            m_Assets.Clear();
            if (version.importedAssets?.Count > 0)
                m_Assets.AddRange(version.importedAssets);
            SortAssetsAndRefreshItems();
        }

        private void AddColumnsAndRestoreSorting()
        {
            var columns = new Column[k_NumColumns];
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
                    image.image = InternalEditorUtility.GetIconForFile(m_Assets[index].importedPath);
                    var label = ve.Q<Label>();
                    var labelText = IOUtils.GetFileName(m_Assets[index].importedPath);
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
                    ((Label)ve).text = IOUtils.GetParentDirectory(m_Assets[index].importedPath);
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
                    var versionString = m_Assets[index].origin.packageVersion;
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
            m_PackageManagerPrefs.importedAssetsSortedColumns = m_ListView.sortedColumns.SelectAsEnumerable(c => new SortedColumn(c)).ToNewArray(k_NumColumns);
            SortAssetsAndRefreshItems();
        }

        private void SortAssetsAndRefreshItems()
        {
            m_Assets.Sort(new AssetComparer(m_PackageManagerPrefs.importedAssetsSortedColumns));
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
            private readonly SortedColumn[] m_SortedColumns;

            public AssetComparer(SortedColumn[] sortedColumns)
            {
                m_SortedColumns = sortedColumns ?? Array.Empty<SortedColumn>();
            }

            public int Compare(Asset x, Asset y)
            {
                if (x == null || y == null)
                    return 0;

                foreach (var c in m_SortedColumns)
                {
                    var result = CompareByColumn(c, x, y);
                    if (result != 0)
                        return result;
                }
                // Because we are using unstable sort, we want to add a final fallback sort metric on a unique property
                // to make the sort result stable
                return string.CompareOrdinal(x.importedPath, y.importedPath);
            }

            private int CompareByColumn(SortedColumn column, Asset x, Asset y)
            {
                var result = 0;
                if (column.columnIndex == k_LabelColumnId)
                    result = string.CompareOrdinal(IOUtils.GetFileName(x.importedPath), IOUtils.GetFileName(y.importedPath));
                else if (column.columnIndex == k_LocationColumnId)
                    result = string.CompareOrdinal(IOUtils.GetParentDirectory(x.importedPath), IOUtils.GetParentDirectory(y.importedPath));
                else
                    result = string.CompareOrdinal(x.origin.packageVersion, y.origin.packageVersion);
                return column.sortDirection == SortDirection.Ascending ? result : -result;
            }
        }
    }
}
