// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Debug = UnityEngine.Debug;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal enum TrackedPropertyType
{
    MarkOverride,
    ClearOverride,
    StopTracking,
}

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal interface ITrackablePropertyProvider
{
    event Action<ITrackablePropertyProvider, string, TrackedPropertyType> OnTrackedPropertyChanged;
    event Action<ITrackablePropertyProvider, string, bool, bool> OnTrackedPropertySourceChanged;
}

internal interface INotifyCompositeStylePropertyChanged<in TValue>
{
    void SetValue(BindingId id, TValue v, bool notify);

    void NotifyStylePropertyChanged(BindingId id, TValue previousValue, TValue newValue);
}

internal static class NotifyCompositeStylePropertyChangedExtensions
{
    public static void NotifyStylePropertyChanged<TValue>(this INotifyCompositeStylePropertyChanged<TValue> self, VisualElement element, BindingId id, TValue previousValue, TValue newValue)
    {
        var evt = CompositeStylePropertyChangeEvent<TValue>.GetPooled(id, previousValue, newValue);
        evt.target = element;
        element.SendEvent(evt);
    }
}

class CompositeStylePropertyChangeEvent<T> : EventBase<CompositeStylePropertyChangeEvent<T>>, IChangeEvent
{
    static CompositeStylePropertyChangeEvent()
    {
        SetCreateFunction(() => new CompositeStylePropertyChangeEvent<T>());
    }

    public BindingId Id { get; protected set; }
    public T PreviousValue { get; protected set; }
    public T NewValue { get; protected set; }

    protected override void Init()
    {
        base.Init();
        LocalInit();
    }

    void LocalInit()
    {
        bubbles = false;
        tricklesDown = false;
        PreviousValue = default(T);
        NewValue = default(T);
    }

    public static CompositeStylePropertyChangeEvent<T> GetPooled(BindingId id, T previousValue, T newValue)
    {
        CompositeStylePropertyChangeEvent<T> e = GetPooled();
        e.Id = id;
        e.PreviousValue = previousValue;
        e.NewValue = newValue;
        return e;
    }

    public CompositeStylePropertyChangeEvent()
    {
        LocalInit();
    }
}

[UxmlObject]
sealed partial class StylePropertyBinding : CustomBinding, ITrackablePropertyProvider, IDataSourceProvider
{
    [Flags]
    enum UpdateFlags
    {
        None = 0,
        IgnoreChanges = 1,
    }

    readonly struct IgnoreChangeScope : IDisposable
    {
        readonly StylePropertyBinding m_Binding;
        readonly bool m_WasIgnoringChanges;

        public IgnoreChangeScope(StylePropertyBinding binding)
        {
            m_Binding = binding;
            m_WasIgnoringChanges = m_Binding.ignoreChanges;
            m_Binding.ignoreChanges = true;
        }

        public void Dispose()
        {
            m_Binding.ignoreChanges = m_WasIgnoringChanges;
        }
    }

    readonly record struct CallbackContext(StylePropertyBinding binding, VisualElement element, BindingId bindingId)
    {
        public readonly StylePropertyBinding binding = binding;
        public readonly VisualElement element = element;
        public readonly StyleInspectorElement.AuthoringContext authoringContext = (StyleInspectorElement.AuthoringContext)element.GetHierarchicalDataSourceContext().dataSource;
        public readonly BindingId bindingId = bindingId;
    }

    readonly record struct BindingInfoKey(VisualElement target, in BindingId id)
    {
        public readonly VisualElement target = target;
        public readonly BindingId id = id;
    }

    struct State
    {
        public bool IsInlined;
        public bool HasBinding;
        public bool HasVariable;

        public bool IsOverridden => IsInlined || HasBinding || HasVariable;
    }

    interface IProcessGenericChange<T>
    {
        void ProcessGenericChange(ref T value);
    }

    partial class GenericValueAtPath : PathVisitor
    {
        public StylePropertyBinding binding;
        public BindingId bindingId;
        public VisualElement element;
        public StyleInspectorElement.AuthoringContext authoringContext;

        public override void Reset()
        {
            base.Reset();
            binding = null;
            bindingId = default;
            element = null;
            authoringContext = null;
        }

        protected override void VisitPath<TContainer, TValue>(Property<TContainer, TValue> property, ref TContainer container, ref TValue value)
        {
            if (this is IProcessGenericChange<TValue> typedProcessor)
            {
                typedProcessor.ProcessGenericChange(ref value);
            }
        }

