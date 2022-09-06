// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class UpdateFoldoutGroup : MultiSelectFoldoutGroup
    {
        public UpdateFoldoutGroup(ApplicationProxy applicationProxy,
                                  PackageDatabase packageDatabase,
                                  PackageOperationDispatcher operationDispatcher,
                                  PageManager pageManager)
            : base(new UpdateFoldout(applicationProxy, packageDatabase, operationDispatcher, pageManager), new MultiSelectFoldout())
        {
            mainFoldout.headerTextTemplate = L10n.Tr("Update {0}");
            inProgressFoldout.headerTextTemplate = L10n.Tr("Updating {0}...");
        }

        public override bool AddPackageVersion(IPackageVersion version)
        {
            if (!version.package.Is(PackageType.Upm))
                return false;
            return base.AddPackageVersion(version);
        }
    }
}
