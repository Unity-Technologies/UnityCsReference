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
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (mainFoldout.packages.FirstOrDefault()?.versions.primary.HasTag(PackageTag.BuiltIn) == true)
#pragma warning restore RS0030
                mainFoldout.headerTextTemplate = L10n.Tr("Disable {0}");
            else
                mainFoldout.headerTextTemplate = L10n.Tr("Remove {0}");

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (inProgressFoldout.packages.FirstOrDefault()?.versions.primary.HasTag(PackageTag.BuiltIn) == true)
#pragma warning restore RS0030
                inProgressFoldout.headerTextTemplate = L10n.Tr("Disabling {0}");
            else
                inProgressFoldout.headerTextTemplate = L10n.Tr("Removing {0}");

            base.Refresh();
        }

        public override bool AddPackage(IPackage package)
        {
            return package.versions.primary.HasTag(PackageTag.UpmFormat) && base.AddPackage(package);
        }
    }
}
