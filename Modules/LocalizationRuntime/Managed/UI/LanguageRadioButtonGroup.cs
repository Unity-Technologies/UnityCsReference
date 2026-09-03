// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Localization.UI;

/// <summary>
/// A radio button group that lets the player pick a language.
/// </summary>
/// <remarks>
/// This is the <see cref="LanguageDropdown"/> as a list, for a settings screen that shows every language at once. It
/// fills itself from <see cref="LocalizationSettings.AvailableLocales"/> and writes the chosen locale to
/// <see cref="LocalizationSettings.SelectedLocale"/>, so it needs no setup code. Only the locales whose
/// <see cref="Locale.Enabled"/> is true are offered, and the choices are replaced whenever the locales change, so
/// setting them in UXML has no effect.
/// </remarks>
/// <example>
/// <para>Adds a language radio button group to a panel from code.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/UI/LanguageRadioButtonGroupExample.cs"/>
/// </example>
/// <seealso cref="LanguageDropdown"/>
/// <seealso cref="LocalizationSettings.SelectedLocale"/>
/// <seealso cref="LocalizationSettings.AvailableLocales"/>
[UxmlElement]
public partial class LanguageRadioButtonGroup : RadioButtonGroup
{
    // The control owns its choices, so authoring them would only be overwritten on the first refresh.
    [UxmlAttribute("choices"), HideInInspector]
    internal string choicesOverride { get; set; }

    readonly LocaleSelectionBridge m_Bridge;

    /// <summary>
    /// Creates a language radio button group with no label.
    /// </summary>
#pragma warning disable UAL0015 // LocaleSelectionBridge only (un)subscribes on AttachToPanelEvent/DetachFromPanelEvent, so it re-syncs on the next attach after any reload
    public LanguageRadioButtonGroup() : this(null)
    {
    }

    /// <summary>
    /// Creates a language radio button group with a label above it.
    /// </summary>
    /// <param name="label">The label to display for the group.</param>
    public LanguageRadioButtonGroup(string label) : base(label)
    {
        m_Bridge = new LocaleSelectionBridge(this, Refresh);
        this.RegisterValueChangedCallback(evt => m_Bridge.Select(LocaleAt(evt.newValue)));
    }
#pragma warning restore UAL0015

    Locale LocaleAt(int index)
    {
        var locales = m_Bridge.Locales;
        return index >= 0 && index < locales.Count ? locales[index] : null;
    }

    void Refresh()
    {
        var locales = m_Bridge.Locales;
        var names = new List<string>(locales.Count);
        var selected = LocalizationSettings.SelectedLocale;
        var selectedIndex = -1;
        for (var i = 0; i < locales.Count; i++)
        {
            names.Add(locales[i].LocaleName);
            if (ReferenceEquals(locales[i], selected))
                selectedIndex = i;
        }
        choices = names;
        SetValueWithoutNotify(selectedIndex);
    }
}
