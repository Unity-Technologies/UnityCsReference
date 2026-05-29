// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// This class is used to control the editing of uxml attributes
/// </summary>
class UxmlAttributesEditingController : IDisposable, IVisualElementChangeProcessor
{
    List<UxmlAttributeFieldDecorator> m_RegisteredDecorators = new();

    UxmlAttributesEditingContext m_Context;

    bool m_HasPendingSync;

    /// <summary>
    /// Responsible for syncing live properties to the serialized data.
    /// </summary>
    public LiveAttributePropertyController liveAttributePropertyController { get; } = new LiveAttributePropertyController();

    /// <summary>
    /// Handles changes to UXML attributes.
    /// </summary>
    public UxmlAttributeChangeHandler attributeChangeHandler { get; } = new UxmlAttributeChangeHandler();

    /// <summary>
    /// The authoring context this controller is operating on.
    /// </summary>
    public UxmlAttributesEditingContext context
    {
        get => m_Context;
        internal set
        {
            if (m_Context != null)
                m_Context.contextChanged -= OnContextChanged;

            m_Context = value;
            liveAttributePropertyController.context = m_Context;
            attributeChangeHandler.Context = m_Context;

            if (m_Context != null)
                m_Context.contextChanged += OnContextChanged;
        }
    }

    /// <summary>
    /// Constructs a controller
    /// </summary>
    public UxmlAttributesEditingController()
    {
        UICommandQueue.RegisterHandler<SetAttributeOverrideCommand>(OnAttributeOverrideSet);
    }

    /// <summary>
    /// Called when the context is initialized
    /// </summary>
    /// <param name="sender">The context that has changed</param>
    /// <param name="args">The arguments of the event</param>
    void OnContextChanged(object sender, UxmlAttributesEditingContext.ContextChangedEventArgs args)
    {
        args.oldElement?.UnregisterCallback<PropertyChangedEvent>(OnElementPropertyChange);

        if (args.oldElement?.panel is Panel panel)
            panel.UnregisterChangeProcessor(this);

        if (context.element != null && context.uxmlSerializedData != null)
        {
            context.element.RegisterCallback<PropertyChangedEvent>(OnElementPropertyChange);

            // If the context is not read-only then default the not overridden data of the serialized data
            if (!context.isReadOnly)
            {
                // We treat the serialized data as the source of truth.
                // There are times when we may need to resync, such as when an undo/redo was performed.
                context.uxmlSerializedDataDescription.SyncDefaultValues(context.uxmlSerializedData, false);

                // Deserialize the element to ensure it has the latest data
                DeserializeElement();

                attributeChangeHandler.StartTrackingChanges();
            }

            // Ensure the serialized object is up to date
            context.rootSerializedObject.UpdateIfRequiredOrScript();

            // We need to sync the serialized data from the element
            liveAttributePropertyController.SyncLiveProperties(!context.isReadOnly);

            if (context.element?.panel is Panel targetPanel)
            {
                targetPanel.RegisterChangeProcessor(this);
            }
        }
    }

    public void Dispose()
    {
        if (m_HasPendingSync)
            EditorApplication.delayCall -= Sync;
        liveAttributePropertyController.RemoveLiveProperties();
        attributeChangeHandler.StopTrackingChanges();
        UICommandQueue.UnregisterHandler<SetAttributeOverrideCommand>(OnAttributeOverrideSet);
    }

    // Called when a property has changed
    void OnElementPropertyChange(PropertyChangedEvent property)
    {
        ScheduleSync();
    }

    void ScheduleSync()
    {
        if (m_HasPendingSync)
            return;
        // Delay the sync to avoid multiple syncs during the same frame
        m_HasPendingSync = true;
        EditorApplication.delayCall += Sync;
    }

    void Sync()
    {
        m_HasPendingSync = false;

        if (context.element == null)
            return;

        // If the context is read only, we need to sync the serialized data from the element
        liveAttributePropertyController.SyncLiveProperties(!context.isReadOnly);
    }

    internal void DeserializeElement()
    {
        if (context == null || context.element == null || context.uxmlSerializedData == null)
            return;

        // We need to clear bindings before calling Init to avoid corrupting the data source.
        ClearUxmlBindings();
        context.uxmlSerializedData.Deserialize(context.element, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml | UxmlSerializedData.UxmlAttributeFlags.DefaultValue);
    }

    void ClearUxmlBindings()
    {
        using var pool = ListPool<BindingId>.Get(out var idsToRemove);
        foreach (var bindingInfo in context.element.GetBindingInfos())
        {
            var bindingId = bindingInfo.binding.property;
            if (bindingId != BindingId.Invalid)
            {
                idsToRemove.Add(bindingId);
            }
        }

        foreach (var bindingId in idsToRemove)
        {
            context.element.ClearBinding(bindingId);
        }
    }

    public void RegisterUxmlAttributeFieldDecorator(UxmlAttributeFieldDecorator decorator)
    {
        if (m_RegisteredDecorators.Contains(decorator))
            return;

        m_RegisteredDecorators.Add(decorator);
    }

    public void UnregisterUxmlAttributeFieldDecorator(UxmlAttributeFieldDecorator decorator)
    {
        m_RegisteredDecorators.Remove(decorator);
    }

    public void BeginProcessing(BaseVisualElementPanel panel)
    {
        UpdateDecoratorsForBoundProperties();
    }

    public void ProcessChanges(BaseVisualElementPanel targetPanel, AuthoringChanges changes)
    {
        if (changes.bindingContextChanged.Contains(context.element))
        {
            UpdateDecoratorsForBoundProperties();
        }
    }

    public void EndProcessing(BaseVisualElementPanel panel)
    {
        // Intentionally left empty.
    }

    void UpdateDecoratorsForBoundProperties()
    {
        using var listHandle = ListPool<BindingInfo>.Get(out var bindingInfos);
        context.element?.GetBindingInfos(bindingInfos);

        foreach (var info in bindingInfos)
        {
            PropertyPath path = info.bindingId;
            var isStyleBinding = path.Length == 2 && path[0].IsName &&
                string.CompareOrdinal(path[0].Name, "style") == 0 &&
                path[1].IsName;
            if (!isStyleBinding)
            {
                // Find attribute field and update it
                foreach (var decorator in m_RegisteredDecorators)
                {
                    if (BindingId.Equals(info.bindingId, decorator.GetFullBindingPath()))
                    {
                        decorator.Refresh();
                        break;
                    }
                }
            }
        }
    }

    void OnAttributeOverrideSet(in CommandContext commandContext)
    {
        if (commandContext.Status != CommandExecutionStatus.Success)
            return;

        var command = (SetAttributeOverrideCommand)commandContext.Command;

        // Refresh all decorators that match the modified attribute
        foreach (var decorator in m_RegisteredDecorators)
        {
            if (decorator.boundAttributeDescription == command.AttributeDescription)
            {
                decorator.ScheduleRefresh();
            }
        }
    }
}
