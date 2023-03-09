// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class ExtensionPageArgs
    {
        public string name;
        public string displayName;
        public int priority;
        public RefreshOptions refreshOptions;
        public PageCapability capability;
        public PageFilters.Status[] supportedStatusFilters;
        public PageSortOption[] supportedSortOptions;
        public Func<IPackage, bool> filter;
        public Func<IPackage, string> getGroupName;
        public Func<string, string, int> compareGroup;
        public string id => ExtensionPage.GetIdFromName(name);
    }

    [Serializable]
    internal class ExtensionPage : SimplePage
    {
        public const string k_IdPrefix = "Extension";

        public static string GetIdFromName(string name) => $"{k_IdPrefix}/{name}";

        [SerializeField]
        private ExtensionPageArgs m_Args;

        public override string id => GetIdFromName(m_Args.name);
        public override string displayName => m_Args.displayName;
        public override RefreshOptions refreshOptions => m_Args.refreshOptions;

        public override IEnumerable<PageFilters.Status> supportedStatusFilters => m_Args.supportedStatusFilters ?? Enumerable.Empty<PageFilters.Status>();
        public override IEnumerable<PageSortOption> supportedSortOptions => m_Args.supportedSortOptions ?? Enumerable.Empty<PageSortOption>();
        public override PageCapability capability => m_Args.capability;

        public ExtensionPage(PackageDatabase packageDatabase, ExtensionPageArgs args)
            : base(packageDatabase)
        {
            UpdateArgs(args);
        }

        public void UpdateArgs(ExtensionPageArgs args)
        {
            m_Args = args;
        }

        public override bool ShouldInclude(IPackage package)
        {
            return m_Args.filter?.Invoke(package) ?? false;
        }

        public override string GetGroupName(IPackage package)
        {
            return m_Args.getGroupName?.Invoke(package) ?? string.Empty;
        }

        protected override void SortGroupNames(List<string> groupNames)
        {
            if (m_Args.compareGroup != null)
                groupNames.Sort((x, y) => m_Args.compareGroup.Invoke(x, y));
            else
                base.SortGroupNames(groupNames);
        }
    }
}
