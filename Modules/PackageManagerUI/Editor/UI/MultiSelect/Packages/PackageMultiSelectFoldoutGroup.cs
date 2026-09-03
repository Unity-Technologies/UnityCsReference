// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Packman not yet converted
namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageMultiSelectFoldoutGroup : MultiSelectFoldoutGroupBase<IPackageVersion, IPackage>
    {
        public PackageMultiSelectFoldoutGroup(PackageAction mainAction, PackageAction cancelAction = null)
            : base(new PackageMultiSelectFoldout(mainAction), new PackageMultiSelectFoldout(cancelAction))
        {
        }

        public PackageMultiSelectFoldoutGroup(PackageMultiSelectFoldout main, PackageMultiSelectFoldout cancel) : base(main, cancel)
        {
        }

        protected override ActionState GetActionState(IPackage item)
        {
            return mainAction.GetActionState(item.versions.primary, out _, out _);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
