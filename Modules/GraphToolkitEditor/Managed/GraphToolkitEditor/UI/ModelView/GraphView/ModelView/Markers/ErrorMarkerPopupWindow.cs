// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor;

class ErrorMarkerPopupWindow : PopupWindowContent
{
    static readonly Vector2 k_DefaultSize = new(600, 200);

    ErrorMarkerPopupModel m_Model;
    GraphView m_GraphView;

    public void Show(Rect position,
        List<ErrorMarkerModel> currentGraphErrors,
        List<ErrorMarkerPopupModel.SubgraphErrorGroup> subgraphErrors,
        GraphView window)
    {
        m_Model = new ErrorMarkerPopupModel(currentGraphErrors, subgraphErrors);
        m_GraphView = window;
        UnityEditor.PopupWindow.Show(position, this);
    }

    public override Vector2 GetWindowSize() => k_DefaultSize;

    public override VisualElement CreateGUI()
    {
        var content = new ErrorMarkerPopupContent(m_Model, m_GraphView);
        content.CloseRequest += Close;
        content.RegisterCallback<AttachToPanelEvent>(ContentAttachedToPanel);
        content.RegisterCallback<DetachFromPanelEvent>(ContentDetachedFromPanel);
        return content;
    }

    void ContentDetachedFromPanel(DetachFromPanelEvent _)
    {
        editorWindow.rootVisualElement.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
    }

    void ContentAttachedToPanel(AttachToPanelEvent _)
    {
        var root = editorWindow.rootVisualElement;
        root.focusable = true;
        root.Focus();
        root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
    }

    void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Escape)
        {
            evt.StopImmediatePropagation();
            Close();
        }
    }

    void Close() => editorWindow.Close();
}
