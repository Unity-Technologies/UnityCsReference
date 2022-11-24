// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.PackageManager.UI.Internal;

[ExcludeFromCodeCoverage]
internal class SelectionWindowProxy
{
    public event Action<IEnumerable<Asset>> onRemoveSelectionDone = delegate {};

    public virtual void Open(SelectionWindowData data)
    {
        SelectionWindow.Open(data);
    }

    public void OnEnable()
    {
        SelectionWindow.onRemoveSelectionDone += OnRemoveSelectionDone;
    }

    public void OnDisable()
    {
        SelectionWindow.onRemoveSelectionDone -= OnRemoveSelectionDone;
    }

    private void OnRemoveSelectionDone(IEnumerable<Asset> selections)
    {
        onRemoveSelectionDone?.Invoke(selections);
    }
}
