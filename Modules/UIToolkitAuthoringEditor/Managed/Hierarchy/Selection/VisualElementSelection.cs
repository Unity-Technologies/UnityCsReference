// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class VisualElementSelection : UISelectionObject
{
    public static readonly BindingId ElementProperty = nameof(Element);
    public static readonly BindingId EditFlagsProperty = nameof(EditFlags);

    private VisualElement m_Element;
    EventCallback<ReleasingResourcesEvent> m_OnElementReleasing;

    [CreateProperty]
    public VisualElement Element
    {
        get => m_Element;
        set
        {
            if (m_Element == value)
                return;
            if (value is { resourcesReleased: true })
                throw new ArgumentException("A selection can't point to a released VisualElement.", nameof(value));

            m_OnElementReleasing ??= OnElementReleasing;
            m_Element?.UnregisterCallback(m_OnElementReleasing);
            m_Element = value;
            m_Element?.RegisterCallback(m_OnElementReleasing);
            Notify(ElementProperty);
        }
    }

    // NonSerialized prevents Unity's debug inspector from exposing this field as a
    // SerializedProperty and writing back an invalid value (e.g. -1) when the inspector
    // mode is switched.
    [NonSerialized]
    private VisualElementEditFlags m_EditFlags;

    [CreateProperty]
    public VisualElementEditFlags EditFlags
    {
        get => m_EditFlags;
        set
        {
            if (m_EditFlags == value)
                return;
            m_EditFlags = value;
            Notify(EditFlagsProperty);
        }
    }

    void OnDestroy()
    {
        if (m_OnElementReleasing != null)
            m_Element?.UnregisterCallback(m_OnElementReleasing);
    }

    // The registry keeps this selection object alive so the re-cloned element can reclaim it on the next panel update.
    void OnElementReleasing(ReleasingResourcesEvent evt) => Element = null;
}
