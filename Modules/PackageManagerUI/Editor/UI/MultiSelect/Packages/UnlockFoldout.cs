// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Packman not yet converted
namespace UnityEditor.PackageManager.UI.Internal
{
    internal class UnlockFoldout : PackageMultiSelectFoldout
    {
        public UnlockFoldout(IPageManager pageManager) : base(new UnlockAction(pageManager))
        {
            headerTextTemplate = L10n.Tr("Unlock {0}", null);
        }

        public override bool AddItem(IPackage package)
        {
            if (!action.GetActionState(package?.versions.primary, out _, out _).HasFlag(ActionState.Visible))
                return false;
            return base.AddItem(package);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
