// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    /// <summary>
    /// Inspector section listing the UI Toolkit components attached to the selected element. Per component it
    /// shows the <c>[UxmlAttribute]</c> fields (editable, written straight back to the live component) and the
    /// <c>[StyleProperty]</c> fields (read-only resolved values). Lives in the debugger's detail ScrollView
    /// next to the styles section. Reads/writes the live component through VisualElement's debug API.
    /// </summary>
    [NoAutoStaticsCleanup] // the shown-in-debugger cache derives from immutable type attributes
    internal class ComponentsDebugger : UIElementsDebuggerImpl.DebuggerFoldout
    {
        static readonly string k_StyleSourceTooltip = L10n.Tr("Where this style value comes from.", null);
        static readonly string k_ResolvedFromFormat = L10n.Tr("{0} — resolved from {1}", null);

        // Resolves which USS rule sets each component [StyleProperty] (a --custom property), for the source
        // indicator on the read-only style rows.
        readonly MatchedRulesExtractor m_RulesExtractor = new(AssetDatabase.GetAssetPath);
        readonly Dictionary<string, int> m_CustomSpecificity = new();

        // Lightweight-refresh state: per-field actions that re-read the live value into the existing field
        // (so a periodic refresh doesn't rebuild the section and lose focus mid-edit), plus the component
        // types the current layout was built from (to detect when a full rebuild is actually needed).
        readonly List<Action> m_ValueRefreshers = new();
        readonly List<Type> m_BuiltTypes = new();

        public ComponentsDebugger(DebuggerSelection debuggerSelection)
            : base("Components", debuggerSelection, isLowLevel: false) { }

        // Code-only components ([VisualElementComponent(exposeToUxml: false)]) are hidden from the
        // debugger for now. Cached per type: the attribute cannot change at runtime.
        static readonly Dictionary<Type, bool> s_ShownInDebugger = new();

        internal static bool IsShownInDebugger(Type componentType)
        {
            if (s_ShownInDebugger.TryGetValue(componentType, out var shown))
                return shown;

            var attribute = (VisualElementComponentAttribute)Attribute.GetCustomAttribute(
                componentType, typeof(VisualElementComponentAttribute));
            shown = attribute == null || attribute.exposeToUxml;
            s_ShownInDebugger[componentType] = shown;
            return shown;
        }

        // The debugger's view of an element's components: the runtime debug API stays complete,
        // the editor filters code-only components out of every debugger surface.
        internal static IEnumerable<Type> EnumerateShownComponentTypes(VisualElement element)
        {
            foreach (var type in element.EnumerateComponentTypesForDebug())
            {
                if (IsShownInDebugger(type))
                    yield return type;
            }
        }

        static bool HasShownComponents(VisualElement element)
        {
            foreach (var _ in EnumerateShownComponentTypes(element))
                return true;
            return false;
        }

        // Show the section only when the experimental UI Components feature is enabled and the selected
        // element actually carries components.
        protected override void UpdateVisiblity()
        {
            style.display = UIToolkitProjectSettings.enableUIComponents
                && m_SelectedElement != null && HasShownComponents(m_SelectedElement)
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        // Lightweight refresh used by the debugger's periodic update: re-read the live values into the
        // existing fields, and only fall back to a full rebuild when the set of attached components changed.
        // Mirrors DebuggerFoldout.RefreshIfNeeded's "only when open + visible" gate.
        public void RefreshValuesIfNeeded()
        {
            UpdateVisiblity();
            if (!value || style.display != DisplayStyle.Flex)
                return;

            if (ComponentSetChanged())
                Refresh();
            else
                foreach (var refresh in m_ValueRefreshers)
                    refresh();
        }

        // True when the attached components no longer match what the current layout was built from
        // (added/removed/reordered at runtime), so the section must be rebuilt rather than value-refreshed.
        bool ComponentSetChanged()
        {
            var element = m_SelectedElement;
            if (element == null)
                return m_BuiltTypes.Count != 0;

            var i = 0;
            foreach (var type in EnumerateShownComponentTypes(element))
            {
                if (i >= m_BuiltTypes.Count || m_BuiltTypes[i] != type)
                    return true;
                i++;
            }
            return i != m_BuiltTypes.Count;
        }

        protected override void Refresh()
        {
            Clear();
            m_ValueRefreshers.Clear();
            m_BuiltTypes.Clear();
            var element = m_SelectedElement;
            if (element == null)
                return;

            // Which USS rule wins each --custom property, so [StyleProperty] rows can show their source.
            m_RulesExtractor.selectedElementRules.Clear();
            m_RulesExtractor.selectedElementStylesheets.Clear();
            m_RulesExtractor.FindMatchingRules(element);
            StyleDebug.FindSpecifiedCustomProperties(m_RulesExtractor.matchRecords, m_CustomSpecificity);

            // A plain container marked as an inspector element anchors BaseField label alignment for the
            // fields below (the marker goes on a container, not the foldout — putting it on the foldout hides
            // its toggle). The per-component blocks use plain headers, not nested foldouts: a nested foldout's
            // toggle event bubbles to this DebuggerFoldout and triggers a full rebuild, so it never collapses.
            var content = new VisualElement();
            content.AddToClassList("unity-inspector-element");
            foreach (var type in EnumerateShownComponentTypes(element))
            {
                m_BuiltTypes.Add(type);
                content.Add(BuildComponentBlock(element, type));
            }
            Add(content);
        }

        VisualElement BuildComponentBlock(VisualElement element, Type type)
        {
            var block = new VisualElement();
            var header = new Label(type.Name);
            header.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            header.style.marginTop = 4;
            header.style.marginBottom = 2;
            block.Add(header);

            // [UxmlAttribute] fields: the generic editable view, writing back to the live component.
            var description = UxmlSerializedDataRegistry.GetDescription(type.FullName);
            if (description != null)
            {
                var attributesView = new UxmlAttributesDebugView(
                    description,
                    () => ReadComponent(element, type),
                    (attr, value) => CommitAttribute(element, type, attr, value),
                    readOnly: false);
                m_ValueRefreshers.Add(attributesView.Refresh); // lightweight per-field re-read
                block.Add(attributesView);
            }

            // [StyleProperty] fields: read-only resolved values (the specificity indicator is a later pass).
            var box = ReadComponent(element, type);
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var styleProperty = field.GetCustomAttribute<StylePropertyAttribute>();
                if (styleProperty == null || string.IsNullOrEmpty(styleProperty.name))
                    continue;

                // Source indicator: which selector/sheet won this custom property, or "default" when no rule set it.
                var source = m_CustomSpecificity.TryGetValue(styleProperty.name, out var specificity)
                    ? SpecificityToString(specificity)
                    : SpecificityLabels.Default;

                // Label is the field name (aligned with the [UxmlAttribute] fields); the USS name + source go
                // in the tooltip. The field aligns into the same label column as the rest.
                var valueField = new TextField(field.Name) { isReadOnly = true };
                valueField.tooltip = string.Format(k_ResolvedFromFormat, styleProperty.name, source);
                valueField.SetValueWithoutNotify(box != null ? field.GetValue(box)?.ToString() ?? "null" : "");
                valueField.SetEnabled(false);
                valueField.style.flexGrow = 1;
                valueField.AddToClassList(BaseField<int>.alignedFieldUssClassName);

                // Re-read this resolved value on a lightweight refresh (field/valueField are per-iteration).
                m_ValueRefreshers.Add(() =>
                {
                    var current = ReadComponent(element, type);
                    valueField.SetValueWithoutNotify(current != null ? field.GetValue(current)?.ToString() ?? "null" : "");
                });

                var sourceLabel = new Label(source) { tooltip = k_StyleSourceTooltip };
                sourceLabel.style.opacity = 0.6f;
                sourceLabel.style.marginLeft = 4;

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.Add(valueField);
                row.Add(sourceLabel);
                block.Add(row);
            }

            return block;
        }

        static string SpecificityToString(int specificity) => specificity switch
        {
            StyleDebug.UnitySpecificity => SpecificityLabels.UnityStyleSheet,
            StyleDebug.InheritedSpecificity => SpecificityLabels.Inherited,
            StyleDebug.InlineSpecificity => SpecificityLabels.Inline,
            StyleDebug.UndefinedSpecificity => SpecificityLabels.Default,
            _ => SpecificityLabels.Selector,
        };

        // Reads the live value of the component of the given type as a boxed copy, or null if absent.
        static object ReadComponent(VisualElement element, Type type)
        {
            foreach (var (t, value) in element.EnumerateComponentsForDebug())
            {
                if (t == type)
                    return value;
            }
            return null;
        }

        static void CommitAttribute(VisualElement element, Type type, UxmlSerializedAttributeDescription attr, object value)
        {
            // Hold the component boxed as object so SetValueToObject mutates the same instance we write back.
            var box = ReadComponent(element, type);
            if (box == null)
                return;

            attr.SetValueToObject(box, value);
            element.SetComponentForDebug(type, box);

            // Refresh any data bindings to this component field (${component:<Type>}.<field>).
            var fieldName = attr.serializedField?.Name ?? attr.name;
            element.NotifyComponentChanged(new BindingId($"${{component:{type.Name}}}.{fieldName}"));
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
