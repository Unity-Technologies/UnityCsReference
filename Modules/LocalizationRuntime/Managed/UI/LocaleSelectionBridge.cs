// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.Localization.UI;

// Shared by the language controls. SelectedLocaleChanged is static and outlives any panel, so a control that only
// subscribes keeps itself and everything it references alive for the rest of the session.
class LocaleSelectionBridge
{
    readonly Action m_Refresh;
    readonly List<Locale> m_Locales = new();
    bool m_Connected;
    bool m_Applying;

    public LocaleSelectionBridge(VisualElement owner, Action refresh)
    {
        m_Refresh = refresh;
#pragma warning disable UAL0015 // Connect/Disconnect subscribe to the static LocalizationSettings events symmetrically on attach/detach, so a control re-syncs on its next attach after any reload
        owner.RegisterCallback<AttachToPanelEvent>(_ => Connect());
        owner.RegisterCallback<DetachFromPanelEvent>(_ => Disconnect());
#pragma warning restore UAL0015
    }

    // The locales the control last drew, so an index a control reports still means the locale the player clicked.
    public IReadOnlyList<Locale> Locales => m_Locales;

    public void Select(Locale locale)
    {
        // The refresh below writes the control's value, and a control that notifies on that write would land back here.
        if (!m_Applying)
            LocalizationSettings.SelectedLocale = locale;
    }

    void Connect()
    {
        if (m_Connected)
            return;
        m_Connected = true;
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        LocalizationSettings.InitializationCompleted += Apply;
        Apply();
    }

    void Disconnect()
    {
        if (!m_Connected)
            return;
        m_Connected = false;
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        LocalizationSettings.InitializationCompleted -= Apply;
    }

    void OnSelectedLocaleChanged(Locale _) => Apply();

    void Apply()
    {
        CollectLocales();
        m_Applying = true;
        try
        {
            m_Refresh();
        }
        finally
        {
            m_Applying = false;
        }
    }

    void CollectLocales()
    {
        m_Locales.Clear();
        var settings = LocalizationSettings.Instance;
        if (settings == null)
            return;
        var available = settings.AvailableLocales;
        for (var i = 0; i < available.Count; i++)
        {
            // A disabled locale is not offered to a player, and a locale that failed to deserialize leaves a null.
            if (available[i] != null && available[i].Enabled)
                m_Locales.Add(available[i]);
        }
    }
}
