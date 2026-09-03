// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Events;

namespace Unity.Localization.Components;

/// <summary>
/// Component that resolves a localized string and forwards it through a UnityEvent whenever the value or the selected locale changes.
/// </summary>
/// <remarks>
/// Add this component to a <see cref="GameObject"/>, assign its <see cref="StringReference"/> in the Inspector, then connect
/// <see cref="OnUpdateString"/> to a text field so the field updates automatically. While the component is active it subscribes
/// to <see cref="LocalizedString.StringChanged"/>, so it re-resolves and re-invokes the event when
/// <see cref="LocalizationSettings.SelectedLocale"/> changes. Use it for per-object localization without writing subscription
/// code by hand.
/// </remarks>
/// <example>
/// <para>Forward the resolved string to a UI text field.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/LocalizeStringEventBindingExample.cs"/>
/// </example>
/// <seealso cref="LocalizedString"/>
/// <seealso cref="LocalizationSettings"/>
/// <seealso cref="ResourceDatabase"/>
[AddComponentMenu("Localization/Localize String Event")]
public class LocalizeStringEvent : MonoBehaviour
{
    [SerializeField] LocalizedString m_String = new();
    [SerializeField] UnityEvent<string> m_OnUpdateString = new();

    /// <summary>
    /// The localized string reference to resolve.
    /// </summary>
    public LocalizedString StringReference
    {
        get => m_String;
        set
        {
            if (m_String != null)
                m_String.StringChanged -= OnStringChanged;
            m_String = value;
            if (m_String != null && isActiveAndEnabled)
                m_String.StringChanged += OnStringChanged;
        }
    }

    /// <summary>
    /// The event invoked with the localized string each time it resolves.
    /// </summary>
    public UnityEvent<string> OnUpdateString => m_OnUpdateString;

    void OnEnable()
    {
        if (m_String != null)
            m_String.StringChanged += OnStringChanged;
    }

    void OnDisable()
    {
        if (m_String != null)
            m_String.StringChanged -= OnStringChanged;
    }

    void OnStringChanged(string value) => m_OnUpdateString.Invoke(value);
}
