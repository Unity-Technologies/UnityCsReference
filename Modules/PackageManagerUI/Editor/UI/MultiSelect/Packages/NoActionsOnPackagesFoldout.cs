// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Packman not yet converted
namespace UnityEditor.PackageManager.UI.Internal
{
    internal class NoActionsOnPackagesFoldout : PackageMultiSelectFoldout
    {
        public NoActionsOnPackagesFoldout(IPageManager pageManager)
            : base(new DeselectPackageAction(pageManager, "deselectNoAction"))
        {
            headerTextTemplate = L10n.Tr("No common action available for {0}", null);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
