// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Accessibility;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Generates <see cref="AccessibilityNode"/>s from a visual tree, and wires the inbound screen
    /// reader interactions on the nodes it creates.
    /// </summary>
    /// <remarks>
    /// The tree-to-hierarchy transform is pure — no bridge lifecycle involved, unit-testable
    /// without a screen reader — and applies the following rules:
    ///
    /// - Element types registered in <see cref="AccessibilityRoleRegistry"/> produce a node with
    ///   their registered role and are treated as accessibility leaves — their internal hierarchy
    ///   (prefix labels, checkmarks) is never traversed. Content hosts are the exception (see
    ///   <see cref="TryGetContentHost"/>).
    /// - Open dropdown menus' items, identified by their class, read as selectable options
    ///   (Toggle role, the current choice Selected); the generic rules would omit them.
    /// - Unknown types are never announced as interactive: an unknown leaf with a derivable label
    ///   produces a <see cref="AccessibilityRole.None"/> node; an unknown branch produces a labeled
    ///   <see cref="AccessibilityRole.Container"/> node, or is flattened (children promoted to its
    ///   parent) when it has no label or ignores picking.
    /// - Elements that are not displayed or not visible are omitted along with their subtree, as
    ///   are subtrees of nested panel components whose accessibility gate is off, and a field's
    ///   caption element (already spoken as the field's label).
    ///
    /// Reading order is depth-first tree order by construction.
    ///
    /// The inbound half — activation, slider adjustment, page scrolling with its announcement and
    /// refocus, and keyboard focus following the screen reader cursor — reacts to the live session
    /// through the handlers wired at node creation.
    /// </remarks>
    internal static partial class AccessibilityTreeGenerator
    {
        // Names carrying this prefix are internal implementation ids (unity-content-viewport,
        // unity-checkmark, ...), not author-provided labels.
        const string k_InternalNamePrefix = "unity-";

        /// <summary>
        /// Generates root-level nodes for the element and its subtree into the hierarchy,
        /// recording each element-to-node association in <paramref name="nodeMap"/>.
        /// </summary>
        public static void GenerateSubtree(VisualElement element, AccessibilityHierarchy hierarchy,
            AccessibilityNodeMap nodeMap)
        {
            var cursor = hierarchy.rootNodes.Count;
            GenerateSubtree(element, hierarchy, null, nodeMap, ref cursor, null, null);
        }

        /// <summary>
        /// Diffing variant used to regenerate a scope in place. Nodes for the walked elements are
        /// positioned as the children of <paramref name="parentNode"/> starting at
        /// <paramref name="cursor"/> (which advances past each placed node); an element already
        /// mapped to a live node of the same role <b>from this scope's previous generation</b>
        /// (<paramref name="previousNodes"/>) keeps that node — moved into position and its
        /// derived data refreshed — so its identity (and any screen reader focus on it) survives
        /// the regeneration. An element that entered the scope from elsewhere gets a fresh node
        /// instead: a cross-scope move is remove+add — the map repoints to the fresh node, the
        /// old one is swept by its own scope's regeneration, and the element's hooks survive
        /// through the map's remove guard. That confinement is what keeps concurrent scopes of
        /// one flush (and scopes on other panels) safe: no walk ever moves a node out of another
        /// scope's range, so segment positions and sweeps stay valid without shared state. Every
        /// placed node is recorded in <paramref name="visited"/> (when provided) so the caller
        /// can sweep the stale remainder of the previous generation.
        /// </summary>
        internal static void GenerateSubtree(VisualElement element, AccessibilityHierarchy hierarchy,
            AccessibilityNode parentNode, AccessibilityNodeMap nodeMap, ref int cursor,
            HashSet<AccessibilityNode> previousNodes, HashSet<AccessibilityNode> visited)
        {
            if (IsOmittedSubtree(element))
                return;

            if (AccessibilityRoleRegistry.TryGetRole(element, out var role))
            {
                // Known controls are accessibility leaves; never descend into their internals.
                var node = PlaceNode(element, role, DeriveLabel(element), hierarchy, parentNode,
                    nodeMap, ref cursor, previousNodes, visited);

                // Except a content host, whose content children are placed under its node —
                // see TryGetContentHost.
                if (TryGetContentHost(element, out var content))
                {
                    var contentCursor = 0;
                    for (var i = 0; i < content.hierarchy.childCount; i++)
                    {
                        GenerateSubtree(content.hierarchy[i], hierarchy, node, nodeMap,
                            ref contentCursor, previousNodes, visited);
                    }
                }

                return;
            }

            // Dropdown menu items are plain elements identified by their class (they have no type
            // of their own), and their label elements ignore picking, so the generic rules would
            // omit them entirely. They present as selectable options: they borrow the Toggle role
            // (no closer role exists), the checked pseudo-state marks the current choice as
            // Selected, and activation picks the item and closes the menu.
            if (IsDropdownMenuItem(element))
            {
                PlaceNode(element, AccessibilityRole.Toggle, DeriveDropdownItemLabel(element),
                    hierarchy, parentNode, nodeMap, ref cursor, previousNodes, visited);
                return;
            }

            if (element.hierarchy.childCount == 0)
            {
                var label = DeriveLabel(element);
                if (label != null && element.pickingMode != PickingMode.Ignore)
                    PlaceNode(element, AccessibilityRole.None, label, hierarchy, parentNode, nodeMap, ref cursor, previousNodes, visited);
                return;
            }

            var branchLabel = element.pickingMode != PickingMode.Ignore ? DeriveLabel(element) : null;

            if (branchLabel != null)
            {
                // A labeled branch gets a Container node; its children are positioned inside it
                // with their own cursor. Children are indexed rather than enumerated: the walk
                // runs per structural change, and Children() boxes the list enumerator.
                var containerNode = PlaceNode(element, AccessibilityRole.Container, branchLabel, hierarchy, parentNode, nodeMap, ref cursor, previousNodes, visited);
                var containerCursor = 0;
                for (var i = 0; i < element.hierarchy.childCount; i++)
                {
                    GenerateSubtree(element.hierarchy[i], hierarchy, containerNode, nodeMap, ref containerCursor, previousNodes, visited);
                }

                return;
            }

            // An unlabeled branch is flattened: its children attach to its own parent, continuing
            // at the same position.
            for (var i = 0; i < element.hierarchy.childCount; i++)
            {
                GenerateSubtree(element.hierarchy[i], hierarchy, parentNode, nodeMap, ref cursor, previousNodes, visited);
            }
        }

        /// <summary>
        /// Derives the element's accessible label: prefix-label caption first, then text content,
        /// then the element name as a last resort. UI text is used verbatim; a name is an
        /// identifier, so it is humanized (see <see cref="HumanizeName"/>) before being spoken.
        /// Names inside a composite field's internals never count (see
        /// <see cref="IsInsideCompositeFieldInternals"/>). Returns null for unlabeled elements.
        /// </summary>
        internal static string DeriveLabel(VisualElement element)
        {
            if (element is IPrefixLabel prefixLabel && !string.IsNullOrEmpty(prefixLabel.label))
                return prefixLabel.label;

            if (element is TextElement textElement && !string.IsNullOrEmpty(textElement.text))
                return textElement.text;

            // A toggle's or radio button's optional side text is its text content, like a button's.
            if (element is BaseBoolField boolField && !string.IsNullOrEmpty(boolField.text))
                return boolField.text;

            // Template names ("<document>-container", instanced templates) and internal
            // implementation ids are element identifiers, not labels.
            if (element is TemplateContainer)
                return null;

            var name = element.name;
            if (!string.IsNullOrEmpty(name) && !name.StartsWith(k_InternalNamePrefix, System.StringComparison.Ordinal) &&
                !IsInsideCompositeFieldInternals(element))
                return HumanizeName(name);

            return null;
        }

        // Composite fields name their internal wrappers without the "unity-" convention
        // (RadioButtonGroup's "choicesContentContainer", "contentContainer"), so the
        // name-as-label last resort would promote pure chrome to labeled nodes. Between an
        // unknown field and its content container, names are implementation ids, never labels;
        // inside the content container (and inside registered controls, which host author
        // content), author naming resumes.
        static bool IsInsideCompositeFieldInternals(VisualElement element)
        {
            for (var ancestor = element.hierarchy.parent; ancestor != null; ancestor = ancestor.hierarchy.parent)
            {
                if (AccessibilityRoleRegistry.TryGetRole(ancestor, out _))
                    return false;

                if (ancestor is AbstractBaseField field)
                {
                    var content = field.contentContainer;
                    if (content == field)
                        return false;

                    return content == element || !content.Contains(element);
                }
            }

            return false;
        }

        /// <summary>
        /// Converts an element name — an identifier such as "pause-button", "main_menu" or
        /// "mainMenu" — into a human-readable label ("Pause button", "Main menu"): separator
        /// characters and camel-case boundaries become spaces, the result is sentence-cased, and
        /// all-caps words are preserved as acronyms ("HUD-panel" -> "HUD panel"). Returns null
        /// when nothing readable remains.
        /// </summary>
        internal static string HumanizeName(string name)
        {
            var words = new List<string>();
            var word = new StringBuilder();

            void FlushWord()
            {
                if (word.Length > 0)
                {
                    words.Add(word.ToString());
                    word.Clear();
                }
            }

            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];

                if (c is '-' or '_' or '.' || char.IsWhiteSpace(c))
                {
                    FlushWord();
                    continue;
                }

                if (char.IsUpper(c) && word.Length > 0)
                {
                    var previous = word[word.Length - 1];

                    // A new word starts where lowercase or a digit meets uppercase ("mainMenu"),
                    // and at the last capital of an all-caps sequence followed by lowercase
                    // ("HUDPanel" -> "HUD" + "Panel").
                    if (char.IsLower(previous) || char.IsDigit(previous) ||
                        (char.IsUpper(previous) && i + 1 < name.Length && char.IsLower(name[i + 1])))
                        FlushWord();
                }

                word.Append(c);
            }

            FlushWord();

            if (words.Count == 0)
                return null;

            var label = new StringBuilder(name.Length + words.Count);
            for (var i = 0; i < words.Count; i++)
            {
                if (i > 0)
                    label.Append(' ');

                var current = words[i];
                if (IsAcronym(current))
                    label.Append(current);
                else if (i == 0)
                    label.Append(char.ToUpperInvariant(current[0])).Append(current.Substring(1).ToLowerInvariant());
                else
                    label.Append(current.ToLowerInvariant());
            }

            return label.ToString();
        }

        static bool IsAcronym(string word)
        {
            if (word.Length < 2)
                return false;

            var hasUpper = false;
            foreach (var c in word)
            {
                if (char.IsLower(c))
                    return false;
                if (char.IsUpper(c))
                    hasUpper = true;
            }

            return hasUpper;
        }

        /// <summary>
        /// Whether the element would produce a node of its own under the current transform rules —
        /// the inclusion half of the keep/flatten/omit decision, used by the live-sync flush to
        /// detect elements whose representation flipped (for example a visibility change).
        /// Ancestor-driven omission is not considered; a flip reported for an element under an
        /// omitted ancestor resolves to no regeneration context and is dropped.
        /// </summary>
        internal static bool WouldGenerateNode(VisualElement element)
        {
            if (IsOmittedSubtree(element))
                return false;

            if (AccessibilityRoleRegistry.TryGetRole(element, out _))
                return true;

            if (element.pickingMode == PickingMode.Ignore)
                return false;

            // Unknown leaves and branches alike only get a node when a label is derivable.
            return DeriveLabel(element) != null;
        }

        /// <summary>
        /// The registered controls that host represented user content instead of being pure
        /// accessibility leaves: their content children are generated under their node while
        /// their internal chrome stays unrepresented, structural changes in the content
        /// regenerate under their node, and the frame walk descends through them. ScrollView is
        /// the only such control today; the next one (a later phase's list views) only needs a
        /// case here.
        /// </summary>
        internal static bool TryGetContentHost(VisualElement element, out VisualElement content)
        {
            if (element is ScrollView scrollView)
            {
                content = scrollView.contentContainer;
                return true;
            }

            content = null;
            return false;
        }

        internal static bool IsOmittedSubtree(VisualElement element)
        {
            if (element.resolvedStyle.display == DisplayStyle.None || !element.visible)
                return true;

            // A field's caption element is already spoken as its parent's label (DeriveLabel
            // reads the caption first), never as a node of its own. Registered controls are
            // leaves, so this only matters for unknown IPrefixLabel branches — a composite field
            // would otherwise announce its caption twice, as the group label and again as a
            // static-text child.
            if (element.hierarchy.parent is IPrefixLabel prefixLabel && prefixLabel.labelElement == element)
                return true;

            // A nested panel component (child UIDocument) that opted out of accessibility is never
            // walked; this composes with the top-level gate the bridge evaluates at registration.
            if (element is IPanelComponentRootElement rootElement &&
                rootElement.panelComponent is { } component && !component.GetAccessibilityEnabled())
                return true;

            return false;
        }

        // Reuses the element's existing node when it belongs to the scope being regenerated
        // (previousNodes), is still live and has the same role; otherwise, creates a fresh one at
        // the cursor. Reuse is confined to the scope on purpose: pulling a node in from another
        // scope would shift that scope's positions under its own walk (see the GenerateSubtree
        // remarks). A reused node is only moved when it is not already in position — during a
        // sequential walk, every earlier position is already filled, so a surviving sibling that
        // kept its place costs one list peek and no native call.
        static AccessibilityNode PlaceNode(VisualElement element, AccessibilityRole role, string label,
            AccessibilityHierarchy hierarchy, AccessibilityNode parentNode,
            AccessibilityNodeMap nodeMap, ref int cursor, HashSet<AccessibilityNode> previousNodes,
            HashSet<AccessibilityNode> visited)
        {
            AccessibilityNode node = null;

            if (visited != null && previousNodes != null &&
                nodeMap.TryGetNode(element, out var existing) &&
                previousNodes.Contains(existing) &&
                existing.role == role && hierarchy.ContainsNode(existing))
                node = existing;

            if (node != null)
            {
                var siblings = parentNode?.children ?? hierarchy.rootNodes;
                if (node.parent != parentNode || cursor >= siblings.Count || siblings[cursor] != node)
                    hierarchy.MoveNode(node, parentNode, cursor);

                // Refresh the derived data that may have changed since the node was generated;
                // the frame getter and inbound action handlers already capture this element.
                node.label = label;

                // Expanded is session state owned by the bridge (set while the element's dropdown
                // menu is open), not derivable from the element; a reusing walk must not clear it.
                node.state = DeriveState(element) | (node.state & AccessibilityState.Expanded);

                node.value = DeriveValue(element);
            }
            else
            {
                node = CreateNode(element, role, label, hierarchy, parentNode, cursor, nodeMap);
            }

            cursor++;
            visited?.Add(node);
            return node;
        }

        // Creation lives in its own method because the frame getter's closure makes the compiler
        // allocate the capture environment on every call of the method containing the lambda,
        // even when the creating branch is not taken — inlined into PlaceNode it costs one
        // allocation per REUSED node per regeneration walk.
        static AccessibilityNode CreateNode(VisualElement element, AccessibilityRole role, string label,
            AccessibilityHierarchy hierarchy, AccessibilityNode parentNode, int cursor,
            AccessibilityNodeMap nodeMap)
        {
            var node = hierarchy.InsertNode(cursor, label, parentNode);
            node.role = role;
            node.state = DeriveState(element);
            node.value = DeriveValue(element);
            node.frameGetter = () => AccessibilityFrameProjection.GetScreenFrame(element);
            node.focusChanged += (_, focused) => OnScreenReaderFocusChanged(element, node, focused);

            WireActions(element, node);

            nodeMap.Set(element, node);
            return node;
        }

        // Inbound screen reader actions are wired when the node is created, before the hierarchy
        // can be activated: platforms that push node capabilities once at native-node creation
        // (Android) must already see the handlers by then.
        static void WireActions(VisualElement element, AccessibilityNode node)
        {
            switch (element)
            {
                case Button button:
                    node.invoked += () => InvokeButton(button);
                    break;
                case Toggle toggle:
                    node.invoked += () => InvokeToggle(toggle);
                    break;
                case RadioButton radioButton:
                    node.invoked += () => InvokeRadioButton(radioButton);
                    break;
                case var textInput when IsTextInputField(textInput):
                    node.invoked += () => FocusTextInputField(textInput);
                    break;
                case BaseSlider<float> slider:
                    node.incremented += () => AdjustSliderValue(slider, +1);
                    node.decremented += () => AdjustSliderValue(slider, -1);
                    break;
                case BaseSlider<int> slider:
                    node.incremented += () => AdjustSliderValue(slider, +1);
                    node.decremented += () => AdjustSliderValue(slider, -1);
                    break;
                case ScrollView scrollView:
                    node.scrolled += direction => ScrollTowards(scrollView, node, direction);
                    break;
                case var popupField when IsPopupField(popupField):
                    node.invoked += () => InvokePopupField(popupField, node);
                    break;
                case var menuItem when IsDropdownMenuItem(menuItem):
                    node.invoked += () => InvokeDropdownMenuItem(menuItem);
                    break;
            }
        }

        // Shared guard for every inbound screen reader action: a detached or disabled control
        // takes none.
        static bool CanReceiveAction(VisualElement element) =>
            element.enabledInHierarchy && element.elementPanel != null;

        static bool InvokeButton(Button button)
        {
            if (!CanReceiveAction(button))
                return false;

            // The keyboard-activation path: a submitted button simulates a click, briefly showing
            // the active pseudo-state and firing clicked/clickedWithEventInfo.
            using var evt = NavigationSubmitEvent.GetPooled();
            evt.elementTarget = button;
            button.clickable?.SimulateSingleClick(evt);
            return true;
        }

        // Activation toggles the choices menu, the platform dropdown convention: opening goes
        // through the same event the keyboard submit path uses, and collapsing closes the open
        // menu the bridge tracked for this field (Expanded is the bridge-set open marker).
        static bool InvokePopupField(VisualElement popupField, AccessibilityNode node)
        {
            if (!CanReceiveAction(popupField))
                return false;

            if ((node.state & AccessibilityState.Expanded) != 0)
            {
                if (!UITKAccessibilityBridge.TryGetOpenDropdownMenu(popupField, out var openMenu))
                    return false;

                openMenu.Hide(giveFocusBack: true);
                return true;
            }

            using var evt = NavigationSubmitEvent.GetPooled();
            evt.elementTarget = popupField;
            popupField.SendEvent(evt);
            return true;
        }

        // A menu item's owning menu holds the activation (the rows carry no callbacks); the menu
        // marks its content container with itself as userData while open, which doubles as the
        // liveness check. Activation mirrors the menu's own keyboard submit path
        // (GenericDropdownMenu.Apply, KeyboardNavigationOperation.Submit): the item's registered
        // actions run, then a single-selection menu closes.
        static bool InvokeDropdownMenuItem(VisualElement itemElement)
        {
            if (!CanReceiveAction(itemElement))
                return false;

            GenericDropdownMenu menu = null;
            for (var current = itemElement.hierarchy.parent; current != null; current = current.hierarchy.parent)
            {
                if (current.userData is GenericDropdownMenu owner)
                {
                    menu = owner;
                    break;
                }
            }

            if (menu == null)
                return false;

            var items = menu.items;
            for (var i = 0; i < items.Count; ++i)
            {
                var item = items[i];
                if (item.element != itemElement)
                    continue;

                if (!item.element.enabledSelf)
                    return false;

                item.action?.Invoke();
                item.actionUserData?.Invoke(item.element.userData);

                if (menu.isSingleSelectionDropdown)
                {
                    menu.Hide(giveFocusBack: true);
                }

                return true;
            }

            return false;
        }

        static bool InvokeToggle(Toggle toggle)
        {
            if (!CanReceiveAction(toggle))
                return false;

            toggle.value = !toggle.value;
            return true;
        }

        // Selecting is one-way, mirroring RadioButton.ToggleValue: activating an already selected
        // radio button keeps it selected (deselecting would leave its group with no choice), and
        // the action still reports handled — Windows drives it through the UIA Toggle pattern,
        // where a refusal plays an error sound.
        static bool InvokeRadioButton(RadioButton radioButton)
        {
            if (!CanReceiveAction(radioButton))
                return false;

            if (!radioButton.value)
                radioButton.value = true;

            return true;
        }

        /// <summary>
        /// Whether focusing a text field opens the on-screen keyboard on this platform — the text
        /// editing stack's own answer (<see cref="TextEditingUtilities.TouchScreenKeyboardCanBeUsed"/>),
        /// so the two can never drift. Settable so tests can exercise both behaviors on any host.
        /// </summary>
        [AutoStaticsCleanupOnCodeReload] // Auto re-captures the initializer: the platform default is re-derived on reload.
        internal static bool focusOpensOnScreenKeyboard { get; set; } = TextEditingUtilities.TouchScreenKeyboardCanBeUsed();

        // The node currently holding the screen reader cursor, tracked from the focusChanged
        // events every generated node subscribes to. Feeds the page-scroll refocus (see
        // ScrollTowards). A node from a discarded generation self-heals: it can never sit under
        // a current scroll view node, so the refocus falls back.
        [AutoStaticsCleanupOnCodeReload]
        static AccessibilityNode s_ScreenReaderFocusedNode;

        internal static AccessibilityNode screenReaderFocusedNode => s_ScreenReaderFocusedNode;

        // The element behind the focused node. Nodes do not survive a rebuild — every one is
        // replaced — so anything that must outlive one (returning the cursor where it was)
        // remembers the element and resolves the node again afterwards.
        [AutoStaticsCleanupOnCodeReload]
        static VisualElement s_ScreenReaderFocusedElement;

        internal static VisualElement screenReaderFocusedElement => s_ScreenReaderFocusedElement;

        /// <summary>
        /// Forgets the tracked cursor position. The cursor belongs to a running screen reader: no
        /// focus-loss event arrives for the node that held it when the screen reader stops, so
        /// without this, a node from the previous session would answer for the cursor's position.
        /// </summary>
        internal static void ClearScreenReaderFocusedNode()
        {
            s_ScreenReaderFocusedNode = null;
            s_ScreenReaderFocusedElement = null;
        }

        /// <summary>
        /// Re-points the tracked cursor at the node its element maps to now, after a rebuild
        /// replaced every node. The cursor has not moved — only the node representing it has — so
        /// the element is kept; a null node means the element is no longer represented and the
        /// tracked position is forgotten entirely.
        /// </summary>
        /// <remarks>
        /// Without this, everything keyed on the tracked node (the page-scroll refocus, the
        /// arrival re-anchor) silently degrades after a rebuild: a node from a discarded
        /// generation can never match a current one, so those paths fall back instead of working.
        /// </remarks>
        internal static void RetrackScreenReaderFocusedNode(AccessibilityNode node)
        {
            if (s_ScreenReaderFocusedElement == null)
                return;

            if (node == null)
            {
                ClearScreenReaderFocusedNode();
                return;
            }

            s_ScreenReaderFocusedNode = node;
        }

        // The screen reader cursor moved onto (or off) the element's node. On arrival, the
        // element is first scrolled into view — the cursor navigates the whole hierarchy,
        // including nodes outside their scroll view's viewport, and its highlight must never sit
        // on invisible content — and then keyboard focus follows it onto focusable controls so
        // the control under the cursor can take keyboard input right away (typing into a text
        // field, arrow-adjusting a slider). Losing the cursor deliberately leaves UI focus alone:
        // desktop screen readers behave that way natively (keyboard focus stays put while the
        // cursor reads static text), and the cursor landing on the next focusable control grabs
        // it anyway.
        static void OnScreenReaderFocusChanged(VisualElement element, AccessibilityNode node, bool focused)
        {
            // A loss only clears the tracked cursor while no other node has taken it since —
            // the gain/loss event order is not guaranteed across platforms.
            if (focused)
            {
                s_ScreenReaderFocusedNode = node;
                s_ScreenReaderFocusedElement = element;
            }
            else
            {
                if (s_ScreenReaderFocusedNode == node)
                {
                    s_ScreenReaderFocusedNode = null;
                    s_ScreenReaderFocusedElement = null;
                }

                // The pending re-anchor self-guards by node identity, so the cancel must NOT be
                // gated on the tracked cursor still pointing here: the gain for the next node
                // can arrive before this loss, and a stale re-anchor left pending would yank
                // the cursor backward once it fired.
                UITKAccessibilityBridge.OnCursorArrivalScrollCanceled(node);
            }

            if (!focused || element.elementPanel == null)
                return;

            // An arrival that scrolls needs two follow-ups. The corrected frame is pushed right
            // here (scrolling is inline translate writes and worldBound recomputes lazily, so the
            // post-scroll frame is already available) for everything that reads it later. But the
            // visual cursor is NOT such a reader: VoiceOver samples the element's geometry before
            // it delivers the focus change so the bridge also re-anchors the cursor with one
            // layout-changed notification once the scrolled frames settle, dropped if the cursor
            // has already moved on.
            if (ScrollAncestorsToShow(element))
            {
                node.frame = AccessibilityFrameProjection.GetScreenFrame(element);
                UITKAccessibilityBridge.OnCursorArrivalScrolled(element, node);
            }

            if (!element.canGrabFocus)
                return;

            // Where focusing a text input field pops the on-screen keyboard, the cursor merely
            // arriving must not: touch screen readers reserve editing for an explicit activation
            // (the VoiceOver/TalkBack double-tap-to-edit convention), and auto-focus oscillated
            // the TalkBack keyboard on the uGUI sample. Activation focuses it instead (see
            // FocusTextInputField).
            if (focusOpensOnScreenKeyboard && IsTextInputField(element))
                return;

            element.Focus();
        }

        // Each ancestor scroll view scrolls just enough to show the element, innermost first
        // (ScrollTo is a no-op for an already fully visible target). Outer scroll views are asked
        // to show the inner scroll view rather than the element itself: the inner scroll view's
        // own geometry is unaffected by its content scrolling, so the outer amounts stay exact
        // even though this frame's world bounds don't yet reflect the inner scroll. Returns
        // whether any offset actually moved. (A ScrollTo fully deferred to the scheduler with no
        // immediate movement is reported as unmoved — that needs the panel to be dirty from
        // unrelated churn in the same frame the cursor arrives, and the miss only costs the
        // cursor-redraw nudge.)
        static bool ScrollAncestorsToShow(VisualElement element)
        {
            var scrolled = false;
            var target = element;
            for (var ancestor = element.hierarchy.parent; ancestor != null; ancestor = ancestor.hierarchy.parent)
            {
                if (ancestor is ScrollView scrollView && scrollView.contentContainer.Contains(target))
                {
                    var offsetBefore = scrollView.scrollOffset;
                    scrollView.ScrollTo(target);
                    scrolled |= scrollView.scrollOffset != offsetBefore;
                    target = scrollView;
                }
            }

            return scrolled;
        }

        // Activation is the editing entry point: focusing the field starts editing and, on touch
        // platforms, opens the on-screen keyboard — which under a screen reader must only happen
        // when the user explicitly activates the field, never because the cursor landed on it.
        // On desktop, the cursor already focused the field, and re-focusing is a no-op, so wiring
        // this uniformly costs nothing and keeps the node's invokable capability consistent
        // across platforms (Android freezes it at node creation).
        static bool FocusTextInputField(VisualElement field)
        {
            if (field.elementPanel == null || !field.canGrabFocus)
                return false;

            field.Focus();
            return true;
        }

        static string DeriveDropdownItemLabel(VisualElement itemElement) =>
            itemElement.Q<Label>(className: GenericDropdownMenu.labelUssClassName)?.text;

        // Screen readers send plain adjust actions carrying no amount of their own (a VoiceOver or
        // TalkBack swipe, Narrator's touch actions); step by a tenth of the range — the common
        // adjustable-control convention — so any slider is crossed in ten actions. The value
        // setter clamps at the ends.
        static void AdjustSliderValue(BaseSlider<float> slider, float direction)
        {
            if (!CanReceiveAction(slider))
                return;

            slider.value += direction * (slider.highValue - slider.lowValue) * 0.1f;
        }

        // Integer sliders step by at least one, so short ranges stay adjustable.
        static void AdjustSliderValue(BaseSlider<int> slider, int direction)
        {
            if (!CanReceiveAction(slider))
                return;

            // An inverted range (highValue below lowValue, a supported slider state) must still
            // step toward highValue on increment like the float sliders do, and flooring a
            // negative step at 1 would flip its direction.
            var range = slider.highValue - slider.lowValue;
            var step = Mathf.Max(1, Mathf.Abs(Mathf.RoundToInt(range * 0.1f)));
            slider.value += direction * (range < 0 ? -step : step);
        }

        // Scroll actions move one viewport page along the requested direction (the
        // AccessibilityScrollDirection docs define each direction's visible-region movement);
        // Forward/Backward map to the scroll view's primary axis.
        static bool ScrollTowards(ScrollView scrollView, AccessibilityNode scrollViewNode,
            AccessibilityScrollDirection direction)
        {
            if (!CanReceiveAction(scrollView))
                return false;

            if (direction is not (AccessibilityScrollDirection.Up or AccessibilityScrollDirection.Down or
                AccessibilityScrollDirection.Left or AccessibilityScrollDirection.Right or
                AccessibilityScrollDirection.Forward or AccessibilityScrollDirection.Backward))
                return false;

            var horizontal =
                direction is AccessibilityScrollDirection.Left or AccessibilityScrollDirection.Right ||
                (direction is AccessibilityScrollDirection.Forward or AccessibilityScrollDirection.Backward &&
                    scrollView.mode == ScrollViewMode.Horizontal);
            var towardStart = direction is AccessibilityScrollDirection.Up or
                AccessibilityScrollDirection.Left or AccessibilityScrollDirection.Backward;

            var viewport = scrollView.contentViewport.layout;
            var page = (horizontal ? viewport.width : viewport.height) * (towardStart ? -1 : 1);
            var target = scrollView.scrollOffset;
            if (horizontal)
                target.x += page;
            else
                target.y += page;

            // The cursor's frame must be captured before scrolling: the refocus target is
            // whatever content arrives at that same position (the iOS convention — the reading
            // position advances by exactly the scrolled page, no matter which element held the
            // cursor). A cursor on the scroll view node itself stays put: the scroll view has
            // not moved.
            Rect? cursorFrame = null;
            var cursorNode = s_ScreenReaderFocusedNode;
            if (cursorNode != null && cursorNode != scrollViewNode &&
                cursorNode.frameGetter != null && IsNodeWithin(scrollViewNode, cursorNode))
                cursorFrame = cursorNode.frameGetter();

            // The setter clamps through the scrollers; report whether anything actually moved so
            // the platform can hand the gesture back at the edges.
            var previousOffset = scrollView.scrollOffset;
            scrollView.scrollOffset = target;
            if (scrollView.scrollOffset == previousOffset)
                return false;

            AccessibilityNode nodeToFocus = null;
            if (cursorNode != scrollViewNode)
            {
                // The refocus target is whatever content arrived at the cursor's pre-scroll
                // position; falls back to the top of the revealed page when nothing arrives
                // there (a clamped, partial last page) or when the cursor was not inside this
                // scroll view's content at all. Element frames already reflect the scroll that
                // just happened: the offset setter applies the content translate through the
                // transform fast path, and worldBound recomputes on read (only the nodes'
                // cached frames wait for the next flush).
                if (cursorFrame.HasValue)
                    nodeToFocus = FirstContentNodeOverlapping(scrollViewNode, cursorFrame.Value);

                nodeToFocus ??= FirstContentNodeOverlapping(scrollViewNode,
                    AccessibilityFrameProjection.GetScreenFrame(scrollView.contentViewport));
            }

            AnnouncePageScrolled(scrollView, nodeToFocus, horizontal);
            return true;
        }

        static bool IsNodeWithin(AccessibilityNode root, AccessibilityNode node)
        {
            for (var current = node.parent; current != null; current = current.parent)
            {
                if (current == root)
                    return true;
            }

            return false;
        }

        // First content node in reading order whose frame overlaps the rect; edge-touching does
        // not count (Rect.Overlaps is exclusive).
        static AccessibilityNode FirstContentNodeOverlapping(AccessibilityNode scrollViewNode, Rect rect)
        {
            var children = scrollViewNode.children;
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child.frameGetter != null && child.frameGetter().Overlaps(rect))
                    return child;
            }

            return null;
        }

        // The scrolled contract asks the handler to follow a successful scroll with the
        // page-scrolled notification, which the screen reader reads out (and which ends the
        // scroll interaction cleanly on iOS). Numerals and "%" keep the announcement
        // language-neutral — the engine cannot localize prose like the "Page 19 of 27" the API
        // docs suggest; Windows deliberately ignores the text (Narrator already announces the
        // scroll percent) and only uses the focus node, computed by the caller (see
        // ScrollTowards: position-preserving, with the top of the revealed page as fallback).
        static void AnnouncePageScrolled(ScrollView scrollView, AccessibilityNode nodeToFocus,
            bool scrolledHorizontally)
        {
            var percentage = ScrollerPercentage(
                scrolledHorizontally ? scrollView.horizontalScroller : scrollView.verticalScroller);

            AssistiveSupport.notificationDispatcher.SendPageScrolledAnnouncement($"{percentage}%", nodeToFocus);
        }

        internal static AccessibilityState DeriveState(VisualElement element)
        {
            var state = AccessibilityState.None;

            if (!element.enabledInHierarchy)
                state |= AccessibilityState.Disabled;

            // There is no dedicated checked state; toggles and radio buttons report their checked
            // state as Selected.
            if (element is BaseBoolField { value: true })
                state |= AccessibilityState.Selected;

            // A dropdown menu item's checked pseudo-state marks the menu's current choice.
            if ((element.pseudoStates & PseudoStates.Checked) != 0 && IsDropdownMenuItem(element))
                state |= AccessibilityState.Selected;

            return state;
        }

        /// <summary>
        /// Derives the element's accessible value string: a text field's text (masked for password
        /// fields), a slider's current value, or a scroll view's scroll percentage where the
        /// platform consumes it (see <see cref="platformUsesScrollViewValue"/>). Controls whose
        /// value is already announced through their state (a toggle's checked state) or label carry
        /// no value string.
        /// </summary>
        internal static string DeriveValue(VisualElement element)
        {
            switch (element)
            {
                // Every text input field speaks its displayed text, whatever its value type —
                // matched like the registry matches the family's role, so custom instantiations
                // speak too.
                case var field when IsTextInputField(field):
                    return DeriveTextInputValue(field);

                case BaseSlider<float> slider:
                    // Invariant culture is a correctness requirement, not a style choice: the
                    // Windows adapter parses the leading number to expose the UIA RangeValue
                    // pattern, and a locale decimal comma would break it.
                    return slider.value.ToString("0.##", CultureInfo.InvariantCulture);

                case BaseSlider<int> slider:
                    return slider.value.ToString(CultureInfo.InvariantCulture);

                case ScrollView scrollView:
                    return platformUsesScrollViewValue ? DeriveScrollPercentage(scrollView) : null;

                default:
                    // A dropdown field's value is the choice it currently displays, read off its
                    // internal text element (the controls carry no accessibility members).
                    return IsPopupField(element) ? DerivePopupFieldValue(element) : null;
            }
        }

        // The value is read off the field's inner text element — the one non-generic surface the
        // whole TextInputBaseField family shares. Its text is the real content even when the
        // field masks it on screen (masking is applied at render time), so it is masked here too:
        // the raw text of a password field never reaches the screen reader. An empty field speaks
        // nothing — its rendered placeholder is a prompt, not a value.
        static string DeriveTextInputValue(VisualElement field)
        {
            var textElement = field.Q<TextElement>(
                className: TextInputBaseField<int>.TextInputBase.innerTextElementUssClassName);
            var text = textElement?.text;
            if (string.IsNullOrEmpty(text))
                return null;

            var edition = textElement.edition;
            return edition.isPassword ? new string(edition.maskChar, text.Length) : text;
        }

        // The displayed choice lives on an internal text element (field -> input -> text, beside
        // the arrow). The class-name constant is per-instantiation, but every instantiation wraps
        // the identical string, so any closed type reads the shared value.
        static string DerivePopupFieldValue(VisualElement popupField)
        {
            var text = popupField.Q<TextElement>(className: BasePopupField<int, int>.textUssClassName)?.text;
            return string.IsNullOrEmpty(text) ? null : text;
        }

        // Whether the element is a TextInputBaseField of any value type — the family whose focus
        // starts text editing (and opens the touch keyboard where supported). Matched by generic
        // type definition because the instantiations are open-ended.
        internal static bool IsTextInputField(VisualElement element)
        {
            for (var type = element.GetType(); type != null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(TextInputBaseField<>))
                    return true;
            }

            return false;
        }

        // Whether the element is a BasePopupField of any type arguments — the dropdown family the
        // registry maps to the Dropdown role, identified the same way the role is (by generic type
        // definition), so activation, value and role can never disagree on what counts as a popup.
        static bool IsPopupField(VisualElement element)
        {
            for (var type = element.GetType(); type != null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(BasePopupField<,>))
                    return true;
            }

            return false;
        }

        // Whether the element is a row of an open GenericDropdownMenu. The rows have no type of
        // their own; their class is their identity.
        static bool IsDropdownMenuItem(VisualElement element) =>
            element.ClassListContains(GenericDropdownMenu.itemUssClassNameUnique);

        /// <summary>
        /// Whether this platform's adapter consumes a scroll view node's value. Only the Windows
        /// adapter does. Everywhere else, the string would only be spoken — macOS and iOS map
        /// values onto accessibilityValue, Android folds them into the content description — and
        /// the page-scrolled announcement already carries the position, so the value stays unset.
        /// Settable so tests can exercise both behaviors on any host.
        /// </summary>
        [AutoStaticsCleanupOnCodeReload] // Auto re-captures the initializer: the platform default is re-derived on reload.
        internal static bool platformUsesScrollViewValue { get; set; } = ComputePlatformUsesScrollViewValue();

        static bool ComputePlatformUsesScrollViewValue()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return true;
                default:
                    return false;
            }
        }

        // Integer percentages from 0 to 100, and when the scroll view scrolls both ways, both
        // percentages with the vertical one first ("50%, 75%"). The "%" is safe for the one
        // consumer that parses the string: the Windows adapter extracts the numbers with a
        // regex and skips everything between them.
        static string DeriveScrollPercentage(ScrollView scrollView)
        {
            switch (scrollView.mode)
            {
                case ScrollViewMode.Horizontal:
                    return $"{ScrollerPercentage(scrollView.horizontalScroller)}%";
                case ScrollViewMode.VerticalAndHorizontal:
                    return $"{ScrollerPercentage(scrollView.verticalScroller)}%, {ScrollerPercentage(scrollView.horizontalScroller)}%";
                default:
                    return $"{ScrollerPercentage(scrollView.verticalScroller)}%";
            }
        }

        // The scroller range is the scrollable distance; InverseLerp clamps and reads an empty
        // range (content fits) as 0, "not scrolled".
        static int ScrollerPercentage(Scroller scroller) =>
            Mathf.RoundToInt(Mathf.InverseLerp(scroller.lowValue, scroller.highValue, scroller.value) * 100);

        /// <summary>
        /// True when the value string already reads the scroll view's current percentages,
        /// compared without allocating: the geometry flush asks this once per frame while
        /// anything translates a scroll view, where re-formatting an unchanged "50%, 75%"
        /// only to discard it would be pure garbage.
        /// </summary>
        internal static bool ScrollPercentageIsCurrent(string value, VisualElement element)
        {
            if (value == null || element is not ScrollView scrollView)
                return false;

            var span = value.AsSpan();
            switch (scrollView.mode)
            {
                case ScrollViewMode.Horizontal:
                    return PercentageEquals(span, ScrollerPercentage(scrollView.horizontalScroller));

                case ScrollViewMode.VerticalAndHorizontal:
                {
                    var separator = span.IndexOf(',');
                    return separator >= 0 &&
                        PercentageEquals(span[..separator], ScrollerPercentage(scrollView.verticalScroller)) &&
                        PercentageEquals(span[(separator + 1)..].TrimStart(' '), ScrollerPercentage(scrollView.horizontalScroller));
                }

                default:
                    return PercentageEquals(span, ScrollerPercentage(scrollView.verticalScroller));
            }
        }

        static bool PercentageEquals(ReadOnlySpan<char> text, int percentage) =>
            int.TryParse(text.TrimEnd('%'), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) &&
            parsed == percentage;
    }
}
