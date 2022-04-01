// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    public abstract class ToolbarOverlay : Overlay, ICreateToolbar
    {
        readonly string[] m_ToolbarElementIds;
        public IEnumerable<string> toolbarElements => m_ToolbarElementIds;

        protected ToolbarOverlay(params string[] toolbarElementIds)
        {
            m_ToolbarElementIds = toolbarElementIds ?? Array.Empty<string>();
        }

        [Obsolete("Use Overlay.CreateContent(Layout.Horizontal)")]
        public VisualElement CreateHorizontalToolbarContent() => CreatePanelContent();

        [Obsolete("Use Overlay.CreateContent(Layout.Vertical)")]
        public VisualElement CreateVerticalToolbarContent() => CreatePanelContent();

        public override VisualElement CreatePanelContent()
        {
            return new EditorToolbar(toolbarElements, containerWindow).rootVisualElement;
        }
    }
}
