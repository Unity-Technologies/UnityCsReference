// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Localization.Providers;

namespace Unity.Localization.Editor;

[AssetProviderEditor(typeof(ReferencedAssetProvider))]
class ReferencedAssetProviderEditor : AssetProviderEditor
{
    protected override void RegisterTable(IAssetProvider provider, ResourceTableCollection collection, ResourceTable table)
    {
        if (provider is not ReferencedAssetProvider referenced)
            return;
        referenced.RemoveAsset(table);
        referenced.AddOwned(TableAddress(collection, table), table);
        var guidAddress = GuidTableAddress(collection, table);
        if (!string.IsNullOrEmpty(guidAddress))
            referenced.AddOwned(guidAddress, table);
    }

    protected override void UnregisterTable(IAssetProvider provider, ResourceTableCollection collection, ResourceTable table)
    {
        if (provider is not ReferencedAssetProvider referenced)
            return;
        referenced.Remove(TableAddress(collection, table));
        var guidAddress = GuidTableAddress(collection, table);
        if (!string.IsNullOrEmpty(guidAddress))
            referenced.Remove(guidAddress);
    }

    public override void ClearRegistrations(IAssetProvider provider)
    {
        if (provider is ReferencedAssetProvider referenced)
            referenced.ClearOwned();
    }
}
