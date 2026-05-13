// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    /// <summary>
    /// Editor-only contract: size and UI Toolkit scaling for a play mode (game) view on a logical display.
    /// Implementation lives in UnityEditor; runtime modules consume via GameViewRenderInfoQuery.
    /// </summary>
    [VisibleToOtherModules("UnityEditor", "UnityEngine.UIElementsModule", "UnityEngine.InputForUIModule")]
    internal interface IGameViewRenderInfo
    {
        Vector2 targetSize { get; }
        int targetDisplay { get; }
        /// <summary>Effective pixels-per-point for this view (editor: <c>GUIView</c> backing scale, or <see cref="Screen.dpi"/>/96 fallback).</summary>
        float scaling { get; }

        /// <summary>
        /// Effective DPI for <see cref="UnityEngine.UIElements.PanelScaleMode.ConstantPhysicalSize"/> scaling in the editor
        /// (comparable to <see cref="Screen.dpi"/> on players). Default <c>PlayModeView</c> uses 96 × GUIView backing scale, then <see cref="Screen.dpi"/>; the Device Simulator overrides with simulated <see cref="Screen.dpi"/>.
        /// </summary>
        float dpi { get; }
    }

    /// <summary>
    /// Holds the editor implementation for <see cref="IGameViewRenderInfo"/> lookup by display index.
    /// </summary>
    [VisibleToOtherModules("UnityEditor", "UnityEngine.UIElementsModule", "UnityEngine.InputForUIModule")]
    internal static class GameViewRenderInfoQuery
    {
        [VisibleToOtherModules("UnityEditor", "UnityEngine.UIElementsModule", "UnityEngine.InputForUIModule")]
        internal static Func<int, IGameViewRenderInfo> getImplementation;
    }
}
