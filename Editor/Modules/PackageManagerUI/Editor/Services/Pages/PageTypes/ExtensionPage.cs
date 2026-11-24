// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class ExtensionPageArgs
    {
        public string name;
        public string displayName;
        public Icon icon;
        public int priority;
        public RefreshOptions refreshOptions;
        public PageCapability capability;
        public PageFilterStatus[] supportedStatusFilters;
        public PageSortOption[] supportedSortOptions;
        public Func<IPackage, bool> filter;
        public Func<IPackage, string> getGroupName;
        public Func<string, string, int> compareGroup;
        public string id => ExtensionPage.GetIdFromName(name);
    }

    [Serializable]
    internal class ExtensionPage : SimplePageWithPackages
    {
        public const string k_IdPrefix = "Extension";

        public static string GetIdFromName(string name) => $"{k_IdPrefix}/{name}";

        [SerializeField]
        private ExtensionPageArgs m_Args;

        public override string id => GetIdFromName(m_Args.name);
        public override string displayName => m_Args.displayName;
        public override Icon icon => m_Args.icon;
        public override RefreshOptions refreshOptions => m_Args.refreshOptions;

        public override PageCapability capability => m_Args.capability;

        public ExtensionPage(IPackageDatabase packageDatabase, ExtensionPageArgs args) : base(packageDatabase)
        {
            UpdateArgs(args);
        }

        public void UpdateArgs(ExtensionPageArgs args)
        {
            m_Args = args;
            UpdateSupportedSortOptions(args.supportedSortOptions ?? Array.Empty<PageSortOption>(), false);
            UpdateSupportedStatuses(args.supportedStatusFilters ?? Array.Empty<PageFilterStatus>(), false);
        }

        public override bool ShouldInclude(IPackage package)
        {
            return m_Args.filter?.Invoke(package) ?? false;
        }

        public override string GetGroupName(IPackage package)
        {
            return m_Args.getGroupName?.Invoke(package) ?? string.Empty;
        }

        protected override int CompareGroupName(string x, string y) => m_Args?.compareGroup?.Invoke(x, y) ?? base.CompareGroupName(x, y);
    }
}
