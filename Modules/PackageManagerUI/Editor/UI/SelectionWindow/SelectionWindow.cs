// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Packman not yet converted
using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal;

internal partial class SelectionWindow : EditorWindow
{
    #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
    internal SelectionWindow() {}
    #pragma warning restore UAL0015

    public static void Open(SelectionWindowData data)
    {
        var window = GetWindow<SelectionWindow>(true, data.actionLabel);
        window.SetData(data);
        window.minSize = new Vector2(415, 250);
        window.Show();
    }

    [AutoStaticsCleanupOnCodeReload]
    public static event Action<IReadOnlyCollection<Asset>> onRemoveSelectionDone = delegate {};

    private SelectionWindowRoot m_Root;
    private bool m_SelectionCompleted;

    [SerializeField]
    private SelectionWindowData m_Data;

    public void OnEnable()
    {
        var container = ServicesContainer.instance;
        m_Root = new SelectionWindowRoot(container.Resolve<IResourceLoader>(), container.Resolve<IApplicationProxy>());
        m_Root.onSelectionCompleted += OnSelectionCompleted;
        rootVisualElement.Add(m_Root);

        if (m_Data != null)
            m_Root.SetData(m_Data, false);
    }

    // The internal modifier is used (instead of private) to give our test project access to these properties/methods
    internal void SetData(SelectionWindowData data)
    {
        // We're storing a reference to "data" because we need to serialize it for domain reload.
        m_Data = data;
        m_SelectionCompleted = false;
        m_Root.SetData(m_Data, true);
    }

    public void OnDestroy()
    {
        // This function is called every time the window is closed, whether user clicks confirm, cancel or force quit (i.e. clicking the X button or ALT+F4).
        // In the case when user's force quit the window, m_SelectionCompleted will be false and onRemoveSelectionDone will not have been called yet.
        // This is a special handling to catch that case.
        if (!m_SelectionCompleted)
            onRemoveSelectionDone?.Invoke(Array.Empty<Asset>());
    }

    private void OnSelectionCompleted(IReadOnlyCollection<Asset> selections)
    {
        m_SelectionCompleted = true;
        onRemoveSelectionDone?.Invoke(selections);
        Close();
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
