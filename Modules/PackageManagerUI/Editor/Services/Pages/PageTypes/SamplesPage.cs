// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.PackageManager.UI.Internal;

[Serializable]
internal class SamplesPage : SimplePage
{
    public static readonly PageSortOption[] k_SupportedSortOptions = { PageSortOption.NameAsc, PageSortOption.NameDesc, PageSortOption.PublishedDateDesc };
    public static readonly PageFilterStatus[] k_SupportedStatusFilters = { PageFilterStatus.Imported, PageFilterStatus.UpdateAvailable, PageFilterStatus.Deprecated };

    public const string k_Id = "Samples";

    public override string id => k_Id;
    public override string displayName => L10n.Tr("All Samples");
    public override Icon icon => Icon.SamplesPage;

    public override RefreshOptions refreshOptions => RefreshOptions.UpmList | RefreshOptions.ImportedSamples;

    [NonSerialized]
    protected IPackageDatabase m_PackageDatabase;
    [ExcludeFromCodeCoverage]
    public void ResolveDependencies(IPackageDatabase packageDatabase)
    {
        m_PackageDatabase = packageDatabase;
    }

    public SamplesPage(IPackageDatabase packageDatabase)
    {
        ResolveDependencies(packageDatabase);

        UpdateSupportedSortOptions(k_SupportedSortOptions, false);
        UpdateSupportedStatuses(k_SupportedStatusFilters, false);
    }

    public override void OnEnable()
    {
        m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
        m_PackageDatabase.onSamplesChanged += OnSamplesChanged;
    }

    public override void OnDisable()
    {
        m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
        m_PackageDatabase.onSamplesChanged -= OnSamplesChanged;
    }

    private void OnPackagesChanged(PackagesChangeArgs args)
    {
        if (!isActive)
            return;

        var updatedSampleUniqueIds = new List<string>();
        foreach (var package in args.updated)
        {
            var sampleCollection = m_PackageDatabase.GetSamples(package.uniqueId);
            if (sampleCollection != null)
                updatedSampleUniqueIds.AddRange(sampleCollection.SelectAsEnumerable(s => s.uniqueId));
        }
        IncrementalListUpdate(updated: updatedSampleUniqueIds);
    }

    private void OnSamplesChanged(SamplesChangeArgs args)
    {
        if (!isActive)
            return;

        var addedOrUpdatedSamples = new Dictionary<string, Sample>();
        foreach (var collection in args.added.Join(args.updated))
            foreach (var sample in collection)
                addedOrUpdatedSamples[sample.uniqueId] = sample;

        var addList = new List<string>();
        var updateList = new List<string>();
        var removeList = new List<string>();
        foreach (var sample in addedOrUpdatedSamples.Values)
        {
            if (visualStates.Contains(sample.uniqueId))
                updateList.Add(sample.uniqueId);
            else
                addList.Add(sample.uniqueId);
        }

        foreach (var collection in args.removed)
            removeList.AddRange(collection.SelectAsEnumerable(s => s.uniqueId).Filter(i => visualStates.Contains(i)));
        foreach (var collection in args.preUpdate)
            removeList.AddRange(collection.SelectAsEnumerable(s => s.uniqueId).Filter(i => visualStates.Contains(i) && !addedOrUpdatedSamples.ContainsKey(i)));

        IncrementalListUpdate(addList, updateList, removeList);
    }

    protected override bool MatchesSearchTextAndFilter(string itemUniqueId)
    {
        var sample = m_PackageDatabase.GetSample(itemUniqueId);
        if (sample.isDefault)
            return false;
        var packageUniqueIds = filters.packageUniqueIds;
        if (packageUniqueIds.Count > 0 && !packageUniqueIds.ContainsMatches(sample.packageUniqueId))
            return false;
        var filterByStatus = filters.status;
        return (filterByStatus != PageFilterStatus.Imported || sample.isImported || sample.previousImportPaths?.Count > 0)
               && (filterByStatus != PageFilterStatus.UpdateAvailable || (!sample.isImported && sample.previousImportPaths?.Count > 0))
               && (filterByStatus != PageFilterStatus.Deprecated || sample.package.versions.primary.HasTag(PackageTag.Deprecated))
               && sample.MatchesSearchText(trimmedSearchText);
    }