        private bool ShouldProcessChange()
        {
            if (authoringContext.IsReadOnly)
            {
                binding.Update(bindingId, binding.stylePropertyId, authoringContext, element);
                return false;
            }

            return true;
        }
    }

    static readonly UnityEngine.Pool.ObjectPool<GenericValueAtPath> s_VisitorPool = new (CreateVisitor, null, OnReleaseVisitor);

    static GenericValueAtPath CreateVisitor()
    {
        return new GenericValueAtPath();
    }

    static void OnReleaseVisitor(GenericValueAtPath visitor)
    {
        visitor.Reset();
    }

    public static readonly string k_AddBindingText = L10n.Tr("Add Binding");
    public static readonly string k_RemoveBindingText = L10n.Tr("Remove Binding");
    public static readonly string k_EditBindingText = L10n.Tr("Edit Binding");
    public static readonly string k_ViewBindingText = L10n.Tr("View Binding");
    public static readonly string k_ViewVariableText = L10n.Tr("View variable");
    public static readonly string k_SetVariableText = L10n.Tr("Set variable");
    public static readonly string k_EditVariableText = L10n.Tr("Edit variable");
    public static readonly string k_RemoveVariableText = L10n.Tr("Remove variable");

    public static readonly UniqueStyleString k_InlineFieldUssClassName = new("style-property-field__inline-value");
    public static readonly UniqueStyleString k_VariableFieldUssClassName = new("style-property-field__variable");
    public static readonly UniqueStyleString k_BoundFieldUssClassName = new("style-property-field__bound");

    public static readonly UniqueStyleString k_AnimationDrivenFieldUssClassName = new("style-property-field__animation-driven");
    public static readonly UniqueStyleString k_AnimationAnimatedFieldUssClassName = new("style-property-field__animation-animated");
    public static readonly UniqueStyleString k_AnimationRecordingFieldUssClassName = new("style-property-field__animation-recording");
    public static readonly UniqueStyleString k_AnimationCandidateFieldUssClassName = new("style-property-field__animation-candidate");

    const string k_ContextualMenuManipulatorPropertyName = "__ContextMenuManipulator";

    private readonly Dictionary<BindingInfoKey, State> m_State = new ();
    private UpdateFlags m_UpdateFlags;
    private string m_StyleProperty;
    private string m_StylePropertyCSharpName;
    private StylePropertyId stylePropertyId;

    public event Action<ITrackablePropertyProvider, string, TrackedPropertyType> OnTrackedPropertyChanged;
    public event Action<ITrackablePropertyProvider, string, bool, bool> OnTrackedPropertySourceChanged;

    internal const string k_DataSourcePathTooltip = "The name of the style property that is targeted.";

    /// <summary>
    /// Object that serves as a local source for the binding, and is particularly useful when the data source is not
    /// part of the UI hierarchy, such as a static localization table. If this object is null, the binding resolves
    /// the data source using its normal resolution method.
    /// </summary>
    /// <remarks>
    /// Using a local source does not prevent children of the target from using the hierarchy source.
    /// </remarks>
    [UxmlAttribute, HideInInspector, Tooltip(k_DataSourcePathTooltip), CreateProperty]
    public string styleProperty
    {
        get => m_StyleProperty;
        private set
        {
            if (m_StyleProperty == value)
                return;
            m_StyleProperty = value;
            stylePropertyId = GetPropertyId(m_StyleProperty);
            m_StylePropertyCSharpName = StylePropertyUtil.ussNameToCSharpName.GetValueOrDefault(m_StyleProperty, m_StyleProperty);
        }
    }

    private bool ignoreChanges
    {
        get => (m_UpdateFlags & UpdateFlags.IgnoreChanges) == UpdateFlags.IgnoreChanges;
        set
        {
            if (value)
                m_UpdateFlags |= UpdateFlags.IgnoreChanges;
            else
                m_UpdateFlags &= ~UpdateFlags.IgnoreChanges;
        }
    }

    public StylePropertyBinding()
        :this(null)
    {
    }

    public StylePropertyBinding(string styleProperty)
    {
        updateTrigger = BindingUpdateTrigger.OnSourceChanged;
        this.styleProperty = styleProperty;
    }

    protected internal override void OnActivated(in BindingActivationContext context)
    {
        base.OnActivated(in context);
        SendTrackPropertyEvent(this, context.targetElement, styleProperty, PropertyTrackingType.Register);
        RegisterCallbacks(this, context.bindingId, stylePropertyId, context.targetElement);

        // TrickleDown phase: must run before VisualElement.SetTooltip and BaseField.HandleEventBubbleUp
        context.targetElement.RegisterCallback<TooltipEvent, CallbackContext>(
            OnTooltipEvent,
            new CallbackContext(this, context.targetElement, context.bindingId),
            TrickleDown.TrickleDown);

        SetupVariableEditingHandler(context.targetElement);
    }

