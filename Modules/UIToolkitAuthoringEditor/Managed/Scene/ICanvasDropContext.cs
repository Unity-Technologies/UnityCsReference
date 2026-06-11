// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

interface ICanvasDropContext
{
    VisualElement OverlayLayer { get; }

    Vector2 WorldToContentPosition(Vector2 worldPosition);
    void PickAll(Vector2 worldPosition, List<VisualElement> results);
}
