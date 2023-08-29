// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal;

internal interface IAssetSelectionHandler : IService
{
    event Action<IEnumerable<Asset>> onRemoveSelectionDone;

    void Remove(IEnumerable<Asset> assets, string packageName, string versionString);
}

internal class AssetSelectionHandler : BaseService<IAssetSelectionHandler>, IAssetSelectionHandler
{
    public event Action<IEnumerable<Asset>> onRemoveSelectionDone = delegate {};

    private readonly ISelectionWindowProxy m_SelectionWindowProxy;
    public AssetSelectionHandler(ISelectionWindowProxy selectionWindowProxy)
    {
        m_SelectionWindowProxy = RegisterDependency(selectionWindowProxy);
    }

    public override void OnEnable()
    {
        m_SelectionWindowProxy.onRemoveSelectionDone += OnRemoveSelectionDone;
    }

    public override void OnDisable()
    {
        m_SelectionWindowProxy.onRemoveSelectionDone -= OnRemoveSelectionDone;
    }

    public void Remove(IEnumerable<Asset> assets, string packageName, string versionString)
    {
        m_SelectionWindowProxy.Open(new SelectionWindowData(assets, packageName, versionString));
    }

    private void OnRemoveSelectionDone(IEnumerable<Asset> selections)
    {
        onRemoveSelectionDone?.Invoke(selections);
    }
}
