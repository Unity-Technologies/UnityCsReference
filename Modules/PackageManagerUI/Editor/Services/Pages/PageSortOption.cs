// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
                PageSortOption.NameAsc => L10n.Tr("Name (asc)", null),
                PageSortOption.NameDesc => L10n.Tr("Name (desc)", null),
                PageSortOption.PublishedDateDesc => L10n.Tr("Published date", null),
                PageSortOption.UpdateDateDesc => L10n.Tr("Recently updated", null),
                PageSortOption.PurchasedDateDesc => L10n.Tr("Purchased date", null),
                _ => string.Empty
            };
        }
    }
}
