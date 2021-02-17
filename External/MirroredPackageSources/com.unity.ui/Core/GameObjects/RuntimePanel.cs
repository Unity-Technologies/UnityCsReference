namespace UnityEngine.UIElements
{
    //TODO: make IRuntimePanel public when UGUI EventSystem support lands in trunk
    /// <summary>
    /// Interface for classes implementing UI runtime panels.
    /// </summary>
    internal interface IRuntimePanel
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
        static readonly EventDispatcher s_EventDispatcher = RuntimeEventDispatcher.Create();

        private readonly PanelSettings m_PanelSettings;
        public PanelSettings panelSettings => m_PanelSettings;

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
        }

        public override void Update()
        {
            if (m_PanelSettings != null)
                m_PanelSettings.ApplyPanelSettings();

            base.Update();
        }
    }
}
