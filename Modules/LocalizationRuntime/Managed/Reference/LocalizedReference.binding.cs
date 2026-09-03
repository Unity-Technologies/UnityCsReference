// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Localization;

[UxmlObject]
public abstract partial class LocalizedReference : CustomBinding
{
    ConverterGroup m_Converters;
    int m_ActiveCount;

    [UxmlAttribute("table")]
    internal TableReference Table
    {
        get => TableReference;
        set => TableReference = value;
    }

    [UxmlAttribute("fallback")]
    internal bool Fallback
    {
        get => EnableFallback;
        set => EnableFallback = value;
    }

    /// <summary>
    /// Creates a localized reference.
    /// </summary>
    /// <remarks>
    /// The reference pushes a new value to the bound field as it resolves, so the binding updates only when there is
    /// something to write rather than on every frame.
    /// </remarks>
    protected LocalizedReference()
    {
        updateTrigger = BindingUpdateTrigger.WhenDirty;
    }

    // Falls back to the global converters, so a subclass only registers what UI Toolkit cannot already convert.
    internal ConverterGroup Converters => m_Converters ??= new ConverterGroup(string.Empty);

    /// <summary>
    /// Starts resolving the reference when it becomes active on an element.
    /// </summary>
    /// <remarks>
    /// One reference can be bound to several elements, so resolving starts on the first activation and stops on the
    /// last deactivation.
    /// </remarks>
    /// <param name="context">Describes the element and property the binding was activated for.</param>
    protected internal override void OnActivated(in BindingActivationContext context)
    {
        base.OnActivated(context);
        if (++m_ActiveCount == 1)
            StartBinding();
    }

    /// <summary>
    /// Stops resolving the reference once it is no longer active on any element.
    /// </summary>
    /// <remarks>
    /// The resolved value is dropped, so a reference that becomes active again resolves afresh.
    /// </remarks>
    /// <param name="context">Describes the element and property the binding was deactivated for.</param>
    protected internal override void OnDeactivated(in BindingActivationContext context)
    {
        base.OnDeactivated(context);
        if (--m_ActiveCount == 0)
            StopBinding();
    }

    /// <summary>
    /// Writes the resolved value to the bound field.
    /// </summary>
    /// <remarks>
    /// A reference with nothing to write reports success and leaves the field alone.
    /// </remarks>
    /// <param name="context">Describes the element and property to write to.</param>
    /// <returns>Whether the value reached the bound field.</returns>
    protected internal override BindingResult Update(in BindingContext context)
        => IsEmpty ? new BindingResult(BindingStatus.Success) : WriteBoundValue(context);

    private protected virtual BindingResult WriteBoundValue(in BindingContext context)
        => new(BindingStatus.Success);

    private protected virtual void StartBinding() { }

    private protected virtual void StopBinding() { }

    private protected BindingResult WriteToTarget<TValue>(in BindingContext context, TValue value)
    {
        var element = context.targetElement;
        if (Converters.TrySetValue(ref element, context.bindingId, value, out var code))
            return new BindingResult(BindingStatus.Success);
        return new BindingResult(BindingStatus.Failure,
            $"{GetType().Name}: cannot write a {typeof(TValue).Name} to '{context.bindingId}' ({code}).");
    }

    // UI Builder and the authoring preview run outside Play Mode, where no locale is selected. Without this the
    // bound fields would all render empty while authoring.
    private protected void ApplyEditorPreviewLocale()
    {
        if (Application.isPlaying || HasLocaleOverride || LocalizationSettings.SelectedLocale != null)
            return;
        var project = LocalizationSettings.Instance != null ? LocalizationSettings.Instance.ProjectLocale : null;
        if (project != null)
            LocaleOverride = project.Identifier;
    }
}
