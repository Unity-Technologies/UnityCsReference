// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal;

internal class AssetSelectionHandler
{
    public event Action<IEnumerable<Asset>> onRemoveSelectionDone = delegate {};

    private SelectionWindowProxy m_SelectionWindowProxy;

    public void ResolveDependencies(SelectionWindowProxy selectionWindowProxy)
    {
        m_SelectionWindowProxy = selectionWindowProxy;
    }

    public void OnEnable()
    {
        m_SelectionWindowProxy.onRemoveSelectionDone += OnRemoveSelectionDone;
    }

    public void OnDisable()
    {
        m_SelectionWindowProxy.onRemoveSelectionDone -= OnRemoveSelectionDone;
    }

    internal virtual void Remove(IEnumerable<Asset> assets, string packageName, string versionString)
    {
        m_SelectionWindowProxy.Open(new SelectionWindowData(assets, packageName, versionString));
    }

    private void OnRemoveSelectionDone(IEnumerable<Asset> selections)
    {
        onRemoveSelectionDone?.Invoke(selections);
    }
}
