// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class VisualTreeAssetSelection : UISelectionObject
{
    public static readonly BindingId DocumentProperty = nameof(Document);

    private UIDocument m_Document;

    [CreateProperty]
    public UIDocument Document
    {
        get => m_Document;
        set
        {
            if (m_Document == value)
                return;
            m_Document = value;
            Notify(DocumentProperty);
        }
    }
}
