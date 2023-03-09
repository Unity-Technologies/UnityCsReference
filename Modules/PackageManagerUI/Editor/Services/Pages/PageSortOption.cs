// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal enum PageSortOption
    {
        NameAsc,
        NameDesc,
        PublishedDateDesc,
        UpdateDateDesc,
        PurchasedDateDesc,
    }

    internal static class PageSortOptionExtension
    {
        public static string GetDisplayName(this PageSortOption value)
        {
            return value switch
            {
                PageSortOption.NameAsc => L10n.Tr("Name (asc)"),
                PageSortOption.NameDesc => L10n.Tr("Name (desc)"),
                PageSortOption.PublishedDateDesc => L10n.Tr("Published date"),
                PageSortOption.UpdateDateDesc => L10n.Tr("Recently updated"),
                PageSortOption.PurchasedDateDesc => L10n.Tr("Purchased date"),
                _ => string.Empty
            };
        }
    }
}
