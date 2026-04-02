// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class OpenManifestExternallyFoldoutGroup : PackageMultiSelectFoldoutGroup
{
    public OpenManifestExternallyFoldoutGroup() : base(new OpenManifestExternallyAction())
    {
    }

    public override void Refresh()
    {
        mainFoldout.headerTextTemplate = L10n.Tr("Open {0}");
        base.Refresh();
    }
}
