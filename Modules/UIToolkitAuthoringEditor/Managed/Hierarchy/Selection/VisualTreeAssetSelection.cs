// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class VisualTreeAssetSelection : UISelectionObject
{
    public static readonly BindingId PanelComponentProperty = nameof(panelComponent);

    // No manual page exists for this type, so hide the help button.
    static VisualTreeAssetSelection() => Help.RegisterHelpFileName(typeof(VisualTreeAssetSelection), "");

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
