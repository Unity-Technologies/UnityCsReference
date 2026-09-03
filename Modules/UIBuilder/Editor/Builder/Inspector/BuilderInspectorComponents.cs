// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIBuilder not yet converted
using Unity.UIToolkit.Editor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Hosts the shared <see cref="VisualElementComponentsInspectorElement"/> inside the UI Builder inspector
    /// so UI Toolkit components are added, removed, and edited through the exact same path as the standalone
    /// VisualElement inspector. This section owns no component logic: it feeds the current selection and
    /// bridges a component add or remove back into the UI Builder document.
    /// </summary>
    internal class BuilderInspectorComponents : IBuilderInspectorSection
    {
        readonly BuilderInspector m_Inspector;
        readonly PersistedFoldout m_Foldout;
        readonly VisualElementComponentsInspectorElement m_Components;

        public VisualElement root => m_Foldout;

        public BuilderInspectorComponents(BuilderInspector inspector)
        {
            m_Inspector = inspector;

            m_Foldout = new PersistedFoldout
            {
                text = "Components",
                name = "inspector-components-foldout",
                viewDataKey = "inspector-components-foldout",
            };

            m_Components = new VisualElementComponentsInspectorElement();
            m_Components.changed += OnComponentsChanged;
            m_Foldout.Add(m_Components);

            // Sit directly below the element attributes foldout, matching the standalone inspector's order.
            // Fall back to the inspector's scroll view content (never the root, which the scroll view clips).
            var attributesFoldout = inspector.Q<PersistedFoldout>("inspector-attributes-foldout");
            if (attributesFoldout?.parent != null)
                attributesFoldout.parent.Insert(attributesFoldout.parent.IndexOf(attributesFoldout) + 1, m_Foldout);
            else if (inspector.Q<ScrollView>("inspector-scroll-view") is { } scrollView)
                scrollView.Add(m_Foldout);
            else
                inspector.Add(m_Foldout);
        }

        public void Refresh()
        {
            // This section owns its own visibility rather than relying on the section-enable pass: show it only
            // for an element selection with the experimental setting on. Only a real element is editable;
            // instances are shown read only, mirroring how the standalone inspector treats a read-only target.
            var selectionType = m_Inspector.selection.selectionType;
            var isElementSelection =
                selectionType == BuilderSelectionType.Element ||
                selectionType == BuilderSelectionType.ElementInTemplateInstance ||
                selectionType == BuilderSelectionType.ElementInControlInstance ||
                selectionType == BuilderSelectionType.ElementInParentDocument;

            var show = UIToolkitProjectSettings.enableUIComponents && isElementSelection;
            m_Foldout.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

            m_Components.IsReadOnly = selectionType != BuilderSelectionType.Element;

            // The shared inspector element resolves the element's asset through the authoring linkage
            // (VisualElement.visualElementAsset); the Builder canvas links elements through its own
            // property, so bridge it when handing over the selection.
            var target = show ? m_Inspector.currentVisualElement : null;
            if (target != null && target.visualElementAsset == null)
                target.visualElementAsset = target.GetVisualElementAsset();
            m_Components.Target = target;
        }

        public void Enable() => m_Components.SetEnabled(true);

        public void Disable() => m_Components.SetEnabled(false);

        // Setting Target to null disposes each per-component editing context.
        public void Dispose() => m_Components.Target = null;

        // A component add or remove changes the selected element's asset. Notify UI Builder so the canvas,
        // hierarchy, and unsaved-changes state pick it up, the same signal a native attribute edit sends.
        void OnComponentsChanged()
            => m_Inspector.selection.NotifyOfHierarchyChange(null, m_Inspector.currentVisualElement);
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
