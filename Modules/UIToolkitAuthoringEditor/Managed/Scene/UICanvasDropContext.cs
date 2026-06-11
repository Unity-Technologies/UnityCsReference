// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class UICanvasDropContext : ICanvasDropContext
{
    readonly UICanvas m_Canvas;

    public UICanvasDropContext(UICanvas canvas) => m_Canvas = canvas;

    public VisualElement OverlayLayer => m_Canvas.DocumentRoot.OverlayLayer;

    public Vector2 WorldToContentPosition(Vector2 worldPosition) => m_Canvas.WorldToSubPanel(worldPosition);

    public void PickAll(Vector2 worldPosition, List<VisualElement> results) => m_Canvas.PickAll(worldPosition, results);
}
