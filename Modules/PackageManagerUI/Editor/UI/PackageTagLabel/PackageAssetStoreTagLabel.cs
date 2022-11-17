// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageAssetStoreTagLabel : PackageBaseTagLabel
    {
        public PackageAssetStoreTagLabel()
        {
            name = "tagAssetStore";
            text = L10n.Tr("asset store");
        }

        public override void Refresh(IPackageVersion version)
        {
            UIUtils.SetElementDisplay(this, version.package.product != null);
        }
    }
}
