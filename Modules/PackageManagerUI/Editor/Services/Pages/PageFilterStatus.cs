// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal enum PageFilterStatus
    {
        None,
        Unlabeled,
        Downloaded,
        Imported,
        UpdateAvailable,
        Hidden,
        Deprecated,
        SubscriptionBased,
    }

    internal static class PageFilterStatusExtension
    {
        public static string GetDisplayName(this PageFilterStatus value)
        {
            return value switch
            {
                PageFilterStatus.None => L10n.Tr("Any"),
                PageFilterStatus.Unlabeled => L10n.Tr("Unlabeled"),
                PageFilterStatus.Downloaded => L10n.Tr("Downloaded"),
                PageFilterStatus.Imported => L10n.Tr("Imported"),
                PageFilterStatus.UpdateAvailable => L10n.Tr("Update available"),
                PageFilterStatus.Hidden => L10n.Tr("Hidden"),
                PageFilterStatus.Deprecated => L10n.Tr("Deprecated"),
                PageFilterStatus.SubscriptionBased => L10n.Tr("Subscription based"),
                _ => string.Empty
            };
        }
    }
}
