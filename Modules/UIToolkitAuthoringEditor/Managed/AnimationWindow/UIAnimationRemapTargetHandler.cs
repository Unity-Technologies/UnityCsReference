// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Hierarchy;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    // Feeds the remap picker's tree. Derives from the handler the Hierarchy window uses so rows are built
    // by the same code - icon, name and type labels, and the stylesheet OnBindView installs, which is what
    // makes the disabled class mean anything.
    internal sealed class UIAnimationRemapTargetHandler : VisualElementNodeHandler
    {
        internal new const string NodeTypeName = "UIAnimationRemapTargetHandler";

        readonly Dictionary<VisualElement, UIAnimationRemapTarget> m_TargetByElement = new();
        VisualElement m_ScopeRoot;

        public override string GetNodeTypeName() => NodeTypeName;

        // The picker borrows the handler for its row rendering, not its stage integration: the stage
        // strategies register the stage's panels and re-clone the edited document while the handler
        // initializes, replacing every element the picker's model was just built from. An inert strategy
        // touches neither; SetScope wires the one panel the picker watches.
        protected override NodeHandlerStageStrategy CreateStageStrategy() => new RemapPickerStrategy();

        // The strategy declares rows fully editable so they render undimmed, which would also open the
        // drag gates that key off the same flags. Picking is selection only.
        protected override bool CanStartDrag(HierarchyView view, in SelectionContext selection) => false;

        sealed class RemapPickerStrategy() : NodeHandlerStageStrategy(null)
        {
            // The defaults would dim every row: IsReadOnly greys the labels and anything short of
            // FullyEditable disables the whole row. Which rows read as unpickable is this handler's
            // OnBindItem to decide, from the target's status.
            public override bool IsReadOnly => false;

            public override VisualElementEditFlags GetEditFlags(VisualElement element) =>
                VisualElementEditFlags.FullyEditable;
        }

        /// <summary>
        /// Point the handler at the subtree a broken curve can be remapped within.
        /// </summary>
        internal void SetScope(UIAnimationRemapTarget root)
        {
            m_TargetByElement.Clear();
            m_ScopeRoot = root?.Element;
            Collect(root);

            if (m_ScopeRoot?.panel is not Panel panel)
                return;

            RegisterPanel(panel);

            // The authoring updater begins a processor only on its own next update, so the popup would
            // open on an empty tree. Run the walk directly; the begin the updater delivers later rebuilds
            // the same nodes onto themselves.
            ((IVisualElementChangeProcessor)this).BeginProcessing(panel);
        }

        internal bool TryGetTarget(in HierarchyNode node, out UIAnimationRemapTarget target)
        {
            target = null;
            return TryGetElementFromNode(in node, out var element) &&
                   m_TargetByElement.TryGetValue(element, out target);
        }

        void Collect(UIAnimationRemapTarget target)
        {
            if (target?.Element == null)
                return;

            m_TargetByElement[target.Element] = target;
            foreach (var child in target.Children)
                Collect(child);
        }

        // A panel carries every open document, but only the animation root's subtree is addressable by the
        // binder. Ancestors yield their children so the walk can reach the root; anything off that line is
        // dropped entirely. Before a scope exists everything is refused: a walk can reach the handler
        // through panel registration alone, and admitting it would fill the tree with the panel.
        protected override NodeCreationType ShouldCreateNode(VisualElement element)
        {
            if (m_ScopeRoot == null)
                return NodeCreationType.DontCreate;

            if (element == m_ScopeRoot || m_TargetByElement.ContainsKey(element))
                return NodeCreationType.Create;

            return IsAncestorOf(element, m_ScopeRoot)
                ? NodeCreationType.CreateChildren
                : NodeCreationType.DontCreate;
        }

        protected override void OnBindItem(HierarchyViewItem item)
        {
            base.OnBindItem(item);

            if (!TryGetTarget(item.Node, out var target))
                return;

            item.EnableInClassList(HierarchyItemDisabledClassName,
                target.Status != UIAnimationRemapTargetStatus.Addressable);

            // The animation root is addressable under the empty path, but is often unnamed - so the base
            // would label a perfectly pickable row with nothing, or with the wording for one that is not.
            if (target.ElementPath != null && target.ElementPath.Length == 0)
                item.Name.text = UIAnimationCurveRepair.DescribeLastSegment(string.Empty);
        }

        static bool IsAncestorOf(VisualElement candidate, VisualElement element)
        {
            for (var current = element?.hierarchy.parent; current != null; current = current.hierarchy.parent)
            {
                if (current == candidate)
                    return true;
            }

            return false;
        }
    }
}
