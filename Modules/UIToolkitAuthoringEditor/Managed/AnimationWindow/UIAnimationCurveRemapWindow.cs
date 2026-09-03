// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using Unity.Hierarchy;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    // Picks the element a broken curve row should be remapped to. Rows are rendered by HierarchyView, so
    // they read the same as VisualElements do in the Hierarchy window. Shown as a drop-down at the cursor
    // and dismissed by clicking away, matching the Animation Window's own "Add Property" popup.
    internal sealed class UIAnimationCurveRemapWindow : EditorWindow
    {
        // EditorWindow's own constructor registers a delayed call; this drop-down closes itself before an
        // assembly reload, so the instance it registers for cannot outlive the CodeLoaded scope.
        #pragma warning disable UAL0015
        UIAnimationCurveRemapWindow() { }
        #pragma warning restore UAL0015

        const float k_Width = 340f;
        const float k_Height = 420f;
        const double k_UpdateBudgetMs = 1000.0 / 60.0;

        const string k_StyleSheetPath = "UIToolkitAuthoring/AnimationWindow/UIAnimationCurveRemapWindow.uss";
        const string k_UssClassName = "ui-animation-curve-remap-window";

        // Both placements originate the window at the activator's bottom-left, which for a zero-size rect
        // at the cursor is the cursor itself. Overlay backs up Below because the default order ends in
        // Above, which would anchor the window's bottom-left to the cursor whenever it does not fit under.
        static readonly PopupLocation[] k_Placement = { PopupLocation.Below, PopupLocation.Overlay };

        static readonly string k_Title = L10n.Tr("Remap Animation Curve");
        static readonly string k_SharedFormat = L10n.Tr("Clip shared by {0} elements - all are affected");
        static readonly string k_BrokenLabel = L10n.Tr("Broken path");
        static readonly string k_TargetLabel = L10n.Tr("Target path");
        static readonly string k_Unnamed = L10n.Tr("Name this element to animate it");
        static readonly string k_ShadowedFormat = L10n.Tr("Another element named \"{0}\" is registered first");
        static readonly string k_OccupiedFormat = L10n.Tr("The clip already animates \"{0}\" on this element");
        static readonly string k_NoTarget = L10n.Tr("Select an element");
        static readonly string k_Cancel = L10n.Tr("Cancel");
        static readonly string k_Remap = L10n.Tr("Remap");

        Action<string> m_OnPicked;
        UIAnimationRemapTarget m_Root;
        CurveRepairOptions m_Options;

        // Counted when the popup opens, being its banner's business alone: above one, the clip is
        // reached through a selector and repairing one match rewrites the paths of every match.
        int m_SharedElementCount;

        // Qualified because Unity.Hierarchy is in scope as a namespace here, as HierarchyView itself does.
        Unity.Hierarchy.Hierarchy m_Hierarchy;
        HierarchyView m_View;
        UIAnimationRemapTargetHandler m_Handler;
        ToolbarSearchField m_SearchField;

        Label m_TargetPath;
        Button m_RemapButton;
        UIAnimationRemapTarget m_Selected;

        // Returns before the user has picked anything: onPicked runs later, or never if the popup is
        // dismissed. activatorScreenRect is in screen space because a drop-down is placed against the
        // screen, not against whatever GUI happened to be current.
        internal static void Show(Rect activatorScreenRect, UIAnimationBinder binder, AnimationClip clip,
            UIAnimationClip uiClip, in CurveRepairOptions options, Action<string> onPicked)
        {
            var root = UIAnimationRemapTargetTree.Build(binder, clip, options.BrokenElementPath);
            if (root == null)
                return;

            var window = CreateInstance<UIAnimationCurveRemapWindow>();
            window.titleContent = new GUIContent(k_Title);
            window.m_Root = root;
            window.m_Options = options;
            window.m_SharedElementCount =
                UIAnimationCurveRepair.CountElementsUsingClip(root.Element?.panel?.visualTree, uiClip);
            window.m_OnPicked = onPicked;
            window.ShowAsDropDown(activatorScreenRect, new Vector2(k_Width, k_Height), k_Placement,
                ShowMode.PopupMenu, giveFocus: true);
        }

        void CreateGUI()
        {
            if (m_Root == null)
            {
                Close();
                return;
            }

            var content = rootVisualElement;
            content.AddToClassList(k_UssClassName);
            if (EditorGUIUtility.Load(k_StyleSheetPath) is StyleSheet styleSheet)
                content.styleSheets.Add(styleSheet);

            if (m_SharedElementCount > 1)
            {
                var header = new VisualElement();
                header.AddToClassList(k_UssClassName + "__header");
                header.Add(BuildBanner());
                content.Add(header);
            }

            content.Add(BuildSearchField());
            content.Add(BuildTree());

            var footer = new VisualElement();
            footer.AddToClassList(k_UssClassName + "__footer");
            footer.Add(BuildPathRow(k_BrokenLabel, DescribeBrokenPath(), out _));
            footer.Add(BuildPathRow(k_TargetLabel, k_NoTarget, out m_TargetPath));
            footer.Add(BuildButtons());
            content.Add(footer);

            UpdateSelection();
            m_SearchField.Focus();
        }

        // A drop-down survives a domain reload as a window with no content behind it, so it closes first.
        void OnEnable() => AssemblyReloadEvents.beforeAssemblyReload += Close;

        // HierarchyView is pull-driven: a query set by the search field sits unapplied until someone
        // pumps the view, and nothing else pumps this one. Timed rather than Update() so a pass that
        // will not converge costs a frame's budget instead of the main thread.
        void Update()
        {
            if (m_View?.UpdateNeeded == true)
                m_View.UpdateIncrementalTimed(k_UpdateBudgetMs);
        }

        void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= Close;

            if (m_View != null)
            {
                m_View.GetTooltip -= OnGetTooltip;
                if (m_View.ViewModel != null)
                    m_View.ViewModel.FlagsChanged -= OnFlagsChanged;
                m_View.Dispose();
                m_View = null;
            }

            // SetSourceHierarchy does not take ownership, so the hierarchy is ours to release. Disposing it
            // takes the node type handler with it.
            m_Hierarchy?.Dispose();
            m_Hierarchy = null;
            m_Handler = null;
        }

        VisualElement BuildBanner()
        {
            var banner = new HelpBox(string.Format(k_SharedFormat, m_SharedElementCount),
                HelpBoxMessageType.Warning);
            banner.AddToClassList(k_UssClassName + "__banner");
            return banner;
        }

        static VisualElement BuildPathRow(string label, string value, out Label valueLabel)
        {
            var row = new VisualElement();
            row.AddToClassList(k_UssClassName + "__row");

            var name = new Label(label);
            name.AddToClassList(k_UssClassName + "__row-label");
            row.Add(name);

            valueLabel = new Label(value);
            valueLabel.AddToClassList(k_UssClassName + "__row-value");
            row.Add(valueLabel);

            return row;
        }

        VisualElement BuildSearchField()
        {
            m_SearchField = new ToolbarSearchField();
            m_SearchField.AddToClassList(k_UssClassName + "__search");
            m_SearchField.RegisterValueChangedCallback(e => m_View.Filter = e.newValue);
            return m_SearchField;
        }

        VisualElement BuildTree()
        {
            m_Hierarchy = new Unity.Hierarchy.Hierarchy();
            m_Handler = m_Hierarchy.GetOrCreateNodeTypeHandler<UIAnimationRemapTargetHandler>();
            m_Handler.SetScope(m_Root);
            m_Hierarchy.Update();

            m_View = new HierarchyView();
            m_View.AddToClassList(k_UssClassName + "__tree");
            m_View.SetSourceHierarchy(m_Hierarchy, HierarchyNodeFlags.Expanded);

            // The view installs a navigate column beside Name, for the Hierarchy window's into-document
            // arrows and its header's column menu; the picker binds neither, leaving a bare strip.
            m_View.SetColumns(new List<Column> { m_View.NameColumn });

            // Matches the Hierarchy window, which sets this on its own view rather than it coming with
            // HierarchyView; SearchTreeView does the same for an embedded one.
            m_View.ListView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;

            // Default flags only cover nodes that exist when the source is set, and the handler populates
            // from the panel's change stream afterwards, so the tree is opened once it has been filled.
            // Flagging every node rather than recursing from the root: ExpandRecursive rejects Root.
            m_View.ViewModel.SetFlags(HierarchyNodeFlags.Expanded);

            m_View.GetTooltip += OnGetTooltip;
            m_View.ViewModel.FlagsChanged += OnFlagsChanged;

            return m_View;
        }

        VisualElement BuildButtons()
        {
            var row = new VisualElement();
            row.AddToClassList(k_UssClassName + "__buttons");

            row.Add(new Button(Close) { text = k_Cancel });

            m_RemapButton = new Button(Confirm) { text = k_Remap };
            row.Add(m_RemapButton);

            return row;
        }

        void OnGetTooltip(HierarchyView view, HierarchyViewItem item, StringBuilder tooltip, bool filtering)
        {
            if (m_Handler.TryGetTarget(item.Node, out var target))
                tooltip.Append(Describe(target));
        }

        void OnFlagsChanged(HierarchyNodeFlags flags)
        {
            if ((flags & HierarchyNodeFlags.Selected) != 0)
                UpdateSelection();
        }

        // HierarchyView selects multiple by default and does not expose a way to narrow it, so a
        // multi-row selection resolves to no target rather than silently picking one of them.
        void UpdateSelection()
        {
            UIAnimationRemapTarget selected = null;
            var count = 0;
            var viewModel = m_View?.ViewModel;

            for (var i = 0; viewModel != null && i < viewModel.Count; i++)
            {
                var node = viewModel[i];
                if (!viewModel.HasFlags(node, HierarchyNodeFlags.Selected))
                    continue;

                count++;
                m_Handler.TryGetTarget(node, out selected);
            }

            m_Selected = count == 1 && selected != null &&
                         selected.Status == UIAnimationRemapTargetStatus.Addressable
                ? selected
                : null;

            m_TargetPath.text = m_Selected != null ? DescribePath(m_Selected.ElementPath) : k_NoTarget;
            m_RemapButton.SetEnabled(m_Selected != null && !m_Options.IsReadOnly);
        }

        void Confirm()
        {
            if (m_Selected == null)
                return;

            var picked = m_OnPicked;
            var elementPath = m_Selected.ElementPath;
            Close();
            picked?.Invoke(elementPath);
        }

        string DescribeBrokenPath() =>
            m_Options.BrokenElementPath == null ? string.Empty : DescribePath(m_Options.BrokenElementPath);

        // The root's empty path would otherwise render as nothing at all.
        static string DescribePath(string elementPath) =>
            elementPath.Length == 0 ? UIAnimationCurveRepair.DescribeLastSegment(elementPath) : elementPath;

        static string Describe(UIAnimationRemapTarget target)
        {
            switch (target.Status)
            {
                case UIAnimationRemapTargetStatus.Unnamed:
                    return k_Unnamed;
                case UIAnimationRemapTargetStatus.NameShadowed:
                    return string.Format(k_ShadowedFormat, target.DisplayName);
                case UIAnimationRemapTargetStatus.Occupied:
                    return string.Format(k_OccupiedFormat, target.ConflictingPropertyName);
                default:
                    return DescribePath(target.ElementPath);
            }
        }
    }
}
