// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    // the List<GridTrackSize>-valued track-list style properties have no built-in
    // Builder field, so each is edited by a visual GridTrackAxisEditor hosted next to a hidden TextField
    // that carries the binding, field status/override indicator and copy/paste through Builder machinery.
    partial class BuilderInspectorStyleFields
    {
        static readonly string[] k_GridTrackListStyles =
        {
            "grid-template-columns", "grid-template-rows", "grid-auto-columns", "grid-auto-rows"
        };

        internal static bool IsGridTrackListStyle(string styleName)
        {
            foreach (var s in k_GridTrackListStyles)
                if (s == styleName) return true;
            return false;
        }

        // Grid enum toggle-button groups (grid-auto-flow, justify-items, justify-self) have no icons
        // yet, so their buttons would render as blank squares. Give them word labels instead.
        static readonly string[] k_GridEnumToggleStyles = { "grid-auto-flow", "justify-items", "justify-self" };

        internal static bool TryGetGridEnumButtonLabel(string styleName, string enumAsDash, out string label)
        {
            label = null;
            if (System.Array.IndexOf(k_GridEnumToggleStyles, styleName) < 0)
                return false;
            label = enumAsDash switch
            {
                "flex-start" => "Start",
                "flex-end" => "End",
                "row-dense" => "Row Dense",
                "column-dense" => "Col Dense",
                _ => string.IsNullOrEmpty(enumAsDash)
                    ? enumAsDash
                    : char.ToUpperInvariant(enumAsDash[0]) + enumAsDash.Substring(1)
            };
            return true;
        }

        void BindGridTrackField(BuilderStyleRow styleRow, string styleName, TextField field)
        {
            field.SetContainingRow(styleRow);
            field.isDelayed = true;
            // Tag the field with its USS property name like standard Builder fields do: downstream
            // status/refresh code passes it to StylePropertyName.StylePropertyIdFromString, which throws on null.
            field.SetInspectorStylePropertyName(styleName);
            GetOrCreateFieldListForStyleName(styleName).Add(field);
            SetUpContextualMenuOnStyleField(field);
            field.RegisterValueChangedCallback(e => OnGridTrackFieldChange(e, styleName));

            // The visual editor for this axis, hosted in the same (visible) row as the hidden text field so
            // the row's override indicator (driven by the text field) renders next to it.
            if (styleRow.Q<UnityEditor.UIElements.GridTrackAxisEditor>() == null)
            {
                bool implicitTracks = styleName is "grid-auto-columns" or "grid-auto-rows";
                var axis = new UnityEditor.UIElements.GridTrackAxisEditor(GridAxisTitle(styleName)) { allowRepeat = !implicitTracks };
                axis.changed += list => ApplyGridTrackList(styleName, list);
                styleRow.Add(axis);
            }

            EnsureGridPreview(styleRow);
        }

        static string GridAxisTitle(string styleName) => styleName switch
        {
            "grid-template-columns" => "Template Columns",
            "grid-auto-columns" => "Implicit Columns",
            "grid-template-rows" => "Template Rows",
            "grid-auto-rows" => "Implicit Rows",
            _ => styleName
        };

        UnityEditor.UIElements.GridTemplatePreview m_GridPreview;

        void EnsureGridPreview(BuilderStyleRow anyGridRow)
        {
            var container = anyGridRow.parent?.Q<VisualElement>("grid-template-preview");
            if (container == null)
                return;

            m_GridPreview = container.Q<UnityEditor.UIElements.GridTemplatePreview>();
            if (m_GridPreview == null)
            {
                var label = new Label("Preview");
                label.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
                label.style.marginBottom = 2;
                container.Add(label);
                m_GridPreview = new UnityEditor.UIElements.GridTemplatePreview();
                container.Add(m_GridPreview);
            }
            SyncGridPreview();
        }

        void SyncGridPreview()
        {
            if (m_GridPreview == null || currentVisualElement == null)
                return;

            var cs = currentVisualElement.computedStyle;
            m_GridPreview.SetColumns(new List<GridTrackSize>(cs.gridTemplateColumns.ToArray()));
            m_GridPreview.SetRows(new List<GridTrackSize>(cs.gridTemplateRows.ToArray()));
        }

        // Shared writeback for a whole track list (used by the text field and the visual editor popup).
        void ApplyGridTrackList(string styleName, List<GridTrackSize> list)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            if (list == null || list.Count == 0)
                styleProperty.SetKeyword(styleSheet, StyleKeyword.None);
            else
                styleProperty.SetGridTrackSizeList(styleSheet, list);

            s_StyleChangeList.Clear();
            s_StyleChangeList.Add(styleName);
            NotifyStyleChanges(s_StyleChangeList, true);
        }

        void RefreshGridTrackField(string styleName, TextField field)
        {
            var cs = currentVisualElement.computedStyle;
            ReadOnlySpan<GridTrackSize> tracks;
            if (styleName == "grid-template-columns") tracks = cs.gridTemplateColumns;
            else if (styleName == "grid-template-rows") tracks = cs.gridTemplateRows;
            else if (styleName == "grid-auto-columns") tracks = cs.gridAutoColumns;
            else tracks = cs.gridAutoRows;

            var list = new List<GridTrackSize>(tracks.ToArray());
            field.SetValueWithoutNotify(GridTrackSize.FormatList(list.ToArray()));

            // Keep the visual editor in sync with external changes (selection, undo, etc.).
            field.GetContainingRow()?.Q<UnityEditor.UIElements.GridTrackAxisEditor>()?.SetTracks(list);

            var prop = GetLastStyleProperty(currentRule, styleName);
            m_Inspector.UpdateFieldStatus(field, prop);

            if (styleName is "grid-template-columns" or "grid-template-rows")
                SyncGridPreview();
        }

        void OnGridTrackFieldChange(ChangeEvent<string> evt, string styleName)
        {
            ApplyGridTrackList(styleName, GridTrackSize.ParseList(evt.newValue));
        }

        // CSS Grid line placements (grid-column/row-start/end) are GridLine-valued. Edited here as a
        // text field whose value is "auto", a 1-based line number, or "span <n>".
        static readonly string[] k_GridLineStyles =
        {
            "grid-column-start", "grid-column-end", "grid-row-start", "grid-row-end"
        };

        internal static bool IsGridLineStyle(string styleName)
        {
            foreach (var s in k_GridLineStyles)
                if (s == styleName) return true;
            return false;
        }

        void BindGridLineField(BuilderStyleRow styleRow, string styleName, TextField field)
        {
            field.SetContainingRow(styleRow);
            field.isDelayed = true;
            field.SetInspectorStylePropertyName(styleName);
            GetOrCreateFieldListForStyleName(styleName).Add(field);
            SetUpContextualMenuOnStyleField(field);
            field.RegisterValueChangedCallback(e => OnGridLineFieldChange(e, styleName));
        }

        void RefreshGridLineField(string styleName, TextField field)
        {
            var cs = currentVisualElement.computedStyle;
            GridLine value = styleName switch
            {
                "grid-column-start" => cs.gridColumnStart,
                "grid-column-end" => cs.gridColumnEnd,
                "grid-row-start" => cs.gridRowStart,
                _ => cs.gridRowEnd
            };
            field.SetValueWithoutNotify(value.ToString());

            var prop = GetLastStyleProperty(currentRule, styleName);
            m_Inspector.UpdateFieldStatus(field, prop);
        }

        void OnGridLineFieldChange(ChangeEvent<string> evt, string styleName)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            if (string.IsNullOrWhiteSpace(evt.newValue))
                styleProperty.SetKeyword(styleSheet, StyleKeyword.None);
            else if (GridLine.TryParse(evt.newValue, out var gridLine))
                styleProperty.SetGridLine(styleSheet, gridLine);
            // Invalid input is ignored; the field is reset to the current value on the next refresh.

            s_StyleChangeList.Clear();
            s_StyleChangeList.Add(styleName);
            NotifyStyleChanges(s_StyleChangeList, true);
        }

        // Display strip buttons, bound by name because their order does not match the DisplayStyle enum.
        [NoAutoStaticsCleanup]
        static readonly (string name, DisplayStyle value, string label)[] k_DisplayStripButtons =
        {
            ("flex", DisplayStyle.Flex, "Flex"),
            ("grid", DisplayStyle.Grid, "Grid"),
            ("none", DisplayStyle.None, "None"),
        };

        internal static bool IsDisplayStrip(string styleName) => styleName == "display";

        // A rebuild shifts the selection and dispatches a change; that is not a user edit, so ignore it.
        bool m_RebuildingDisplayStrip;

        void SetupDisplayStrip(ToggleButtonGroup group)
        {
            // Empty selection: never auto-select on rebuild (a spurious change), and allow nothing selected
            // when the value is not in the strip (grid while the flag is off).
            group.allowEmptySelection = true;
            RebuildDisplayStrip(group);
            group.RegisterValueChangedCallback(e => OnDisplayStripChange(e, group));

            void OnFlagChanged(bool _) => RebuildDisplayStrip(group);
            void Subscribe()
            {
                UnityEditor.UIElements.UIToolkitProjectSettings.onEnableGridLayoutChanged -= OnFlagChanged;
                UnityEditor.UIElements.UIToolkitProjectSettings.onEnableGridLayoutChanged += OnFlagChanged;
            }
            group.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                Subscribe();
                // Resync: the flag may have changed while the strip was detached (and unsubscribed).
                RebuildDisplayStrip(group);
            });
            group.RegisterCallback<DetachFromPanelEvent>(_ =>
                UnityEditor.UIElements.UIToolkitProjectSettings.onEnableGridLayoutChanged -= OnFlagChanged);
            if (group.panel != null)
                Subscribe();
        }

        void RebuildDisplayStrip(ToggleButtonGroup group)
        {
            m_RebuildingDisplayStrip = true;
            try
            {
                for (var b = group.GetButton(0); b != null; b = group.GetButton(0))
                    b.RemoveFromHierarchy();

                bool gridOn = UnityEditor.UIElements.UIToolkitProjectSettings.enableGridLayout;
                if (gridOn)
                {
                    // All-text strip in None / Flex / Grid order; the flex/none icons are suppressed by the modifier class.
                    foreach (var (name, _, label) in k_DisplayStripButtons)
                        group.Add(new Button { name = name, text = label, tooltip = DisplayStripTooltip(name) });
                }
                else
                {
                    // Original icon strip (flex, none); Grid is not shown while the feature is off.
                    group.Add(new Button { name = "flex", tooltip = DisplayStripTooltip("flex") });
                    group.Add(new Button { name = "none", tooltip = DisplayStripTooltip("none") });
                }

                // Text vs icon presentation is driven by a modifier class so the icon USS only applies when off.
                group.EnableInClassList(BuilderConstants.InspectorDisplayStripTextModifierClassName, gridOn);
                RefreshDisplayStrip(group);
            }
            finally
            {
                m_RebuildingDisplayStrip = false;
            }
        }

        static string DisplayStripTooltip(string enumAsDash)
        {
            return BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(
                string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, "display", enumAsDash),
                out var tip)
                ? string.Format(BuilderConstants.InputFieldStyleValueTooltipWithDescription, enumAsDash, tip)
                : enumAsDash;
        }

        void RefreshDisplayStrip(ToggleButtonGroup group, DisplayStyle? value = null)
        {
            if (currentVisualElement == null)
                return;

            var current = value ?? currentVisualElement.computedStyle.display;
            var state = new ToggleButtonGroupState(0, 64);
            for (var i = 0; group.GetButton(i) is { } button; ++i)
            {
                if (button.name == DisplayStripName(current))
                {
                    state[i] = true;
                    break;
                }
            }
            group.SetValueWithoutNotify(state);
        }

        static string DisplayStripName(DisplayStyle value)
        {
            foreach (var (name, enumValue, _) in k_DisplayStripButtons)
                if (enumValue == value) return name;
            return "flex";
        }

        void OnDisplayStripChange(ChangeEvent<ToggleButtonGroupState> e, ToggleButtonGroup group)
        {
            if (m_RebuildingDisplayStrip)
                return;

            // No valid edit context (inspector still building): a write here would hit a null style sheet.
            if (currentVisualElement == null || styleSheet == null)
                return;

            var selected = e.newValue.GetActiveOptions(stackalloc int[e.newValue.length]);
            if (selected.IsEmpty)
                return;

            var button = group.GetButton(selected[0]);
            if (button == null)
                return;

            DisplayStyle newValue = DisplayStyle.Flex;
            foreach (var (name, enumValue, _) in k_DisplayStripButtons)
                if (name == button.name) { newValue = enumValue; break; }

            var styleProperty = GetOrCreateStylePropertyByStyleName("display");
            var isNewValue = !styleProperty.HasValue();
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetEnum(styleSheet, newValue);
            PostStyleFieldSteps(group, styleProperty, "display", isNewValue);
        }
    }
}