    protected internal override void OnDeactivated(in BindingActivationContext context)
    {
        base.OnDeactivated(in context);
        OnTrackedPropertyChanged?.Invoke(this, styleProperty, TrackedPropertyType.StopTracking);
        UnregisterCallbacks(this, stylePropertyId, context.targetElement);
        context.targetElement.UnregisterCallback<TooltipEvent, CallbackContext>(OnTooltipEvent, TrickleDown.TrickleDown);
        m_State.Remove(new BindingInfoKey(context.targetElement, context.bindingId));
    }

    protected internal override BindingResult Update(in BindingContext context)
    {
        if (context.dataSource is not StyleInspectorElement.AuthoringContext ctx)
            return new BindingResult(BindingStatus.Failure, "Expected a StyleDiff as the data source");

        // StyleDiff has not been run yet.
        if (ctx.StyleDiff.currentContextType == StyleDiff.ContextType.None)
            return new BindingResult(BindingStatus.Pending);

        if (!IsStylePropertySupported(stylePropertyId))
            return new BindingResult(BindingStatus.Failure, "Expected a style property as the target. Shorthand and custom properties are not supported.");

        var targetElement = context.targetElement;
        return Update(context.bindingId, stylePropertyId, ctx, targetElement);
    }

    static void ProcessChange<T>(T value, StyleInspectorElement.AuthoringContext authoringContext, StylePropertyBinding binding, Action<StyleProperty, StyleSheet, T> setter)
    {
        var styleDiff = authoringContext.StyleDiff;
        switch (styleDiff.currentContextType)
        {
            case StyleDiff.ContextType.VisualElement:
            {
                if (authoringContext.AnimationController != null)
                {
                    Debug.Assert(AnimationMode.InAnimationRecording(),
                        "AnimationController is set but AnimationMode.InAnimationRecording() is false.");
                    if (StyleDebug.IsShorthandProperty(binding.stylePropertyId))
                        break;
                    var inspectedElement = styleDiff.currentTarget;
                    AnimationRecordingStyleBridge.TryRecordStylePropertyChange(inspectedElement, binding.stylePropertyId, false, in value, in value);
                    break;
                }

                Debug.Assert(!AnimationMode.InAnimationRecording(),
                    "AnimationMode is recording but AnimationController is null. Refresh must run before ProcessChange.");

                var command = new SetInlineStylePropertyCommand<T>(styleDiff.currentTarget, binding.stylePropertyId, setter, value);
                command.Execute();
                break;
            }
            case StyleDiff.ContextType.StyleSheet:
            {
                Debug.Assert(authoringContext.AnimationController == null,
                    "AnimationController must be null in StyleSheet context.");
                var command = new SetStyleSheetPropertyCommand<T>(styleDiff.currentStyleSheet, styleDiff.currentRule, binding.stylePropertyId, setter, value);
                command.Execute();

                // Update selector element
                styleDiff.currentTarget?.UpdateInlineRule(styleDiff.currentStyleSheet, styleDiff.currentRule, styleDiff.currentTarget.variableContext);
                styleDiff.currentTarget?.IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Styles);

                break;
            }
            case StyleDiff.ContextType.None:
            default:
                Debug.Assert(authoringContext.AnimationController == null,
                    "AnimationController must be null when context type is None.");
                break;
        }
    }

    // Composes the field tooltip from up to three independent sections, joined by blank lines:
    //   1. <b>{blocked reason}</b>  - recording is active and this property cannot be recorded here.
    //   2. {affordance tooltip}     - echoes the affordance icon's current tooltip (Default Value,
    //                                 Inline Value, Inherited, Animation-driven/Recording/Candidate,
    //                                 binding/variable details, ...). Independent from section 1 -
    //                                 a UXML-driven property that is also recording-blocked shows
    //                                 both the bold status AND its affordance line.
    //   3. UXML tooltip verbatim    - already carries its own bold "<b>USS property: name</b> ..."
    //                                 header and inline description.
    // rect is anchored to the field worldBound in every case so the tooltip pops above the field.
    // StopImmediatePropagation is called only when we actually emit something, so a field with no
    // UXML tooltip and no affordance state lets the default tooltip flow continue undisturbed.
    static void OnTooltipEvent(TooltipEvent evt, CallbackContext ctx)
    {
        evt.rect = ctx.element.worldBound;

        var sb = new System.Text.StringBuilder();

        var controller = ctx.authoringContext?.AnimationController;
        if (controller != null)
        {
            var blocked = ComputeBlockedReason(
                ctx.authoringContext.StyleDiff.currentTarget,
                ctx.binding.stylePropertyId,
                controller);
            if (blocked != null)
                sb.Append("<b>").Append(blocked).Append("</b>");
        }

        var affordance = (ctx.element as IAffordanceField)?.affordanceElement?.GetTooltip();
        if (!string.IsNullOrEmpty(affordance))
        {
            if (sb.Length > 0) sb.Append("\n\n");
            sb.Append(affordance);
        }

        var baseTooltip = ctx.element.tooltip;
        if (!string.IsNullOrEmpty(baseTooltip))
        {
            if (sb.Length > 0) sb.Append("\n\n");
            sb.Append(baseTooltip);
        }

        if (sb.Length == 0)
            return;

        evt.tooltip = sb.ToString();
        evt.StopImmediatePropagation();
    }

    static string ComputeBlockedReason(VisualElement inspected, StylePropertyId id, StyleInspectorAnimationRecordingContext controller)
    {
        if (controller.IsPropertyRecordable(id))
            return null;

        // Per-element clip in scope bypasses the panel-wide naming rule, so any block here can only
        // be property-level (shorthand / unsupported channel). The panel-wide probe would falsely
        // report "give this element a name" for an unnamed element that records fine via its clip.
        if (VisualElementAnimationClipUtility.FindClipOwner(inspected) != null)
            return VisualElementRecordability.k_PropertyNotRecordableMessage;

        return VisualElementRecordability.Probe(inspected, id).GetBlockedMessage();
    }

    static void ProcessChange<T>(ChangeEvent<T> evt, CallbackContext ctx, Action<StyleProperty, StyleSheet, T> setter)
    {
        if (ctx.binding.ignoreChanges)
            return;

        if (ShouldProcessChange(evt, ctx))
            ProcessChange(evt.newValue, ctx.authoringContext, ctx.binding, setter);
    }

    static void ProcessChange<T>(CompositeStylePropertyChangeEvent<T> evt, CallbackContext ctx, Action<StyleProperty, StyleSheet, T> setter)
    {
        if (ctx.binding.ignoreChanges || evt.target != ctx.element || evt.Id != ctx.bindingId)
            return;

        if (ShouldProcessChange(evt, ctx))
            ProcessChange(evt.NewValue, ctx.authoringContext, ctx.binding, setter);
    }

    void SetupContextMenu<TInline, TComputed>(VisualElement field, FieldAffordanceElement fieldAffordanceElement,
        StyleInspectorElement.AuthoringContext authoringContext, StylePropertyData<TInline, TComputed> value)
    {
        if (!field.HasProperty(k_ContextualMenuManipulatorPropertyName))
        {
            var contextMenuManipulator = new ContextualMenuManipulator((evt) =>
            {
                // Dynamically retrieve the current affordance element instead of capturing it
                var currentAffordanceElement = (field as IAffordanceField)?.affordanceElement;
                currentAffordanceElement?.OnContextualMenuPopulate(evt);
            });
            contextMenuManipulator.acceptClicksIfDisabled = true;
            field.AddManipulator(contextMenuManipulator);
            field.SetProperty(k_ContextualMenuManipulatorPropertyName, contextMenuManipulator);
        }

        fieldAffordanceElement.populateMenuItems = menu =>
        {
            var ve = authoringContext.StyleDiff.currentTarget;
            var bindingPath = "style." + m_StylePropertyCSharpName;
            var isBindableElement = UxmlSerializedDataRegistry.GetDescription(ve.GetType().FullName) != null;
            var isBindableProperty = PropertyContainer.IsPathValid(ve, bindingPath);

            // Add a separator in case then menu is already filled with items (e.g: TextField's input)
            menu.AppendSeparator();

            if (isBindableElement && isBindableProperty)
            {
                var hasDataBinding = false;
                var vea = ve.visualElementAsset;

                if (vea != null)
                {
                    hasDataBinding = ve.TryGetBinding(bindingPath, out _);
                }

                if (hasDataBinding)
                {
                    if (authoringContext.IsReadOnly)
                    {
                        menu.AppendAction(k_ViewBindingText,
                            (a) => BindingWindow.OpenToView(ve, bindingPath, fieldAffordanceElement),
                            (a) => DropdownMenuAction.Status.Normal,
                            this);
                    }
                    else
                    {
                        menu.AppendAction(k_EditBindingText,
                            (a) =>  BindingWindow.OpenToEdit(ve, bindingPath, fieldAffordanceElement),
                            (a) => DropdownMenuAction.Status.Normal,
                            this);

                        menu.AppendAction(k_RemoveBindingText, (a) => {
                            var cmd = new RemoveBindingCommand(ve, stylePropertyId);
                            cmd.Execute();
                        }, (a) => DropdownMenuAction.Status.Normal, this);
                    }
                }
                else
                {
                    if (!authoringContext.IsReadOnly && vea != null)
                    {
                        menu.AppendAction(k_AddBindingText,
                            _ => { BindingWindow.OpenToCreate(ve, bindingPath, fieldAffordanceElement); });
                    }
                }
            }

            var isOverridden = value.uxmlValue.isInlined || value.binding != null || value.uxmlValue.requireVariableResolve;

            var status = isOverridden && !authoringContext.IsReadOnly ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
            menu.AppendAction("Unset", _ => UnsetStyleProperty(authoringContext.StyleDiff), status);

            var hasAnyProperties = HasAnyProperties(authoringContext.StyleDiff);
            var unsetAllStatus = hasAnyProperties && !authoringContext.IsReadOnly ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
            menu.AppendAction("Unset All", _ => UnsetAllStyleProperties(authoringContext.StyleDiff), unsetAllStatus);

            menu.AppendSeparator();

            if (value.uxmlValue.requireVariableResolve)
            {
                menu.AppendAction(k_EditVariableText,
                    ViewVariableViaContextMenu,
                    a => VariableActionStatus(a, authoringContext.IsReadOnly),
                    field);

                var removeVariableStatus = !authoringContext.IsReadOnly
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled;
                menu.AppendAction(k_RemoveVariableText, (a) => RemoveVariableViaContextMenu(authoringContext),
                    (a) => removeVariableStatus, field);
            }
            else
            {
                menu.AppendAction(k_SetVariableText, ViewVariableViaContextMenu,
                    a => VariableActionStatus(a, authoringContext.IsReadOnly),
                    field);
            }
        };
    }

    DropdownMenuAction.Status VariableActionStatus(DropdownMenuAction action, bool isReadOnly)
    {
        var bindableElement = action.userData as BindableElement;
        if (bindableElement == null)
            return DropdownMenuAction.Status.Disabled;

        var varEditingHandler = StyleVariableUtility.GetVarHandler(bindableElement);
        if (varEditingHandler == null)
            return DropdownMenuAction.Status.Disabled;

        return !isReadOnly ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
    }

    void ViewVariableViaContextMenu(DropdownMenuAction action)
    {
        var bindableElement = action.userData as BindableElement;
        var varEditingHandler = StyleVariableUtility.GetVarHandler(bindableElement);
        varEditingHandler.ShowVariableField();
    }

    void RemoveVariableViaContextMenu(StyleInspectorElement.AuthoringContext authoringContext)
    {
        var styleDiff = authoringContext.StyleDiff;
        new RemoveVariableCommand(styleDiff.currentStyleSheet, styleDiff.currentRule, stylePropertyId).Execute();

        // Update selector element
        var element = styleDiff.currentTarget;
        element?.UpdateInlineRule(styleDiff.currentStyleSheet, styleDiff.currentRule, element.variableContext);
        element?.IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Styles);
    }

    void UnsetStyleProperty(StyleDiff styleDiff)
    {
        switch (styleDiff.currentContextType)
        {
            case StyleDiff.ContextType.VisualElement:
                UnsetInlineStyleProperty(styleDiff.currentTarget);
                break;
            case StyleDiff.ContextType.StyleSheet:
                UnsetStyleSheetProperty(styleDiff.currentStyleSheet, styleDiff.currentRule, styleDiff.currentTarget);
                break;
        }
    }

    void UnsetInlineStyleProperty(VisualElement element)
    {
        var command = new UnsetInlineStylePropertyCommand(element, stylePropertyId);
        command.Execute();
    }

    void UnsetStyleSheetProperty(StyleSheet styleSheet, StyleRule rule, VisualElement element)
    {
        var command = new UnsetStyleSheetPropertyCommand(styleSheet, rule, stylePropertyId);
        command.Execute();

        element?.UpdateInlineRule(styleSheet, rule, element.variableContext);
        element?.IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Styles);
    }

    bool HasAnyProperties(StyleDiff styleDiff)
    {
        switch (styleDiff.currentContextType)
        {
            case StyleDiff.ContextType.VisualElement:
            {
                var element = styleDiff.currentTarget;
                if (element?.visualTreeAssetSource == null)
                    return false;

                var visualTreeAsset = element.visualTreeAssetSource;
                var inlineStyleSheet = visualTreeAsset.inlineSheet;
                if (inlineStyleSheet == null)
                    return false;

                var vea = element.visualElementAsset;
                if (vea == null || vea.ruleIndex < 0)
                    return false;

                var rule = inlineStyleSheet.rules[vea.ruleIndex];
                return rule.properties.Length > 0;
            }
            case StyleDiff.ContextType.StyleSheet:
            {
                var rule = styleDiff.currentRule;
                return rule != null && rule.properties.Length > 0;
            }
            default:
                return false;
        }
    }

    void UnsetAllStyleProperties(StyleDiff styleDiff)
    {
        switch (styleDiff.currentContextType)
        {
            case StyleDiff.ContextType.VisualElement:
                UnsetAllInlineStyleProperties(styleDiff.currentTarget);
                break;
            case StyleDiff.ContextType.StyleSheet:
                UnsetAllStyleSheetProperties(styleDiff.currentStyleSheet, styleDiff.currentRule, styleDiff.currentTarget);
                break;
        }
    }

    void UnsetAllInlineStyleProperties(VisualElement element)
    {
        var command = new UnsetAllInlineStylePropertiesCommand(element);
        command.Execute();
    }

    void UnsetAllStyleSheetProperties(StyleSheet styleSheet, StyleRule rule, VisualElement element)
    {
        var command = new UnsetAllStyleSheetPropertiesCommand(styleSheet, rule);
        command.Execute();

        // Update selector element
        element?.UpdateInlineRule(styleSheet, rule, element.variableContext);
        element?.IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Styles);
    }

    BindingResult Update<TInline, TComputed>(in BindingId id, StylePropertyData<TInline, TComputed> value,
        StyleInspectorElement.AuthoringContext authoringContext, VisualElement targetElement)
    {
        var currentState = new State
        {
            IsInlined = value.uxmlValue.isInlined,
            HasBinding = value.binding != null,
            HasVariable = value.uxmlValue.requireVariableResolve
        };

        var isOverridden = currentState.IsOverridden;
        var key = new BindingInfoKey(targetElement, in id);
        if (m_State.TryGetValue(key, out var previousState))
        {
            m_State[key] = currentState;
            if (previousState.IsOverridden != currentState.IsOverridden)
                OnTrackedPropertyChanged?.Invoke(this, styleProperty, isOverridden ? TrackedPropertyType.MarkOverride : TrackedPropertyType.ClearOverride);
        }
        // If state is not tracked yet and the value is not inlined, let's wait until it is to start tracking it.
        else if (isOverridden)
        {
            m_State[key] = currentState;
            OnTrackedPropertyChanged?.Invoke(this, styleProperty, TrackedPropertyType.MarkOverride);
        }

        targetElement.EnableInClassList(k_InlineFieldUssClassName, value.uxmlValue.isInlined);
        targetElement.EnableInClassList(k_VariableFieldUssClassName, value.uxmlValue.requireVariableResolve);
        targetElement.EnableInClassList(k_BoundFieldUssClassName, value.binding != null);
        OnTrackedPropertySourceChanged?.Invoke(this, styleProperty, value.uxmlValue.requireVariableResolve, value.binding != null);

        var inlineValue = value.inlineValue;
        var computedValue = value.computedValue;

        var targetEnabled = true;

        var fieldAffordanceElement = (targetElement as IAffordanceField)?.affordanceElement;
        var animationSubState = FieldAffordanceSourceInfoType.Default;
        if (fieldAffordanceElement != null)
        {
            FieldAffordanceController.UpdateFieldAffordanceData(fieldAffordanceElement.fieldAffordanceData,
                authoringContext.StyleDiff.currentTarget, authoringContext.StyleDiff.currentContextType, value);
            SetupContextMenu(targetElement, fieldAffordanceElement, authoringContext, value);
            var sourceType = fieldAffordanceElement.fieldAffordanceData.sourceTypeInfo;
            var hasResolvedBinding = sourceType == FieldAffordanceSourceInfoType.ResolvedBinding;
            var hasResolvedVariable = sourceType == FieldAffordanceSourceInfoType.USSVariable
                && fieldAffordanceElement.fieldAffordanceData.variableSheet != null;
            if (sourceType.IsAnimationDriven())
                animationSubState = sourceType;
            targetEnabled &= !hasResolvedBinding && !hasResolvedVariable && !animationSubState.ShouldDisableInlineEdit();
        }
        targetElement.EnableInClassList(k_AnimationDrivenFieldUssClassName, animationSubState.IsAnimationDriven());
        targetElement.EnableInClassList(k_AnimationAnimatedFieldUssClassName, animationSubState == FieldAffordanceSourceInfoType.AnimationAnimated);
        targetElement.EnableInClassList(k_AnimationRecordingFieldUssClassName, animationSubState == FieldAffordanceSourceInfoType.AnimationRecording);
        targetElement.EnableInClassList(k_AnimationCandidateFieldUssClassName, animationSubState == FieldAffordanceSourceInfoType.AnimationCandidate);

        var inRecording = authoringContext.AnimationController != null;
        Debug.Assert(inRecording == AnimationMode.InAnimationRecording(),
            $"AnimationController presence ({inRecording}) must match AnimationMode.InAnimationRecording() ({AnimationMode.InAnimationRecording()}).");
        var propertyRecordable = !inRecording || authoringContext.AnimationController.IsPropertyRecordable(stylePropertyId);
        targetEnabled &= propertyRecordable;
        targetEnabled &= !authoringContext.IsReadOnly;
        targetElement.enabledSelf = targetEnabled;

        using var _ = new IgnoreChangeScope(this);
        switch (targetElement)
        {
            case BaseField<TComputed> field when id == BaseField<TComputed>.valueProperty:
                field.value = computedValue;
                break;
            case BaseField<TInline> inlineField when id == BaseField<TInline>.valueProperty:
            {
                if (TypeConversion.TryConvert<TComputed, TInline>(ref computedValue, out var convertedValue))
                    inlineField.value = convertedValue;
                else
                    Debug.LogWarning($"Invalid Cast from: `{typeof(TComputed).Name}` to `{typeof(TInline).Name}`");
                break;
            }
            case INotifyCompositeStylePropertyChanged<TComputed> compositeComputedField:
            {
                compositeComputedField.SetValue(id, computedValue, true);
                break;
            }
            case INotifyCompositeStylePropertyChanged<TInline> compositeInlineField:
            {
                if (TypeConversion.TryConvert<TComputed, TInline>(ref computedValue, out var convertedValue))
                    compositeInlineField.SetValue(id, convertedValue, true);
                else
                    Debug.LogWarning($"Invalid Cast from: `{typeof(TComputed).Name}` to `{typeof(TInline).Name}`");
                break;
            }
            default:
                PropertyContainer.SetValue(targetElement, id, computedValue);
                break;
        }

        return default;
    }

    private static void SendTrackPropertyEvent(ITrackablePropertyProvider provider, VisualElement target, string styleProperty, PropertyTrackingType type)
    {
        using var evt = TrackPropertyEvent.GetPooled(provider, styleProperty);
        evt.target = target;
        target.SendEvent(evt);
    }

    private static StylePropertyId GetPropertyId(string propertyName)
    {
        // i.e. backgroundColor => background-color
        if (StylePropertyUtil.cSharpNameToUssName.TryGetValue(propertyName, out var ussName))
            propertyName = ussName;

        return StylePropertyUtil.propertyNameToStylePropertyId.GetValueOrDefault(propertyName, StylePropertyId.Unknown);
    }

    private static bool IsStylePropertySupported(StylePropertyId stylePropertyId)
    {
        return stylePropertyId is not (StylePropertyId.All or StylePropertyId.Custom) && !StyleDebug.IsShorthandProperty(stylePropertyId);
    }

    private static string GetUnsupportedPropertyId(StylePropertyId stylePropertyId)
    {
        return $"[UI Toolkit] Unsupported style property: '{stylePropertyId}'";
    }

    static void RegisterEnumCallbacks<T>(StylePropertyBinding binding, in BindingId id, VisualElement targetElement)
        where T: struct, Enum, IConvertible
    {
        var isValueProperty = id == BaseField<T>.valueProperty;
        switch (targetElement)
        {
            case BaseField<T> when isValueProperty:
                targetElement.RegisterCallback<ChangeEvent<T>, CallbackContext>(ProcessChange, new CallbackContext(binding, targetElement, id));
                break;
            case BaseField<StyleEnum<T>> when isValueProperty:
                targetElement.RegisterCallback<ChangeEvent<StyleEnum<T>>, CallbackContext>(ProcessChange, new CallbackContext(binding, targetElement, id));
                break;
            case BaseField<Enum> when isValueProperty:
                targetElement.RegisterCallback<ChangeEvent<Enum>, CallbackContext>(ProcessChange, new CallbackContext(binding, targetElement, id));
                break;
            default:
                targetElement.RegisterCallback<PropertyChangedEvent, CallbackContext>(binding.ProcessChange, new CallbackContext(binding, targetElement, id));
                break;
        }
    }

    static void RegisterCallbacks<TStyleValue, TValue>(
        StylePropertyBinding binding,
        in BindingId id,
        VisualElement targetElement,
        EventCallback<ChangeEvent<TStyleValue>, CallbackContext> styleValueCallback,
        EventCallback<ChangeEvent<TValue>, CallbackContext> valueCallback,
        EventCallback<CompositeStylePropertyChangeEvent<TStyleValue>, CallbackContext> styleValueChangedCallback,
        EventCallback<CompositeStylePropertyChangeEvent<TValue>, CallbackContext> valueChangedCallback)
    {
        switch (targetElement)
        {
            case INotifyValueChanged<TStyleValue> when id == BaseField<TStyleValue>.valueProperty:
                targetElement.RegisterCallback(styleValueCallback, new CallbackContext(binding, targetElement, id));
                break;
            case INotifyValueChanged<TValue> when id == BaseField<TValue>.valueProperty:
                targetElement.RegisterCallback(valueCallback, new CallbackContext(binding, targetElement, id));
                break;
            case INotifyCompositeStylePropertyChanged<TStyleValue>:
                targetElement.RegisterCallback(styleValueChangedCallback, new CallbackContext(binding, targetElement, id));
                break;
            case INotifyCompositeStylePropertyChanged<TValue>:
                targetElement.RegisterCallback(valueChangedCallback, new CallbackContext(binding, targetElement, id));
                break;
            default:
                targetElement.RegisterCallback<PropertyChangedEvent, CallbackContext>(binding.ProcessChange, new CallbackContext(binding, targetElement, id));
                break;
        }
    }

    static void SetStyleValue<TStyleValue, TValue>(StyleProperty property, StyleSheet sheet, TStyleValue styleValue, Action<StyleProperty, StyleSheet, TValue> setterDelegate)
        where TStyleValue : IStyleValue<TValue>
    {
        switch (styleValue.keyword)
        {
            case StyleKeyword.Undefined:
                setterDelegate(property, sheet, styleValue.value);
                break;
            case StyleKeyword.Null:
                break;
            case StyleKeyword.Auto:
                property.SetKeyword(sheet, StyleValueKeyword.Auto);
                break;
            case StyleKeyword.None:
                property.SetKeyword(sheet, StyleValueKeyword.None);
                break;
            case StyleKeyword.Initial:
                property.SetKeyword(sheet, StyleValueKeyword.Initial);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static bool ShouldProcessChange<T>(ChangeEvent<T> evt, CallbackContext ctx)
    {
        if (evt.target != ctx.element)
            return false;

        if (!ctx.authoringContext.IsReadOnly)
            return true;

        // Write back the previous value so that it looks like it's readonly.
        ((BaseField<T>)evt.target).SetValueWithoutNotify(evt.previousValue);
        evt.StopImmediatePropagation();
        return false;
    }

    private static bool ShouldProcessChange<T>(CompositeStylePropertyChangeEvent<T> evt, CallbackContext ctx)
    {
        if (evt.target != ctx.element)
            return false;

        if (!ctx.authoringContext.IsReadOnly)
            return true;

        // Write back the previous value so that it looks like it's readonly.
        ((INotifyCompositeStylePropertyChanged<T>)evt.target).SetValue(evt.Id, evt.PreviousValue, false);
        evt.StopImmediatePropagation();

        return false;
    }

    void SetupVariableEditingHandler(VisualElement targetElement)
    {
        if (targetElement is not BindableElement bindableField)
            return;

        var styleInspector = bindableField.GetFirstAncestorOfType<StyleInspectorElement>();
        if (styleInspector?.VariableEditingContext == null)
            return;

        var handler = StyleVariableUtility.GetOrCreateVarHandler(bindableField, styleInspector.VariableEditingContext,
            bindableField.GetFirstAncestorOfType<OverrideRow>(),
            targetElement is StyleLengthField or StyleFloatField or StyleIntField);
        handler.styleName = StylePropertyUtil.cSharpNameToUssName.GetValueOrDefault(styleProperty, styleProperty);
    }

    // These are implemented solely to opt-in the change tracking per property, we don't want these to be set
    // from UXML, hence why they are not UXML attributes.
    object IDataSourceProvider.dataSource => null;
    PropertyPath IDataSourceProvider.dataSourcePath => PropertyPath.FromName(m_StylePropertyCSharpName);
}
