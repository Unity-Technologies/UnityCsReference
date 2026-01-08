// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class VisualTreeAssetSelection : UISelectionObject
{
    public static readonly BindingId PanelComponentProperty = nameof(panelComponent);

    private IPanelComponent m_PanelComponent;

    [CreateProperty]
    public IPanelComponent panelComponent
    {
        get => m_PanelComponent;
        set
        {
            if (m_PanelComponent == value)
                return;
            m_PanelComponent = value;
            Notify(PanelComponentProperty);
        }
    }
}
