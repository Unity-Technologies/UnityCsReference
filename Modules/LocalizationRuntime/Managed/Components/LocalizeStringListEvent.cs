// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.Localization.Components;

/// <summary>
/// Component that resolves a localized string list and forwards it through a UnityEvent whenever a value or the selected locale changes.
/// </summary>
/// <remarks>
/// Add this component to a <see cref="GameObject"/>, assign its <see cref="ListReference"/> in the Inspector, then
/// connect <see cref="OnUpdateList"/> to whatever displays the values, for example a dropdown's options. The reference
/// accepts either kind of list: a <see cref="LocalizedStringList"/> splits one entry on a separator, and a
/// <see cref="LocalizedStringGroup"/> combines several entries.
/// </remarks>
/// <example>
/// Forward the resolved values to a dropdown.
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/LocalizeStringListEventBindingExample.cs"/>
/// </example>
/// <seealso cref="ILocalizedStringList"/>
/// <seealso cref="LocalizedStringList"/>
/// <seealso cref="LocalizedStringGroup"/>
[AddComponentMenu("Localization/Localize String List Event")]
public class LocalizeStringListEvent : MonoBehaviour
{
    [SerializeReference] ILocalizedStringList m_List = new LocalizedStringList();
    [SerializeField] UnityEvent<List<string>> m_OnUpdateList = new();

    ILocalizedStringList m_SubscribedList;

    /// <summary>
    /// The localized string list reference to resolve.
    /// </summary>
    public ILocalizedStringList ListReference
    {
        get => m_List;
        set
        {
            Unhook();
            m_List = value;
            Hook();
        }
    }

    /// <summary>
    /// The event invoked with the localized values each time the list resolves.
    /// </summary>
    public UnityEvent<List<string>> OnUpdateList => m_OnUpdateList;

    void OnEnable() => Hook();

    void OnDisable() => Unhook();

    // The Inspector writes m_List behind the property, so a live component must rehook to the new instance.
    void OnValidate()
    {
        if (!Application.isPlaying || ReferenceEquals(m_SubscribedList, m_List))
            return;
        Unhook();
        Hook();
    }

    void Hook()
    {
        if (m_SubscribedList != null || m_List == null || !isActiveAndEnabled)
            return;
        m_List.ListChanged += OnListChanged;
        m_SubscribedList = m_List;
    }

    void Unhook()
    {
        if (m_SubscribedList == null)
            return;
        m_SubscribedList.ListChanged -= OnListChanged;
        m_SubscribedList = null;
    }

    void OnListChanged(List<string> values) => m_OnUpdateList.Invoke(values);
}
