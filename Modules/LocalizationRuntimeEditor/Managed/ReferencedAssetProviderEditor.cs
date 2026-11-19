// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Localization.Providers;

namespace Unity.Localization.Editor;

[AssetProviderEditor(typeof(ReferencedAssetProvider))]
class ReferencedAssetProviderEditor : AssetProviderEditor
{
    internal override bool TracksChanges => true;

    protected override void RegisterTable(IAssetProvider provider, ResourceTableCollection collection, ResourceTable table)
    {
        if (provider is not ReferencedAssetProvider referenced)
            return;
        Changed |= referenced.SetOwnedAddresses(table, TableAddress(collection, table), GuidTableAddress(collection, table));
    }

    protected override void UnregisterTable(IAssetProvider provider, ResourceTableCollection collection, ResourceTable table)
    {
        if (provider is not ReferencedAssetProvider referenced)
            return;
        Changed |= referenced.Remove(TableAddress(collection, table));
        var guidAddress = GuidTableAddress(collection, table);
        if (!string.IsNullOrEmpty(guidAddress))
            Changed |= referenced.Remove(guidAddress);
    }

    public override void ClearRegistrations(IAssetProvider provider)
    {
        if (provider is ReferencedAssetProvider referenced)
            referenced.ClearOwned();
    }
}
