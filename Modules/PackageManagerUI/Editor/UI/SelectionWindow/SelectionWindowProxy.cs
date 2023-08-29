// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.PackageManager.UI.Internal;

internal interface ISelectionWindowProxy : IService
{
    event Action<IEnumerable<Asset>> onRemoveSelectionDone;
    void Open(SelectionWindowData data);
}

[ExcludeFromCodeCoverage]
internal class SelectionWindowProxy : BaseService<ISelectionWindowProxy>, ISelectionWindowProxy
{
    public event Action<IEnumerable<Asset>> onRemoveSelectionDone = delegate {};

    public void Open(SelectionWindowData data)
    {
        SelectionWindow.Open(data);
    }

    public override void OnEnable()
    {
        SelectionWindow.onRemoveSelectionDone += OnRemoveSelectionDone;
    }

    public override void OnDisable()
    {
        SelectionWindow.onRemoveSelectionDone -= OnRemoveSelectionDone;
    }

    private void OnRemoveSelectionDone(IEnumerable<Asset> selections)
    {
        onRemoveSelectionDone?.Invoke(selections);
    }
}
