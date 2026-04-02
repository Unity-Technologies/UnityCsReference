// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class UnlockFoldout : PackageMultiSelectFoldout
    {
        public UnlockFoldout(IPageManager pageManager) : base(new UnlockAction(pageManager))
        {
            headerTextTemplate = L10n.Tr("Unlock {0}");
        }

        public override bool AddItem(IPackage package)
        {
            if (!action.GetActionState(package?.versions.primary, out _, out _).HasFlag(ActionState.Visible))
                return false;
            return base.AddItem(package);
        }
    }
}
