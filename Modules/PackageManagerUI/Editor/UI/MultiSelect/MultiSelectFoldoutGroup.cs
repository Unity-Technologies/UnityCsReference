// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class MultiSelectFoldoutGroup : IMultiSelectFoldoutElement
    {
        public MultiSelectFoldout mainFoldout { get; }
        public MultiSelectFoldout inProgressFoldout { get; }

        public PackageAction mainAction => mainFoldout.action;
        public PackageAction cancelAction => inProgressFoldout.action;

        public MultiSelectFoldoutGroup(PackageAction mainAction, PackageAction cancelAction = null)
            : this(new MultiSelectFoldout(mainAction), new MultiSelectFoldout(cancelAction))
        {
        }

        public MultiSelectFoldoutGroup(MultiSelectFoldout mainFoldout, MultiSelectFoldout inProgressFoldout)
        {
            this.mainFoldout = mainFoldout;
            this.inProgressFoldout = inProgressFoldout;
        }

        public virtual bool AddPackageVersion(IPackageVersion version)
        {
            var state = mainAction.GetActionState(version, out _, out _);
            if (state.HasFlag(PackageActionState.InProgress))
                inProgressFoldout.AddPackageVersion(version);
            else if (state == PackageActionState.Visible || state.HasFlag(PackageActionState.DisabledTemporarily))
                mainFoldout.AddPackageVersion(version);
            else
                return false;
            return true;
        }

        public virtual void Refresh()
        {
            mainFoldout.Refresh();
            inProgressFoldout.Refresh();
        }

        public void ClearVersions()
        {
            mainFoldout.ClearVersions();
            inProgressFoldout.ClearVersions();
        }
    }
}
