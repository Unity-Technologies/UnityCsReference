// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class VisualElementSelection : UISelectionObject
{
    public static readonly BindingId ElementProperty = nameof(Element);

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
}
