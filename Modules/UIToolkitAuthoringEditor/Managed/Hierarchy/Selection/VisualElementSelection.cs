// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class VisualElementSelection : UISelectionObject
{
    public static readonly BindingId ElementProperty = nameof(Element);
    public static readonly BindingId EditFlagsProperty = nameof(EditFlags);

    private VisualElement m_Element;

    [CreateProperty]
    public VisualElement Element
    {
        get => m_Element;
        set
        {
            if (m_Element == value)
                return;
            m_Element = value;
            Notify(ElementProperty);
        }
    }

    // NonSerialized prevents Unity's debug inspector from exposing this field as a
    // SerializedProperty and writing back an invalid value (e.g. -1) when the inspector
    // mode is switched.
    [System.NonSerialized]
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
}
