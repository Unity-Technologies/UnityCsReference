// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageMultiSelectFoldoutGroup : IMultiSelectFoldoutElement<IPackage>
    {
        public PackageMultiSelectFoldout mainFoldout { get; }
        public PackageMultiSelectFoldout inProgressFoldout { get; }

        public PackageAction mainAction => (PackageAction) mainFoldout.action;
        public PackageAction cancelAction => (PackageAction) inProgressFoldout.action;

        public PackageMultiSelectFoldoutGroup(PackageAction mainAction, PackageAction cancelAction = null)
            : this(new PackageMultiSelectFoldout(mainAction), new PackageMultiSelectFoldout(cancelAction))
        {
        }

        public PackageMultiSelectFoldoutGroup(PackageMultiSelectFoldout mainFoldout, PackageMultiSelectFoldout inProgressFoldout)
        {
            this.mainFoldout = mainFoldout;
            this.inProgressFoldout = inProgressFoldout;

            if (mainFoldout.action != null)
                mainFoldout.action.onActionTriggered += () =>
                {
                    if (mainFoldout.isExpanded && !inProgressFoldout.isExpanded)
                        inProgressFoldout.SetExpanded(true);
                };
        }

        public virtual bool AddItem(IPackage package)
        {
            var state = mainAction.GetActionState(package.versions.primary, out _, out _);
            if (state.HasFlag(ActionState.InProgress))
                inProgressFoldout.AddItem(package);
            else if (state == ActionState.Visible || state.HasFlag(ActionState.DisabledTemporarily))
                mainFoldout.AddItem(package);
            else
                return false;
            return true;
        }

        public virtual void Refresh()
        {
            mainFoldout.Refresh();
            inProgressFoldout.Refresh();
        }

        public void ClearItems()
        {
            mainFoldout.ClearItems();
            inProgressFoldout.ClearItems();
        }
    }
}
