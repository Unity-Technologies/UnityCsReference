// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for the panel toolbar toggles.
    /// </summary>
    abstract class PanelToggle : EditorToolbarToggle, IAccessContainerWindow
    {
        Overlay m_OverlayWindow;

        /// <inheritdoc />
        public EditorWindow containerWindow { get; set; }

        /// <summary>
        /// The id of the window controlled by this button.
        /// </summary>
        protected abstract string WindowId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PanelToggle"/> class.
        /// </summary>
        protected PanelToggle()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            RegisterCallback<ChangeEvent<bool>>(OnValueChanged);
        }

        /// <summary>
        /// Event handler for <see cref="AttachToPanelEvent"/>.
        /// </summary>
        /// <param name="evt">The event to handle.</param>
        protected void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            if (containerWindow.TryGetOverlay(WindowId, out m_OverlayWindow))
            {
                m_OverlayWindow.displayedChanged += OnWindowDisplayedChanged;
                OnWindowDisplayedChanged(m_OverlayWindow.displayed);
            }
        }

        /// <summary>
        /// Event handler for <see cref="DetachFromPanelEvent"/>.
        /// </summary>
        /// <param name="evt">The event to handle.</param>
        protected void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (m_OverlayWindow != null)
                m_OverlayWindow.displayedChanged -= OnWindowDisplayedChanged;
        }

        /// <summary>
        /// Callbacks for the <see cref="ChangeEvent{T}"/> on the toggle.
        /// </summary>
        /// <param name="e"></param>
        protected void OnValueChanged(ChangeEvent<bool> e)
        {
            if (m_OverlayWindow != null && m_OverlayWindow.displayed != e.newValue)
            {
                m_OverlayWindow.displayed = e.newValue;
            }
        }

        void OnWindowDisplayedChanged(bool displayed)
        {
            SetValueWithoutNotify(displayed);
        }
    }
}
