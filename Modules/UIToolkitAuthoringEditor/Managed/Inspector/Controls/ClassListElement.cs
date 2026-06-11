// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
sealed partial class ClassListElement : VisualElement, IVisualElementChangeProcessor
{
    static UnityEngine.Pool.ObjectPool<ClassPill> s_PillPool = new(CreatePill, null, OnReleasePill);

    static ClassPill CreatePill()
    {
        var pill = new ClassPill();
        pill.AddToClassList(SelectorLabelClassName);
        pill.AddManipulator(CreateClassPillDoubleClickManipulator());
        pill.RegisterCallback<TooltipEvent, ClassPill>(OnClassPillTooltip, pill);
        return pill;
    }

    static void OnReleasePill(ClassPill pill)
    {
        pill.text = null;
        pill.canBeRemoved = false;
        pill.SetDeleteButtonUserData(null);
        pill.ClearProperty(TargetClassPillProperty);
    }

    const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/Controls/ClassListElement.uxml";
    const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/Controls/ClassListElementDark.uss";
    const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/Controls/ClassListElementLight.uss";

    public const string UssClass = "unity-class-list-element";
    public const string InputFieldUssClass = UssClass + "__input";
    public const string ValidationHelpBoxUssClass = UssClass + "__validation-help-box";
    public const string AddClassButtonUssClass = UssClass + "__add-class-button";
    public const string ExtractInlineStylesButtonUssClass = UssClass + "__extract-inline-styles-button";
    public const string ClassListContainerUssClass = UssClass + "__class-list-container";
    public const string SelectorLabelClassName = "unity-selector-label";
    const string TargetClassPillProperty = "unity-class-pill__target";

    readonly TextField m_InputTextField;
    readonly HelpBox m_ValidationHelpBox;
    readonly Button m_AddClassButton;
    readonly Button m_ExtractInlineStylesButton;
    readonly VisualElement m_ClassListContainer;

