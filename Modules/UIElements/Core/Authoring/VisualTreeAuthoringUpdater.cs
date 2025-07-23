// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Implement this interface to periodically receive notifications about changes that occurred in the frame.
    /// </summary>
    /// <remarks>
    /// These notifications are batched and sent after the other Visual Tree updaters have run. These changes
    /// are unordered.
    ///
    /// If no changes occured in the frame, the notifications are not sent.
    /// </remarks>
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal interface IVisualElementChangeProcessor
    {
        /// <summary>
        /// Called when the change processor is registered.
        /// </summary>
        /// <param name="panel">The panel the change processor is registering to.</param>
        /// <remarks>
        /// When a change processor is registered, it will not receive changes that frame, so it should process the
        /// entire visual tree.
        /// </remarks>
        void BeginProcessing(BaseVisualElementPanel panel);

        /// <summary>
        /// Called during the update, if changes are detected.
        /// </summary>
        /// <param name="panel">The panel where the changes occurred.</param>
        /// <param name="changes">The changes that occurred during that frame.</param>
        void ProcessChanges(BaseVisualElementPanel panel, AuthoringChanges changes);

        /// <summary>
        /// Called when the change processor is unregistered.
        /// </summary>
        /// <param name="panel">The panel the change processor is unregistering from.</param>
        /// <param name="changes">The changes that occurred during that frame.</param>
        void EndProcessing(BaseVisualElementPanel panel);
    }

    /// <summary>
    /// Utility class containing the list of changes that occurred during a given frame.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal class AuthoringChanges
    {
        /// <summary>
        /// Elements that were either added to the panel or moved within the panel.
        /// </summary>
        public HashSet<VisualElement> addedOrMovedElements { get; } = new();

        /// <summary>
        /// Elements that were removed from the panel.
        /// </summary>
        public HashSet<VisualElement> removedFromPanel { get; } = new();

        /// <summary>
        /// Elements who have changed styles.
        /// </summary>
        public HashSet<VisualElement> styleChanged { get; } = new();

        /// <summary>
        /// Elements where the styling context has changed.
        /// </summary>
        public HashSet<VisualElement> stylingContextChanged { get; } = new();

        /// <summary>
        /// Elements where the binding context has changed.
        /// </summary>
        public HashSet<VisualElement> bindingContextChanged { get; } = new();

        /// <summary>
        /// Indicates if a changes has occurred.
        /// </summary>
        /// <returns><see langword="true"/> if any change occurred; <see langword="false"/> otherwise.</returns>
        public bool ContainsChanges()
        {
            return addedOrMovedElements.Count > 0 ||
                   removedFromPanel.Count > 0 ||
                   styleChanged.Count > 0 ||
                   stylingContextChanged.Count > 0 ||
                   bindingContextChanged.Count > 0;
        }

        /// <summary>
        /// Clears internal buffers.
        /// </summary>
        public void Clear()
        {
            addedOrMovedElements.Clear();
            removedFromPanel.Clear();
            styleChanged.Clear();
            stylingContextChanged.Clear();
            bindingContextChanged.Clear();
        }
    }

    internal sealed class VisualTreeAuthoringUpdater : BaseVisualTreeUpdater
    {
        // For testing purposes
        internal struct StateSnapshot
        {
            public int processorsCount;
            public bool containsAccumulatedChanges;
            public bool isProcessingChanges;
        }

        // For testing purposes
        internal StateSnapshot GetState()
        {
            return new StateSnapshot
            {
                processorsCount = m_RegisteredProcessors.Count,
                containsAccumulatedChanges = m_Accumulator.ContainsChanges(),
                isProcessingChanges = m_AccumulatingChanges
            };
        }

        /// <summary>
        /// Any changes related to styling properties (inline styles, computed styles, etc.)
        /// </summary>
        private const VersionChangeType k_StyleChangedFlags =
            VersionChangeType.Layout |
            VersionChangeType.Styles;

        /// <summary>
        /// Any changes related to the styling context (style sheet, uss classes, etc.)
        /// </summary>
        private const VersionChangeType k_StylingContextChangedFlags =
            VersionChangeType.StyleSheet;

        /// <summary>
        /// Any changes related to the binding context (registration, data sources, etc.)
        /// </summary>
        private const VersionChangeType k_BindingsChangedFlags =
            VersionChangeType.Bindings |
            VersionChangeType.BindingRegistration |
            VersionChangeType.DataSource;

        static readonly ProfilerMarker s_UpdateProfilerMarker = new ProfilerMarker("Update Authoring");
        static readonly ProfilerMarker s_UpdateChangeProfilerMarker = new ProfilerMarker("Update Authoring - Change");

        private readonly List<IVisualElementChangeProcessor> m_RegisteredProcessors = new();
        private readonly List<IVisualElementChangeProcessor> m_ProcessorRegistrationList = new();
        private readonly List<IVisualElementChangeProcessor> m_ProcessorUnregistrationList = new();
        private BaseVisualElementPanel m_AttachedPanel;

        private readonly AuthoringChanges m_Changes1;
        private readonly AuthoringChanges m_Changes2;

        private AuthoringChanges m_Accumulator;
        private AuthoringChanges m_Notifier;

        private bool m_AccumulatingChanges;

        public override ProfilerMarker profilerMarker => s_UpdateProfilerMarker;

        private bool shouldUpdate => m_AccumulatingChanges || m_ProcessorRegistrationList.Count > 0;

        public VisualTreeAuthoringUpdater()
        {
            panelChanged += OnPanelChanged;

            m_Changes1 = new();
            m_Changes2 = new();
            m_Accumulator = m_Changes1;
            m_Notifier = m_Changes2;
        }

        public void RegisterProcessor(IVisualElementChangeProcessor processor)
        {
            if (m_RegisteredProcessors.Contains(processor) || m_ProcessorRegistrationList.Contains(processor))
                return;

            m_ProcessorRegistrationList.Add(processor);
            m_ProcessorUnregistrationList.Remove(processor);
        }

        public void UnregisterProcessor(IVisualElementChangeProcessor processor)
        {
            if (!m_RegisteredProcessors.Contains(processor) || m_ProcessorUnregistrationList.Contains(processor))
                return;

            m_ProcessorUnregistrationList.Add(processor);
            m_ProcessorRegistrationList.Remove(processor);
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if (!m_AccumulatingChanges)
                return;

            using var _ = s_UpdateChangeProfilerMarker.Auto();

            if ((versionChangeType & k_StyleChangedFlags) != 0)
                m_Accumulator.styleChanged.Add(ve);

            if ((versionChangeType & k_StylingContextChangedFlags) != 0)
                m_Accumulator.stylingContextChanged.Add(ve);

            if ((versionChangeType & k_BindingsChangedFlags) != 0)
                m_Accumulator.bindingContextChanged.Add(ve);
        }

        public override void Update()
        {
            if (!shouldUpdate)
                return;

            bool? startAccumulatingChangesNextFrame = null;

            SwapBuffers();

            var changes = m_Notifier;

            if (changes.ContainsChanges())
            {
                for (var i = 0; i < m_RegisteredProcessors.Count; ++i)
                {
                    var processor = m_RegisteredProcessors[i];
                    processor.ProcessChanges(panel, changes);
                }
            }

            for (var i = 0; i < m_ProcessorRegistrationList.Count; ++i)
            {
                var processor = m_ProcessorRegistrationList[i];
                m_RegisteredProcessors.Add(processor);
                processor.BeginProcessing(panel);
                startAccumulatingChangesNextFrame = true;
            }
            m_ProcessorRegistrationList.Clear();

            for (var i = 0; i < m_ProcessorUnregistrationList.Count; ++i)
            {
                var processor = m_ProcessorUnregistrationList[i];
                m_RegisteredProcessors.Remove(processor);
                processor.EndProcessing(panel);
            }
            m_ProcessorUnregistrationList.Clear();

            if (m_RegisteredProcessors.Count == 0)
                startAccumulatingChangesNextFrame = false;

            if (startAccumulatingChangesNextFrame.HasValue)
                m_AccumulatingChanges = startAccumulatingChangesNextFrame.Value;

            changes.Clear();
        }

        private void OnPanelChanged(BaseVisualElementPanel p)
        {
            if (m_AttachedPanel == p)
                return;

            if (null != m_AttachedPanel)
                m_AttachedPanel.hierarchyChanged -= OnHierarchyChange;

            m_AttachedPanel = p;

            if (null != m_AttachedPanel)
                m_AttachedPanel.hierarchyChanged += OnHierarchyChange;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            SwapBuffers();

            var changes = m_Notifier;
            for (var i = 0; i < m_RegisteredProcessors.Count; ++i)
            {
                var processor = m_RegisteredProcessors[i];
                processor.ProcessChanges(panel, changes);
                processor.EndProcessing(panel);
            }

            m_ProcessorRegistrationList.Clear();
            m_ProcessorUnregistrationList.Clear();
            m_RegisteredProcessors.Clear();

            panelChanged -= OnPanelChanged;

            if (null != m_AttachedPanel)
                m_AttachedPanel.hierarchyChanged -= OnHierarchyChange;

            changes.Clear();
        }

        private void OnHierarchyChange(VisualElement ve, HierarchyChangeType type,
            IReadOnlyList<VisualElement> additionalContext = null)
        {
            if (!m_AccumulatingChanges)
                return;

            using var _ = s_UpdateChangeProfilerMarker.Auto();
            switch (type)
            {
                case HierarchyChangeType.RemovedFromParent:
                    m_Accumulator.addedOrMovedElements.Remove(ve);
                    break;
                case HierarchyChangeType.AddedToParent:
                case HierarchyChangeType.ChildrenReordered:
                    m_Accumulator.addedOrMovedElements.Add(ve);
                    break;
                case HierarchyChangeType.AttachedToPanel:
                    for (var i = 0; i < additionalContext.Count; ++i)
                    {
                        var added = additionalContext[i];
                        m_Accumulator.addedOrMovedElements.Add(added);
                        m_Accumulator.removedFromPanel.Remove(added);
                    }

                    break;
                case HierarchyChangeType.DetachedFromPanel:
                    for (var i = 0; i < additionalContext.Count; ++i)
                    {
                        var removed = additionalContext[i];
                        m_Accumulator.addedOrMovedElements.Remove(removed);
                        m_Accumulator.removedFromPanel.Add(removed);
                    }
                    break;
            }
        }

        private void SwapBuffers()
        {
            if (m_Accumulator == m_Changes1)
            {
                m_Accumulator = m_Changes2;
                m_Notifier = m_Changes1;
            }
            else
            {
                m_Accumulator = m_Changes1;
                m_Notifier = m_Changes2;
            }
        }
    }
}
