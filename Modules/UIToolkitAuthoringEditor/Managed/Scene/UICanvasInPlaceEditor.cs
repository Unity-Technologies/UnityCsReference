// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class UICanvasInPlaceEditor
{
    struct EditingContext
    {
        public VisualElement editedElement;
        public TextElement targetTextElement;
        public string uxmlAttributeName;
        public string originalValue;
        public TextField textEditor;
        public IVisualElementScheduledItem scheduledFocusItem;
        public UxmlSerializedAttributeDescription attributeDescription;
        public VisualTreeAsset editedVisualTreeAsset;
        public bool isTemplateInstance;
        public bool isAttributeBound;

        public bool IsValid => editedElement != null;
        public bool CanEdit => targetTextElement?.panel != null
                               && attributeDescription != null;
    }

    internal const string k_TextEditorName = "in-place-text-editor";
    internal const string k_BoundAttributeDisabledTooltipFormat = "Cannot edit in place: the '{0}' attribute is data-bound.";
    const string k_DummyText = " ";
    const string k_TextAttribute = nameof(TextElement.text);

    readonly VisualElement m_Container;
    TextField m_CachedTextField;
    EditingContext m_ActiveContext;

    public UICanvasInPlaceEditor(VisualElement container)
    {
        m_Container = container;
    }

    public bool TryOpenEditorAt(VisualElement element, Vector2 localPosition) => TryOpenEditor(element);

    public bool TryOpenEditor(VisualElement element)
        => HasEditor(element) && OpenEditor(element);

    // Fast type-based check: does this element support in-place editing?
    // Avoids reflection; called on every double-click pick so it must stay cheap.
    public bool HasEditor(VisualElement element)
    {
        if (element == null || element.visualElementAsset == null || element.panel == null)
            return false;

        if (!UIToolkitStageUtility.GetEditFlags(element).CanSetAttribute())
            return false;

        return element is IPrefixLabel
            || element is Foldout
            || element is Tab
            || element is GroupBox
            || element is BaseListView { showFoldoutHeader: true }
            || element is TextElement;
    }

    // Full check: can the editor actually be opened right now?
    // Returns false when the element isn't eligible or its target attribute has an active binding.
    public bool CanOpenEditor(VisualElement element)
    {
        if (!HasEditor(element))
            return false;
        var attrName = GetTargetAttributeName(element);
        if (attrName == null)
            return true;
        var desc = UxmlSerializedDataRegistry.GetDescription(element.GetType().FullName)
            ?.FindAttributeWithUxmlName(attrName);
        return desc == null || !IsAttributeBound(element, desc);
    }

    public bool OpenEditor(VisualElement element)
    {
        if (m_ActiveContext.IsValid)
        {
            if (m_ActiveContext.editedElement == element)
                return false;
            CommitAndClose(m_ActiveContext);
        }

        var ctx = BuildContext(element);

        // Some controls create their label lazily (GroupBox titleLabel, IPrefixLabel labelElement).
        // Force-create it with a dummy value, then rebuild so targetTextElement is attached.
        string preExistingValue = null;
        if (ctx.targetTextElement?.panel == null && ctx.attributeDescription != null)
        {
            preExistingValue = GetTextValue(ctx) ?? string.Empty;
            SetTextValue(ctx, k_DummyText);
            ctx = BuildContext(element);
        }

        var vea = element.visualElementAsset;
        if (!ctx.CanEdit || vea == null || vea.visualTreeAsset == null)
        {
            if (preExistingValue != null)
                SetTextValue(ctx, preExistingValue);
            return false;
        }

        if (ctx.isAttributeBound)
        {
            if (preExistingValue != null)
                SetTextValue(ctx, preExistingValue);
            NotifyBoundAttribute(ctx.uxmlAttributeName);
            return false;
        }

        if (!ctx.isTemplateInstance && vea.serializedData == null)
        {
            var dataDescription = UxmlSerializedDataRegistry.GetDescription(vea.fullTypeName);
            if (dataDescription == null)
            {
                if (preExistingValue != null)
                    SetTextValue(ctx, preExistingValue);
                return false;
            }
            vea.serializedData = dataDescription.CreateDefaultSerializedData();
            vea.serializedData.uxmlAssetId = vea.id;
        }

        var currentValue = preExistingValue ?? GetTextValue(ctx) ?? string.Empty;
        ctx.originalValue = currentValue;

        if (string.IsNullOrEmpty(currentValue))
            SetTextValue(ctx, k_DummyText);

        ctx.textEditor = CreateTextField();
        m_Container.hierarchy.Add(ctx.textEditor);

        m_ActiveContext = ctx;

        UpdateEditorInternal(ctx);

        ctx.textEditor.style.display = DisplayStyle.Flex;
        ctx.textEditor.SetValueWithoutNotify(currentValue);
        m_ActiveContext.scheduledFocusItem = ctx.textEditor.schedule.Execute(_ =>
        {
            ctx.textEditor.Focus();
            ctx.textEditor.textSelection.SelectAll();
        });

        var textInput = ctx.textEditor.Q(TextField.textInputUssName);
        textInput.RegisterCallback<FocusOutEvent, EditingContext>(OnFocusOut, ctx, TrickleDown.TrickleDown);
        textInput.RegisterCallback<KeyDownEvent, EditingContext>(OnKeyDown, ctx, TrickleDown.TrickleDown);
        ctx.textEditor.RegisterCallback<ChangeEvent<string>, EditingContext>(OnTextChanged, ctx);
        element.RegisterCallback<DetachFromPanelEvent, EditingContext>(OnDetachFromPanel, ctx, TrickleDown.TrickleDown);
        ctx.targetTextElement?.RegisterCallback<GeometryChangedEvent, EditingContext>(OnGeometryChanged, ctx);

        return true;
    }

    public void UpdateEditor()
    {
        if (m_ActiveContext.IsValid)
            UpdateEditorInternal(m_ActiveContext);
    }

    internal void CancelEditor()
    {
        if (m_ActiveContext.IsValid)
            CancelAndClose(m_ActiveContext);
    }

    TextField CreateTextField()
    {
        if (m_CachedTextField == null)
        {
            m_CachedTextField = new TextField { name = k_TextEditorName, multiline = true };
            m_CachedTextField.style.position = Position.Absolute;
            m_CachedTextField.style.display = DisplayStyle.None;
            m_CachedTextField.style.backgroundColor = StyleKeyword.None;
            m_CachedTextField.labelElement.style.display = DisplayStyle.None;
        }
        return m_CachedTextField;
    }

    void NotifyBoundAttribute(string attributeName)
    {
        var message = string.Format(k_BoundAttributeDisabledTooltipFormat, attributeName);
        using var evt = CanvasManipulatorMessageEvent.GetPooled(message);
        evt.target = m_Container;
        m_Container.SendEvent(evt);
    }

    void CloseEditor(in EditingContext ctx)
    {
        ctx.scheduledFocusItem?.Pause();

        var textInput = ctx.textEditor.Q(TextField.textInputUssName);
        textInput.UnregisterCallback<FocusOutEvent, EditingContext>(OnFocusOut, TrickleDown.TrickleDown);
        textInput.UnregisterCallback<KeyDownEvent, EditingContext>(OnKeyDown, TrickleDown.TrickleDown);
        ctx.textEditor.UnregisterCallback<ChangeEvent<string>, EditingContext>(OnTextChanged);
        ctx.editedElement?.UnregisterCallback<DetachFromPanelEvent, EditingContext>(OnDetachFromPanel, TrickleDown.TrickleDown);
        ctx.targetTextElement?.UnregisterCallback<GeometryChangedEvent, EditingContext>(OnGeometryChanged);
        ctx.textEditor.textSelection.cursorIndex = 0;
        ctx.textEditor.textSelection.selectIndex = 0;
        ctx.textEditor.style.display = DisplayStyle.None;
        ctx.textEditor.RemoveFromHierarchy();
    }

    void CommitAndClose(in EditingContext ctx)
    {
        if (!ctx.CanEdit)
        {
            CloseEditor(ctx);
            m_ActiveContext = default;
            return;
        }

        var vea = ctx.editedElement?.visualElementAsset;
        if (vea == null)
        {
            CloseEditor(ctx);
            m_ActiveContext = default;
            return;
        }

        // Compare against the resolved value captured when the editor opened so that attribute overrides
        // on template-instance elements are not treated as changes by the raw VEA string attribute.
        var newValue = ctx.textEditor.value;
        if (newValue == ctx.originalValue)
        {
            SetTextValue(ctx, ctx.originalValue);
            CloseEditor(ctx);
            m_ActiveContext = default;
            return;
        }

        // Sync the live element before the command so the dummy-text placeholder doesn't linger.
        SetTextValue(ctx, newValue);

        if (ctx.isTemplateInstance)
        {
            if (ctx.editedVisualTreeAsset != null)
            {
                SetAttributeOverrideCommand.Execute(
                    CommandSources.Viewport,
                    ctx.editedVisualTreeAsset,
                    ctx.attributeDescription,
                    ctx.editedElement,
                    newValue);
            }
        }
        else
        {
            var vta = vea.visualTreeAsset;
            if (vta != null)
            {
                SetAttributeCommand.Execute(
                    CommandSources.Viewport,
                    vta,
                    vea,
                    vea.serializedData,
                    ctx.attributeDescription,
                    newValue);
            }
        }

        CloseEditor(ctx);
        m_ActiveContext = default;
    }

    void CancelAndClose(in EditingContext ctx)
    {
        if (ctx.CanEdit)
            SetTextValue(ctx, ctx.originalValue);
        CloseEditor(ctx);
        m_ActiveContext = default;
    }

    void UpdateEditorInternal(in EditingContext ctx)
    {
        ApplyTextStyles(ctx);
        UpdateTextEditorGeometry(ctx);
    }

    static void UpdateTextEditorGeometry(in EditingContext ctx)
    {
        if (!ctx.CanEdit)
            return;

        var ppp = ((Panel)ctx.targetTextElement.panel)?.pixelsPerPoint ?? 1f;
        var layout = ctx.targetTextElement.layout;
        var rs = ctx.targetTextElement.resolvedStyle;

        // worldTransform encodes all accumulated transforms (zoom, ancestor CSS rotate/scale, element's own transform).
        var wt = ctx.targetTextElement.worldTransform;
        var origin = wt.MultiplyPoint3x4(new Vector3(rs.borderLeftWidth, rs.borderTopWidth, 0));

        ctx.textEditor.style.left = origin.x / ppp;
        ctx.textEditor.style.top = origin.y / ppp;
        ctx.textEditor.style.rotate = new Rotate(new Angle(Mathf.Atan2(wt.m10, wt.m00) * Mathf.Rad2Deg, AngleUnit.Degree));
        ctx.textEditor.style.scale = new Scale(Vector3.one);
        ctx.textEditor.style.transformOrigin = new TransformOrigin(Length.Pixels(0), Length.Pixels(0), 0);

        var textInput = ctx.textEditor.Q(TextField.textInputUssName);
        textInput.style.width = wt.MultiplyVector(
            new Vector3(layout.width - rs.borderLeftWidth - rs.borderRightWidth, 0, 0)).magnitude / ppp;
        textInput.style.height = wt.MultiplyVector(
            new Vector3(0, layout.height - rs.borderTopWidth - rs.borderBottomWidth, 0)).magnitude / ppp;
    }

    static void ApplyTextStyles(in EditingContext ctx)
    {
        var src = ctx.targetTextElement.computedStyle;
        var textInput = ctx.textEditor.Q(TextField.textInputUssName);

        // worldTransform magnitude gives the total scale (zoom + CSS scale); dividing by ppp converts to overlay CSS pixels.
        var wt = ctx.targetTextElement.worldTransform;
        var ppp = ((Panel)ctx.targetTextElement.panel)?.pixelsPerPoint ?? 1f;
        var parentScale = wt.MultiplyVector(Vector3.up).magnitude / ppp;

        textInput.style.unityTextAlign = src.unityTextAlign;
        textInput.style.fontSize = new Length(src.fontSize * parentScale, LengthUnit.Pixel);
        textInput.style.unityFontStyleAndWeight = src.unityFontStyleAndWeight;
        textInput.style.whiteSpace = src.whiteSpace;
        textInput.style.letterSpacing = new Length(src.letterSpacing.value * parentScale, src.letterSpacing.unit);
        textInput.style.wordSpacing = new Length(src.wordSpacing.value * parentScale, src.wordSpacing.unit);
        textInput.style.unityParagraphSpacing = new Length(src.unityParagraphSpacing.value * parentScale, src.unityParagraphSpacing.unit);
        textInput.style.textOverflow = src.textOverflow;
        textInput.style.overflow = src.overflow == OverflowInternal.Visible ? Overflow.Visible : Overflow.Hidden;
        textInput.style.unityFont = (Font)Resources.EntityIdToObject(src.unityFont);
        textInput.style.unityFontDefinition = FontDefinition.From(src.unityFontDefinition);

        // Zero margins/borders on the wrapper; mirror target padding so the cursor aligns with the element's text.
        ctx.textEditor.style.marginTop = 0;
        ctx.textEditor.style.marginBottom = 0;
        ctx.textEditor.style.marginLeft = 0;
        ctx.textEditor.style.marginRight = 0;
        ctx.textEditor.style.paddingTop = 0;
        ctx.textEditor.style.paddingBottom = 0;
        ctx.textEditor.style.paddingLeft = 0;
        ctx.textEditor.style.paddingRight = 0;
        ctx.textEditor.style.borderTopWidth = 0;
        ctx.textEditor.style.borderBottomWidth = 0;
        ctx.textEditor.style.borderLeftWidth = 0;
        ctx.textEditor.style.borderRightWidth = 0;

        textInput.style.paddingTop = new Length(src.paddingTop.value * parentScale, LengthUnit.Pixel);
        textInput.style.paddingBottom = new Length(src.paddingBottom.value * parentScale, LengthUnit.Pixel);
        textInput.style.paddingLeft = new Length(src.paddingLeft.value * parentScale, LengthUnit.Pixel);
        textInput.style.paddingRight = new Length(src.paddingRight.value * parentScale, LengthUnit.Pixel);
        textInput.style.borderTopWidth = 0;
        textInput.style.borderBottomWidth = 0;
        textInput.style.borderLeftWidth = 0;
        textInput.style.borderRightWidth = 0;
        textInput.style.marginTop = 0;
        textInput.style.marginBottom = 0;
        textInput.style.marginLeft = 0;
        textInput.style.marginRight = 0;
        // Override USS min-width/height so the text input does not exceed the explicitly set dimensions.
        textInput.style.minWidth = 0;
        textInput.style.minHeight = 0;
    }

    // VEA is only written on commit; live element updates during typing keep auto-sized elements responsive.
    static void OnTextChanged(ChangeEvent<string> evt, EditingContext ctx)
    {
        var previewValue = string.IsNullOrEmpty(evt.newValue) ? k_DummyText : evt.newValue;
        SetTextValue(ctx, previewValue);
    }

    void OnFocusOut(FocusOutEvent evt, EditingContext ctx)
    {
        CommitAndClose(ctx);
    }

    void OnKeyDown(KeyDownEvent evt, EditingContext ctx)
    {
        if (evt.keyCode != KeyCode.Escape)
            return;
        CancelAndClose(ctx);
        evt.StopPropagation();
    }

    void OnDetachFromPanel(DetachFromPanelEvent evt, EditingContext ctx)
    {
        CloseEditor(ctx);
        m_ActiveContext = default;
    }

    static void OnGeometryChanged(GeometryChangedEvent evt, EditingContext ctx)
    {
        if (!ctx.CanEdit)
            return;
        UpdateTextEditorGeometry(ctx);
    }

    static string GetTextValue(in EditingContext ctx)
    {
        if (ctx.attributeDescription == null)
            return null;
        ctx.attributeDescription.TryGetValueFromObject(ctx.editedElement, out var val);
        return val as string;
    }

    static void SetTextValue(in EditingContext ctx, string value)
        => ctx.attributeDescription?.SetValueToObject(ctx.editedElement, value);

    static EditingContext BuildContext(VisualElement element)
    {
        var stage = StageUtility.GetCurrentStage() as VisualElementEditingStage;
        var editedVta = stage != null ? stage.EditedVisualTreeAsset : element.visualTreeAssetSource;
        var ctx = new EditingContext
        {
            editedElement = element,
            editedVisualTreeAsset = editedVta,
            isTemplateInstance = element.templateAsset != null && editedVta != element.visualTreeAssetSource,
        };

        ctx.uxmlAttributeName = GetTargetAttributeName(element);

        switch (element)
        {
            case IPrefixLabel prefixLabel:
                ctx.targetTextElement = prefixLabel.labelElement;
                break;
            case Foldout foldout:
                ctx.targetTextElement = foldout.toggle.boolFieldLabelElement;
                break;
            case Tab tab:
                ctx.targetTextElement = tab.headerLabel;
                break;
            case GroupBox groupBox:
                ctx.targetTextElement = groupBox.titleLabel;
                break;
            case BaseListView { showFoldoutHeader: true } listView:
                ctx.targetTextElement = listView.headerFoldout.toggle.boolFieldLabelElement;
                break;
            case TextElement textElement:
                ctx.targetTextElement = textElement;
                break;
        }

        if (!string.IsNullOrEmpty(ctx.uxmlAttributeName))
        {
            ctx.attributeDescription = UxmlSerializedDataRegistry
                .GetDescription(element.GetType().FullName)
                ?.FindAttributeWithUxmlName(ctx.uxmlAttributeName);

            if (ctx.attributeDescription != null)
                ctx.isAttributeBound = IsAttributeBound(element, ctx.attributeDescription);
        }

        return ctx;
    }

    static string GetTargetAttributeName(VisualElement element) => element switch
    {
        IPrefixLabel => nameof(IPrefixLabel.label),
        Foldout => k_TextAttribute,
        Tab => nameof(Tab.label),
        GroupBox => k_TextAttribute,
        BaseListView { showFoldoutHeader: true } => "header-title",
        TextElement => k_TextAttribute,
        _ => null
    };

static bool IsAttributeBound(VisualElement element, UxmlSerializedAttributeDescription attributeDescription)
    {
        var bindingId = new BindingId(attributeDescription.bindingPath);
        return element.TryGetLastBindingToUIResult(bindingId, out var result)
            && result.status == BindingStatus.Success;
    }
}