    VisualElement m_Target;
    bool m_IsReadOnly;

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
            RefreshClassList();
        }
    }

    public bool IsReadOnly
    {
        get => m_IsReadOnly;
        set
        {
            if (m_IsReadOnly == value)
                return;
            m_IsReadOnly = value;
            enabledSelf = !m_IsReadOnly;
        }
    }

    public ClassListElement()
    {
        AddToClassList(UssClass);

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(this);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        styleSheets.Add(styleSheet);

        m_InputTextField = this.Q<TextField>(className: InputFieldUssClass);
        m_InputTextField.RegisterCallback<KeyUpEvent>(OnClassNameSubmitted);
        m_ValidationHelpBox = this.Q<HelpBox>(className: ValidationHelpBoxUssClass);
        m_ValidationHelpBox.style.display = DisplayStyle.None;
        m_AddClassButton = this.Q<Button>(className: AddClassButtonUssClass);
        m_AddClassButton.clicked += OnAddClassToElement;
        m_ExtractInlineStylesButton = this.Q<Button>(className: ExtractInlineStylesButtonUssClass);
        m_ExtractInlineStylesButton.clicked += OnExtractLocalStylesToNewClass;
        m_ClassListContainer = this.Q(className: ClassListContainerUssClass);
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
        if (evt.destinationPanel is Panel p)
            p.RegisterChangeProcessor(this);
        RefreshClassList();
    }

    void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        if (evt.originPanel is Panel p)
            p.UnregisterChangeProcessor(this);
        RefreshClassList();
    }

    void IVisualElementChangeProcessor.BeginProcessing(BaseVisualElementPanel p)
    {
    }

    void IVisualElementChangeProcessor.ProcessChanges(BaseVisualElementPanel p, AuthoringChanges changes)
    {
        if (!changes.stylingContextChanged.Contains(m_Target))
            return;

        RefreshClassList();
    }

    void IVisualElementChangeProcessor.EndProcessing(BaseVisualElementPanel p)
    {
    }

    void RefreshClassList()
    {
        if (Target == null)
        {
            ResizeClassList(m_ClassListContainer, 0, s_PillPool);
            return;
        }

        using var _ = ListPool<string>.Get(out var classes);
        classes.AddRange(Target.GetClasses());

        ResizeClassList(m_ClassListContainer, classes.Count, s_PillPool);

        for (var i = 0; i < classes.Count; i++)
        {
            var className = classes[i];
            var pillText = "." + className;

            var pill = (ClassPill)m_ClassListContainer[i];

            pill.text = pillText;
            pill.userData = className;
            pill.SetProperty(TargetClassPillProperty, m_Target);
            pill.SetDeleteButtonUserData(className);

            pill.canBeRemoved = IsClassInUxmlDoc(className);

            pill.simpleSelectorExists = IsClassSelectorInAnyStyleSheet(m_Target, className);
            pill.onDeleteClickable.clickedWithEventInfo -= OnStyleClassDelete;
            if (pill.canBeRemoved)
                pill.onDeleteClickable.clickedWithEventInfo += OnStyleClassDelete;
        }
    }

    void OnClassNameSubmitted(KeyUpEvent evt)
    {
        var className = m_InputTextField.value?.Trim().TrimStart('.');

        m_ValidationHelpBox.style.display = string.IsNullOrEmpty(className) || VerifyNewClassNameIsValid(className) ? DisplayStyle.None : DisplayStyle.Flex;

        if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter))
            return;

        OnAddClassToElement();
        evt.StopPropagation();
    }

    void OnAddClassToElement()
    {
        var className = m_InputTextField.value?.Trim().TrimStart('.');
        try
        {
            if (string.IsNullOrEmpty(className))
                return;

            var elementAsset = Target?.visualElementAsset;
            if (elementAsset == null)
                return;

            if (!VerifyNewClassNameIsValid(className))
                return;

            AddClassCommand.Execute(CommandSources.Inspector, elementAsset, className);

            m_InputTextField.SetValueWithoutNotify(string.Empty);
        }
        finally
        {
            m_InputTextField.visualInput.Focus();
            RefreshClassList();
        }
    }

    void OnExtractLocalStylesToNewClass()
    {
        var className = m_InputTextField.value?.Trim().TrimStart('.');

        var target = Target;
        var vea = target?.visualElementAsset;
        var vta = vea?.visualTreeAsset;

        try{
            if (target == null || vea == null || vta == null)
                return;

            if (!VerifyNewClassNameIsValid(className))
                return;

            using (var _ = UICommandQueue.BeginGroup(ExtractInlineStylesToNewClassCommand.CommandUndoName))
            {
                var activeStyleSheet = GetActiveStyleSheetQuery.Get();
                if (activeStyleSheet == null)
                {
                    var ussPath = StyleSheetAssetUtilities.DisplaySaveFileDialogForUSS();
                    if (string.IsNullOrEmpty(ussPath))
                        return;

                    CreateStyleSheetCommand.Execute(CommandSources.Inspector, vta, ussPath);
                    activeStyleSheet = GetActiveStyleSheetQuery.Get();
                }

                if (activeStyleSheet == null)
                    return;

                ExtractInlineStylesToNewClassCommand.Execute(
                    CommandSources.Inspector,
                    vea,
                    vta,
                    activeStyleSheet,
                    className);
                AddClassCommand.Execute(CommandSources.Inspector, vea, className);
            }
        }
        finally
        {
            m_InputTextField.SetValueWithoutNotify(string.Empty);
        }
    }

    bool VerifyNewClassNameIsValid(string className)
    {
        if (string.IsNullOrEmpty(className))
            return false;

        var error = StyleSheetAssetUtilities.GetClassNameValidationError(className);
        if (!string.IsNullOrEmpty(error))
        {
            m_ValidationHelpBox.messageType = HelpBoxMessageType.Warning;
            m_ValidationHelpBox.text = error;
            return false;
        }

        return true;
    }

    bool IsClassInUxmlDoc(string className)
    {
        var vea = Target?.visualElementAsset;
        if (vea?.classes == null)
            return false;

        return Array.IndexOf(vea.classes, className) >= 0;
    }

    bool IsClassSelectorInAnyStyleSheet(VisualElement target , string className)
    {
        var vta = target?.visualElementAsset?.visualTreeAsset;
        if (vta == null)
            return false;

        using var _ = ListPool<StyleSheet>.Get(out var sheets);
        vta.GetAllReferencedStyleSheets(sheets);
        return StyleSheetUtility.TryGetFirstRuleWithSimpleClassSelector(sheets, className, out var _);
    }

    void OnStyleClassDelete(EventBase evt)
    {
        var className = evt.elementTarget.userData as string;
        if (string.IsNullOrEmpty(className))
            return;

        Target.RemoveFromClassList(className);
        RemoveClassFromElementCommand.Execute(CommandSources.Inspector, Target.visualElementAsset, className);
        evt.StopPropagation();
    }

    static Clickable CreateClassPillDoubleClickManipulator()
    {
        var clickable = new Clickable(OnClassPillDoubleClick);
        var activator = clickable.activators[0];
        activator.clickCount = 2;
        clickable.activators[0] = activator;
        return clickable;
    }

    static void OnClassPillDoubleClick(EventBase evt)
    {
        var pill = evt.currentTarget as ClassPill;
        var className = pill?.userData as string;
        var target = (VisualElement)pill?.GetProperty(TargetClassPillProperty);
        if (string.IsNullOrEmpty(className))
            return;

        var vta = target?.visualTreeAssetSource;
        if (vta == null)
            return;

        using var ssHandle = ListPool<StyleSheet>.Get(out var sheets);
        vta.GetAllReferencedStyleSheets(sheets);

        if (StyleSheetUtility.TryGetFirstRuleWithSimpleClassSelector(sheets, className, out var rule))
        {
            using var selectRuleCommand = RequestSelectionQuery<StyleRule>.GetPooled(CommandSources.Inspector, rule);
            UICommandQueue.Execute(selectRuleCommand);
            return;
        }

        var activeStyleSheet = GetActiveStyleSheetQuery.Get();
        if (activeStyleSheet == null)
        {
            var ussPath = StyleSheetAssetUtilities.DisplaySaveFileDialogForUSS();
            if (string.IsNullOrEmpty(ussPath))
                return;

            CreateStyleSheetCommand.Execute(CommandSources.Inspector, vta, ussPath);
            activeStyleSheet = GetActiveStyleSheetQuery.Get();
        }

        if (activeStyleSheet == null)
            return;

        var selectorString = "." + className;
        if (!StyleSheetExtensions.ValidateSelector(selectorString, out var error))
        {
            Debug.LogError($"Invalid selector string '{selectorString}': {error}.");
            return;
        }

        AddStyleRuleCommand.Execute(CommandSources.Inspector, sheets[0], selectorString);

        RequestSelectionQuery<StyleRule>.Execute(CommandSources.Inspector, sheets[0].rules[^1]);
    }

    void ResizeClassList(VisualElement element, int count, UnityEngine.Pool.ObjectPool<ClassPill> pool)
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
                var pill = (ClassPill)element[index];
                pill.onDeleteClickable.clickedWithEventInfo -= OnStyleClassDelete;
                pool.Release(pill);
                element.RemoveAt(index);
            }
        }
    }

    static void OnClassPillTooltip(TooltipEvent evt, ClassPill pill)
    {
        evt.rect = pill.worldBound;
        evt.tooltip = pill.simpleSelectorExists
            ? $"{pill.text}\n\nDouble-click to select and edit USS selector."
            : $"{pill.text}\n\nDouble-click to create new USS selector.";
        evt.StopPropagation();
    }
}
