// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Localization.UI;

/// <summary>
/// A dropdown that lets the player pick a language.
/// </summary>
/// <remarks>
/// The control fills itself from <see cref="LocalizationSettings.AvailableLocales"/> and writes the chosen locale to
/// <see cref="LocalizationSettings.SelectedLocale"/>, so it needs no setup code. It follows a change made anywhere
/// else, and it refreshes when localization finishes initializing, which is when locales discovered from data files
/// appear. Only the locales whose <see cref="Locale.Enabled"/> is true are offered.
/// </remarks>
/// <example>
/// <para>Adds a language dropdown to a panel from code.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/UI/LanguageDropdownExample.cs"/>
/// </example>
/// <seealso cref="LanguageRadioButtonGroup"/>
/// <seealso cref="LocalizationSettings.SelectedLocale"/>
/// <seealso cref="LocalizationSettings.AvailableLocales"/>
[UxmlElement]
public partial class LanguageDropdown : PopupField<Locale>
{
    // The inherited value attribute is a Locale, which cannot be written in UXML, and the control takes its value from
    // the settings regardless. Concealed the way DropdownField conceals its own.
    [UxmlAttribute("value"), HideInInspector]
    internal string valueOverride { get; set; }

    readonly LocaleSelectionBridge m_Bridge;

    /// <summary>
    /// Creates a language dropdown with no label.
    /// </summary>
#pragma warning disable UAL0015 // LocaleSelectionBridge only (un)subscribes on AttachToPanelEvent/DetachFromPanelEvent, so it re-syncs on the next attach after any reload
    public LanguageDropdown() : this(null)
    {
    }

    /// <summary>
    /// Creates a language dropdown with a label in front of it.
    /// </summary>
    /// <param name="label">The label to display in front of the dropdown.</param>
    public LanguageDropdown(string label) : base(label)
    {
        formatListItemCallback = LocaleName;
        formatSelectedValueCallback = LocaleName;
        m_Bridge = new LocaleSelectionBridge(this, Refresh);
        this.RegisterValueChangedCallback(evt => m_Bridge.Select(evt.newValue));
    }
#pragma warning restore UAL0015

    static string LocaleName(Locale locale) => locale != null ? locale.LocaleName : string.Empty;

    void Refresh()
    {
        var locales = m_Bridge.Locales;
        var updated = new List<Locale>(locales.Count);
        for (var i = 0; i < locales.Count; i++)
            updated.Add(locales[i]);
        choices = updated;
        SetValueWithoutNotify(LocalizationSettings.SelectedLocale);
    }
}
