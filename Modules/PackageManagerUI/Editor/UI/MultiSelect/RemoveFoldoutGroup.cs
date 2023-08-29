// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class RemoveFoldoutGroup : MultiSelectFoldoutGroup
    {
        public RemoveFoldoutGroup(IApplicationProxy applicationProxy,
                                  IPackageManagerPrefs packageManagerPrefs,
                                  IPackageDatabase packageDatabase,
                                  IPackageOperationDispatcher operationDispatcher,
                                  IPageManager pageManager)
            : base(new RemoveAction(operationDispatcher, applicationProxy, packageManagerPrefs, packageDatabase, pageManager))
        {
        }

        public override void Refresh()
        {
            if (mainFoldout.versions.FirstOrDefault()?.HasTag(PackageTag.BuiltIn) == true)
                mainFoldout.headerTextTemplate = L10n.Tr("Disable {0}");
            else
                mainFoldout.headerTextTemplate = L10n.Tr("Remove {0}");

            if (inProgressFoldout.versions.FirstOrDefault()?.HasTag(PackageTag.BuiltIn) == true)
                inProgressFoldout.headerTextTemplate = L10n.Tr("Disabling {0}");
            else
                inProgressFoldout.headerTextTemplate = L10n.Tr("Removing {0}");

            base.Refresh();
        }

        public override bool AddPackageVersion(IPackageVersion version)
        {
            return version.HasTag(PackageTag.UpmFormat) && base.AddPackageVersion(version);
        }
    }
}
