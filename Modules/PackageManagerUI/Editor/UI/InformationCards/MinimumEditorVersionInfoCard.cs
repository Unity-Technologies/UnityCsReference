// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class MinimumEditorVersionInfoCard : PackageInformationCard
{
    protected override string titleText => L10n.Tr("Minimum Editor Version");
    protected override InformationCardSize cardSize => InformationCardSize.Small;
    public override void Refresh(IPackageVersion version)
    {
        var isVisible = !version.HasTag(PackageTag.Feature | PackageTag.BuiltIn);
        UIUtils.SetElementDisplay(this, isVisible);

        if (!isVisible)
            return;

        var minimumUnityVersion = !string.IsNullOrEmpty(version.minimumUnityVersion) ? version.minimumUnityVersion : L10n.Tr("Not set");
        contentText = minimumUnityVersion;
    }
}
