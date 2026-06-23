// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
sealed partial class MatchingSelectorsElement : VisualElement, IVisualElementChangeProcessor
{
    sealed class MatchingSelectorElement : Foldout
    {
        static UnityEngine.Pool.ObjectPool<TextField> s_PropertyPool = new(CreateTextField, null, OnReleaseTextField);

        internal const string ClassName = "unity-matching-selector-element";
        internal const string InputClassName = ClassName + "__input";

        static TextField CreateTextField()
        {
            var field = new TextField { isReadOnly = true };
            field.AddToClassList(TextField.alignedFieldUssClassName);
            field.AddToClassList(ClassName);
            field.textInputBase.AddToClassList(InputClassName);
            return field;
        }

        static void OnReleaseTextField(TextField field) => field.value = null;

        string m_SelectorName;
        StyleRule m_Rule;
        StyleProperty[] m_Properties;
        int m_Index;

        StyleSheetExporter.UssExportOptions Options
        {
            get
            {
                var options = StyleSheetExporter.UssExportOptions.Default;
                options.useColorHighlighting = true;
                return options;
            }
        }

        public MatchingSelectorElement()
        {
            // We need an actual label to set the following line. We revert it at the end.
            text = "label";
            toggle.boolFieldLabelElement.selection.isSelectable = true;
            toggle.toggleOnLabelClick = false;
            toggle.toggleOnTextClick = false;
            text = null;
        }

        public void SetSelector(StyleComplexSelector selector, HashSet<string> setProperties)
        {
            m_Rule = selector.rule;
            m_SelectorName = StyleSheetExporter.Default.ToUssString(m_Rule.styleSheet, selector, Options);
            m_Properties = m_Rule.properties;
            m_Index = selector.ruleIndex;
            RefreshProperties(setProperties);
        }

        public void SetInlineRule(InlineStyleAccess.InlineRule inlineRule, HashSet<string> setProperties)
        {
            m_Rule = inlineRule.rule;
            m_SelectorName = $"<color=#{ColorUtility.ToHtmlStringRGB(StyleSheetExporter.SelectorTypeColor)}>Inline Rule</color>";
            m_Properties = inlineRule.properties;
            m_Index = -1;
            RefreshProperties(setProperties);
        }

        public void Reset()
        {
            m_SelectorName = null;
            m_Rule = null;
            m_Properties = null;
            m_Index = int.MinValue;
        }

        void RefreshProperties(HashSet<string> setProperties)
        {
            text = m_SelectorName;
            viewDataKey = "inspector-rule-foldout__" + m_Index;

            var rule = m_Rule;
            var styleSheet = rule?.styleSheet;
            var properties = m_Properties;
            Resize(this, properties?.Length ?? 0, s_PropertyPool);

            if (properties == null)
                return;

            for (var i = 0; i < properties.Length; ++i)
            {
                var property = properties[i];
                var field = (TextField)this[i];
                var propertyName = property.name;
                field.labelElement.enableRichText = true;
                field.textInputBase.textElement.enableRichText = true;


                if (setProperties.Add(propertyName))
                {
                    field.label = $"<color=#{ColorUtility.ToHtmlStringRGB(StyleSheetExporter.PropertyColor)}>{propertyName}</color>";
                    field.value = StyleSheetExporter.GetStylePropertyValueString(styleSheet, property, Options);
                }
                else
                {
                    field.label = $"<s><color=#{ColorUtility.ToHtmlStringRGB(StyleSheetExporter.PropertyColor)}>{property.name}</color></s>";
                    field.value = $"<s>{StyleSheetExporter.GetStylePropertyValueString(styleSheet, property, Options)}</s>";
                }
            }
        }
    }

    static UnityEngine.Pool.ObjectPool<MatchingSelectorElement> s_MatchingSelectorPool = new(CreateMatchingSelector, null, OnReleaseMatchingSelector);

    static MatchingSelectorElement CreateMatchingSelector() => new ();

    static void OnReleaseMatchingSelector(MatchingSelectorElement field) => field.Reset();

    readonly MatchedRulesExtractor m_MatchedRulesExtractor;
    readonly VisualElement m_MatchingSelectorContainer;
    VisualElement m_Target;

