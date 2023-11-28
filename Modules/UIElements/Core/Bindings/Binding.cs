// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Option to tell a binding when to update.
    /// </summary>
    public enum BindingUpdateTrigger
    {
        /// <summary>
        /// Only when <see cref="Binding.MarkDirty"/> has been called.
        /// </summary>
        WhenDirty,
        /// <summary>
        /// Only when a change is detected in the source or <see cref="Binding.MarkDirty"/> has been called.
        /// </summary>
        OnSourceChanged,
        /// <summary>
        /// On every update, regardless of data source changes.
        /// </summary>
        EveryUpdate,
    }

    /// <summary>
    /// Base class for defining a binding.
    /// </summary>
    [UxmlObject]
    public abstract partial class Binding
    {
        /// <summary>
        /// Sets the log level for all binding failures.
        /// </summary>
        /// <remarks>This can be overriden per panel using <see cref="SetPanelLogLevel"/>.</remarks>
        /// <param name="logLevel">The log level.</param>
        public static void SetGlobalLogLevel(BindingLogLevel logLevel)
        {
            DataBindingManager.globalLogLevel = logLevel;
        }

        /// <summary>
        /// Sets the log level for binding failures on a panel.
        /// </summary>
        /// <param name="panel">The panel to apply to.</param>
        /// <param name="logLevel">The log level.</param>
        public static void SetPanelLogLevel(IPanel panel, BindingLogLevel logLevel)
        {
            if (panel is BaseVisualElementPanel elementPanel)
            {
                elementPanel.dataBindingManager.logLevel = logLevel;
            }
        }

        /// <summary>
        /// Resets the log level for binding failures on a panel to use the global setting.
        /// </summary>
        /// <remarks>You can use <see cref="SetGlobalLogLevel"/> to reset the global log level.</remarks>
        /// <param name="panel">The panel to reset the global log level.</param>
        public static void ResetPanelLogLevel(IPanel panel)
        {
            if (panel is BaseVisualElementPanel elementPanel)
            {
                elementPanel.dataBindingManager.ResetLogLevel();
            }
        }

        private bool m_Dirty;
        private BindingUpdateTrigger m_UpdateTrigger;

        internal string property { get; set; }

        /// <summary>
        /// When set to <see langword="true"/>, the binding instance updates during the next update cycle.
        /// When set to <see langword="false"/>, the binding instance updates only if a change is detected.
        /// </summary>
        public bool isDirty => m_Dirty;

        /// <summary>
        /// When set to <see cref="BindingUpdateTrigger.EveryUpdate"/>, the binding instance updates in every update, regardless of the
        /// data source version.
        /// </summary>
        [CreateProperty]
        public BindingUpdateTrigger updateTrigger
        {
            get => m_UpdateTrigger;
            set => m_UpdateTrigger = value;
        }

        internal Binding()
        {
            m_Dirty = true;
        }

        /// <summary>
        /// Notifies the binding system to process this binding.
        /// </summary>
        public void MarkDirty()
        {
            m_Dirty = true;
        }

        internal void ClearDirty()
        {
            m_Dirty = false;
        }

        /// <summary>
        /// Called when the binding becomes active for a specific <see cref="VisualElement"/>.
        /// </summary>
        /// <param name="context">Context object.</param>
        protected internal virtual void OnActivated(in BindingActivationContext context)
        {
        }

        /// <summary>
        /// Called when the binding is no longer active for a specific <see cref="VisualElement"/>.
        /// </summary>
        /// <param name="context">Context object.</param>
        protected internal virtual void OnDeactivated(in BindingActivationContext context)
        {
        }

        /// <summary>
        /// Called when the resolved data source of a binding changes.
        /// </summary>
        /// <param name="context">Context object.</param>
        protected internal virtual void OnDataSourceChanged(in DataSourceContextChanged context)
        {
        }
    }
}
