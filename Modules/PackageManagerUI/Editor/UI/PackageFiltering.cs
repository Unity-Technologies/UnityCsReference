// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal static class PackageFiltering
    {
        private static bool FilterByText(PackageInfo info, string text)
        {
            if (info == null)
                return false;

            if (info.Name.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            if (!string.IsNullOrEmpty(info.DisplayName) && info.DisplayName.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            if (!info.IsBuiltIn)
            {
                var prerelease = text.StartsWith("-") ? text.Substring(1) : text;
                if (info.Version != null && info.Version.Prerelease.IndexOf(prerelease, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    return true;

                if (info.VersionWithoutTag.StartsWith(text, StringComparison.CurrentCultureIgnoreCase))
                    return true;

                if (info.IsPreview)
                {
                    if (PackageTag.preview.ToString().IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                        return true;
                }

                if (info.IsVerified)
                {
                    if (PackageTag.verified.ToString().IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                        return true;
                }

                if (info.IsCore)
                {
                    if (PackageTag.builtin.ToString().IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                        return true;
                }
            }

            return false;
        }

        internal static bool FilterByText(Package package, string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            var trimText = text.Trim(' ', '\t');
            trimText = Regex.Replace(trimText, @"[ ]{2,}", " ");
            return string.IsNullOrEmpty(trimText) || FilterByText(package.Current ?? package.Latest, trimText);
        }

        private static bool FilterByText(PackageItem item, string text)
        {
            return item.package != null && FilterByText(item.package, text);
        }

        public static void FilterPackageList(PackageList packageList)
        {
            PackageItem firstItem = null;
            var selectedItemInFilter = false;
            var selectedItem = packageList.SelectedItem;
            var packageItems = packageList.Query<PackageItem>().ToList();
            foreach (var item in packageItems)
            {
                if (FilterByText(item, packageList.searchFilter.SearchText))
                {
                    if (firstItem == null)
                        firstItem = item;
                    if (item == selectedItem)
                        selectedItemInFilter = true;

                    UIUtils.SetElementDisplay(item, true);
                }
                else
                    UIUtils.SetElementDisplay(item, false);
            }

            if (packageItems.Any())
                if (firstItem == null)
                    packageList.ShowNoResults();
                else
                    packageList.ShowResults(selectedItemInFilter ? selectedItem : firstItem);
        }
    }
}