    public VisualElement Target
    {
        get => m_Target;
        set
        {
            if (m_Target == value)
                return;
            Release(m_Target);
            m_Target = value;
            Acquire(m_Target);
            Refresh();
        }
    }

    public override VisualElement contentContainer => null;

    public MatchingSelectorsElement()
    {
        m_MatchedRulesExtractor = new (AssetDatabase.GetAssetPath);
        m_MatchingSelectorContainer = new();
        hierarchy.Add(m_MatchingSelectorContainer);
    }

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent:
                PrefSettings.settingChanged += OnPrefsChanged;
                break;
            case DetachFromPanelEvent:
                PrefSettings.settingChanged -= OnPrefsChanged;
                break;
        }
        base.HandleEventBubbleUp(evt);
    }

    void OnPrefsChanged(string prefName, Type prefType)
    {
        if (prefName.StartsWith(StyleSheetExporter.ColorsPreferenceCategory, StringComparison.Ordinal))
            Refresh();
    }

    void Release(VisualElement element)
    {
        if (element == null)
            return;

        element.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        element.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

        if (element.elementPanel == null)
            return;

        element.elementPanel.UnregisterChangeProcessor(this);
    }

    void Acquire(VisualElement element)
    {
        if (element == null)
            return;

        element.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        element.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

        if (element.elementPanel == null)
            return;

        element.elementPanel.RegisterChangeProcessor(this);
    }

    void OnAttachToPanel(AttachToPanelEvent evt)
    {
        m_Target.elementPanel.RegisterChangeProcessor(this);
        Refresh();
    }

    void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        m_Target.elementPanel.UnregisterChangeProcessor(this);
        Resize(m_MatchingSelectorContainer, 0, s_MatchingSelectorPool);
    }

    void IVisualElementChangeProcessor.BeginProcessing(BaseVisualElementPanel p)
    {
    }

    void IVisualElementChangeProcessor.ProcessChanges(BaseVisualElementPanel p, AuthoringChanges changes)
    {
        if (!changes.stylingContextChanged.Contains(m_Target) &&
            !changes.styleChanged.Contains(m_Target))
            return;

        Refresh();
    }

    void IVisualElementChangeProcessor.EndProcessing(BaseVisualElementPanel p)
    {
    }

    void Refresh()
    {
        m_MatchedRulesExtractor.Clear();
        if (m_Target != null)
            m_MatchedRulesExtractor.FindMatchingRules(m_Target);

        var matchedSelectors = m_MatchedRulesExtractor.matchRecords;

        var hasInlineRule = HasInlineRule(m_Target);
        var count = matchedSelectors.Count + (hasInlineRule? 1 : 0);

        Resize(m_MatchingSelectorContainer, count, s_MatchingSelectorPool);

        using var _ = HashSetPool<string>.Get(out var setProperties);

        if (hasInlineRule)
        {
            var field = (MatchingSelectorElement) m_MatchingSelectorContainer[m_MatchingSelectorContainer.childCount-1];
            field.SetInlineRule(m_Target.inlineStyleAccess.inlineRule, setProperties);
        }

        for (var i = matchedSelectors.Count - 1; i >=0 ; --i)
        {
            var field = (MatchingSelectorElement) m_MatchingSelectorContainer[i];
            field.SetSelector(matchedSelectors[i].complexSelector, setProperties);
        }
    }

    static bool HasInlineRule(VisualElement element)
    {
        if (element == null)
            return false;
        if (!element.hasInlineStyle)
            return false;
        var inlineRule = element.inlineStyleAccess.inlineRule;
        return inlineRule.properties?.Length > 0;
    }

    static void Resize<T>(VisualElement element, int count, UnityEngine.Pool.ObjectPool<T> pool)
        where T: VisualElement, new()
    {
        if (element.childCount < count)
        {
            var toAdd = count - element.childCount;
            for (var i = 0; i < toAdd; ++i)
            {
                var field = pool.Get();
                element.Add(field);
            }

        }
        else if (element.childCount > count)
        {
            var toRemove = element.childCount - count;
            for (var i = 0; i < toRemove; ++i)
            {
                var index = element.childCount - 1;
                pool.Release((T)element[index]);
                element.RemoveAt(index);
            }
        }
    }
}
