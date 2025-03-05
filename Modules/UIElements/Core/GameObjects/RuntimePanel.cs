// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for classes implementing UI runtime panels.
    /// </summary>
    public interface IRuntimePanel : IPanel
    {
        /// <summary>
        /// The <see cref="UnityEngine.UIElements.PanelSettings"/> asset associated with this panel.
        /// </summary>
        PanelSettings panelSettings { get; }

        /// <summary>
        /// A GameObject from the Scene that can be used by <see cref="UnityEngine.EventSystems.EventSystem"/>
        /// to get and set focus to this panel. If null, panel focus will be handled independently of
        /// Event System selection.
        /// </summary>
        GameObject selectableGameObject { get; set; }
    }

    internal class RuntimePanel : BaseRuntimePanel, IRuntimePanel
    {
        internal static readonly EventDispatcher s_EventDispatcher = RuntimeEventDispatcher.Create();

        private readonly PanelSettings m_PanelSettings;
        public PanelSettings panelSettings => m_PanelSettings;

        private static readonly List<UIDocument> s_EmptyDocumentList = new();

        internal List<UIDocument> documents =>
            m_PanelSettings.m_AttachedUIDocumentsList?.m_AttachedUIDocuments ?? s_EmptyDocumentList;

        public static RuntimePanel Create(ScriptableObject ownerObject)
        {
            return new RuntimePanel(ownerObject);
        }

        private RuntimePanel(ScriptableObject ownerObject)
            : base(ownerObject, s_EventDispatcher)
        {
            focusController = new FocusController(new NavigateFocusRing(visualTree));
            m_PanelSettings  = ownerObject as PanelSettings;
            name = m_PanelSettings != null ? m_PanelSettings.name : "RuntimePanel";

            visualTree.RegisterCallback<FocusEvent, RuntimePanel>((e, p) => p.OnElementFocus(e), this,
                TrickleDown.TrickleDown);
        }

        internal override void Update()
        {
            if (m_PanelSettings != null)
                m_PanelSettings.ApplyPanelSettings();

            base.Update();
        }

        private void OnElementFocus(FocusEvent evt)
        {
            UIElementsRuntimeUtility.defaultEventSystem.OnFocusEvent(this, evt);
        }
    }
}
