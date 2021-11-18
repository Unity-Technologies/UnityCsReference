// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class RemoveFoldoutGroup : MultiSelectFoldoutGroup
    {
        public RemoveFoldoutGroup(ApplicationProxy applicationProxy,
                                  PackageManagerPrefs packageManagerPrefs,
                                  PackageDatabase packageDatabase,
                                  PageManager pageManager)
            : base(new PackageRemoveButton(applicationProxy, packageManagerPrefs, packageDatabase, pageManager), null)
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
    }
}
