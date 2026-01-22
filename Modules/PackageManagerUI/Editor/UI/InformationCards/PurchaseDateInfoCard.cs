// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;

namespace UnityEditor.PackageManager.UI.Internal;

internal class PurchaseDateInfoCard : PackageInformationCard
{
    protected override string titleText => L10n.Tr("Purchase Date");
    protected override InformationCardSize cardSize => InformationCardSize.Small;

    public override void Refresh(IPackageVersion version)
    {
        var purchaseDate = version.package.product?.purchasedTime?.ToString("MMMM dd, yyyy", CultureInfo.CreateSpecificCulture("en-US"));
        var isVisible = !string.IsNullOrEmpty(purchaseDate);
        UIUtils.SetElementDisplay(this, isVisible);

        if (!isVisible)
            return;

        contentText = purchaseDate;
    }
}
