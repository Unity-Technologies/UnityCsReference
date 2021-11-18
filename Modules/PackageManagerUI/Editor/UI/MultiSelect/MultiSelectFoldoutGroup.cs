// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class MultiSelectFoldoutGroup : IMultiSelectFoldoutElement
    {
        public MultiSelectFoldout mainFoldout { get; private set; }
        public MultiSelectFoldout inProgressFoldout { get; private set; }

        public PackageToolBarRegularButton mainButton => mainFoldout.button;
        public PackageToolBarRegularButton cancelButton => inProgressFoldout.button;

        public MultiSelectFoldoutGroup(PackageToolBarRegularButton mainButton, PackageToolBarRegularButton cancelButton = null)
            : this(new MultiSelectFoldout(mainButton), new MultiSelectFoldout(cancelButton))
        {
        }

        public MultiSelectFoldoutGroup(MultiSelectFoldout mainFoldout, MultiSelectFoldout inProgressFoldout)
        {
            this.mainFoldout = mainFoldout;
            this.inProgressFoldout = inProgressFoldout;
        }

        public bool AddPackageVersion(IPackageVersion version)
        {
            var state = mainButton.GetActionState(version, out _, out _);
            if (state.HasFlag(PackageActionState.InProgress))
                inProgressFoldout.AddPackageVersion(version);
            else if (state == PackageActionState.Visible || state.HasFlag(PackageActionState.DisabledGlobally))
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