    protected override void RebuildVisualStateList()
    {
        var includedItems = new List<(Sample sample, VisualState visualState)>();
        foreach (var collection in m_PackageDatabase.sampleCollections)
        {
            foreach (var sample in collection)
            {
                var visualState = m_VisualStateList.Get(sample.uniqueId) ?? new VisualState(sample.uniqueId);
                visualState.groupName = GetGroupName(sample);
                includedItems.Add((sample, visualState));
            }
        }
        includedItems.Sort(new Comparer(filters.sortOption, CompareGroupName));
        m_VisualStateList.Rebuild(includedItems.SelectToNewArray(i => i.visualState));

        // We do another sort here by package display name we want to the package id filters to always be sorted by package display name
        includedItems.Sort((a, b) => Comparer.CompareDisplayName(a.sample.package, b.sample.package));
        var supportedPackageUniqueIds = new List<string>(includedItems.SelectAsEnumerable(i => i.sample.packageUniqueId).EnumerateDistinct());
        UpdateSupportedPackages(supportedPackageUniqueIds, true);
    }

    public string GetGroupName(Sample sample)
    {
        var version = sample.package?.versions.primary;
        if (version?.HasTag(PackageTag.Unity) == true)
            return L10n.Tr("Unity");

        var authorName = version?.author?.name;
        return !string.IsNullOrEmpty(authorName) ? authorName : L10n.Tr("Other");
    }

    private class Comparer : IComparer<(Sample sample, VisualState visualState)>
    {
        private readonly PageSortOption m_SortOption;
        private readonly Comparison<string> m_GroupNameComparison;
        public Comparer(PageSortOption sortOption, Comparison<string> compareGroupName)
        {
            m_SortOption = sortOption;
            m_GroupNameComparison = compareGroupName;
        }

        public static int CompareDisplayName(IPackage x, IPackage y)
        {
            if (x == null || y == null)
                return 0;
            return string.Compare(x.displayName, y.displayName, StringComparison.CurrentCultureIgnoreCase);
        }

        private static int CompareDisplayName(Sample x, Sample y)
        {
            if (x.isDefault || y.isDefault)
                return 0;
            return string.Compare(x.displayName, y.displayName, StringComparison.CurrentCultureIgnoreCase);
        }

        private static int ComparePublishDate(IPackage x, IPackage y)
        {
            var xDate = x?.versions?.primary?.publishedDate;
            var yDate = y?.versions?.primary?.publishedDate;

            if (!xDate.HasValue)
                return 1;
            if (!yDate.HasValue)
                return -1;

            return -xDate.Value.CompareTo(yDate.Value);
        }

        private int ComparePackage(IPackage x, IPackage y)
        {
            switch (m_SortOption)
            {
                case PageSortOption.NameDesc:
                    return -CompareDisplayName(x, y);
                case PageSortOption.PublishedDateDesc:
                    return ComparePublishDate(x, y);
                default:
                    return CompareDisplayName(x, y);
            }
        }

        private int CompareSample(Sample x, Sample y)
        {
            switch (m_SortOption)
            {
                case PageSortOption.NameDesc:
                    return -CompareDisplayName(x, y);
                default:
                    return CompareDisplayName(x, y);
            }
        }

        public int Compare((Sample sample, VisualState visualState) x, (Sample sample, VisualState visualState) y)
        {
            if (m_GroupNameComparison != null)
            {
                var groupNameCompareResult = m_GroupNameComparison(x.visualState.groupName, y.visualState.groupName);
                if (groupNameCompareResult != 0)
                    return groupNameCompareResult;
            }

            var packageCompareResult = ComparePackage(x.sample.package, y.sample.package);
            return packageCompareResult != 0 ? packageCompareResult : CompareSample(x.sample, y.sample);
        }
    }
}
