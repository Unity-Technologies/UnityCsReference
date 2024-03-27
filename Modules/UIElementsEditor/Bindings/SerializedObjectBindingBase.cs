// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings;

internal abstract class SerializedObjectBindingBase : CustomBinding, IDataSourceProvider, IDataSourceViewHashProvider
{
    private long m_LastUpdateTime;
    private ulong m_LastVersion;

    private static long GetCurrentTime()
    {
        return Panel.TimeSinceStartupMs();
    }

    // This is to ensure that getting the resolved data source is as fast as possible, since we don't need to fetch it from the hierarchy.
    public object dataSource => this;
    public PropertyPath dataSourcePath => default;

    private bool IsBindingContextUninitialized()
    {
        if (null == bindingContext)
            return true;

        if (null == bindingContext.serializedObject)
            return true;

        if (IntPtr.Zero == bindingContext.serializedObject.m_NativeObjectPtr)
            return true;

        return false;
    }

    public long GetViewHashCode()
    {
        if (IsBindingContextUninitialized())
            return -1;

        if (boundElement is VisualElement element)
            bindingContext.UpdateIfNecessary(element);

        // this can be set back to null on update
        if (IsBindingContextUninitialized())
            return -1;

        return bindingContext.serializedObject.objectVersion;
    }

    protected abstract string bindingId { get; }

    public SerializedObjectBindingContext bindingContext
    {
        get => m_BindingContext;
        private set
        {
            m_BindingContext?.UnregisterBindingObject(this);

            m_BindingContext = value;

            if (m_BindingContext != null)
            {
                m_BindingContext.RegisterBindingObject(this);
                SyncObjectVersion();
            }
            else
            {
                boundProperty = null;
            }
        }
    }

    public string boundPropertyPath;
    public int boundPropertyHash;
    private SerializedProperty m_BoundProperty;

    public SerializedProperty boundProperty
    {
        get => m_BoundProperty;
        set
        {
            m_BoundProperty = value;

            if (m_BoundProperty != null)
            {
                boundPropertyPath = m_BoundProperty.propertyPath;
                boundPropertyHash = m_BoundProperty.hashCodeForPropertyPath;
            }
            else
            {
                boundPropertyPath = null;
                boundPropertyHash = 0;
            }
        }
    }

    protected internal bool isReleased { get; set; }

    public abstract void OnRelease();

    public void Unbind()
    {
        if (boundElement is VisualElement element)
            element.ClearBinding(bindingId);
        OnRelease();
    }

    protected internal override void OnActivated(in BindingActivationContext context)
    {
        // Resets the throttling and make sure it will get called on the next update.
        m_LastUpdateTime = GetCurrentTime() - VisualTreeBindingsUpdater.k_MinUpdateDelayMs;
        base.OnActivated(in context);
    }

    protected internal override BindingResult Update(in BindingContext context)
    {
        if (IsBindingContextUninitialized())
        {
            Unbind();
            return default;
        }

        var currentTimeMs = GetCurrentTime();
        if (VisualTreeBindingsUpdater.disableBindingsThrottling || (currentTimeMs - m_LastUpdateTime) >= VisualTreeBindingsUpdater.k_MinUpdateDelayMs ||
            m_LastVersion != bindingContext?.serializedObject?.objectVersion)
        {
            m_LastUpdateTime = currentTimeMs;
            bindingContext?.UpdateIfNecessary(context.targetElement);
            var result = OnUpdate(in context);
            return result;
        }

        m_LastVersion = bindingContext?.serializedObject?.objectVersion ?? 0;
        return new BindingResult(BindingStatus.Pending);
    }

    public abstract BindingResult OnUpdate(in BindingContext context);

    protected void SetContext(SerializedObjectBindingContext context, SerializedProperty prop)
    {
        if (context != null)
        {
            boundProperty = prop;
        }

        bindingContext = context;
    }

    protected void ResetContext()
    {
        bindingContext = null;
    }

    public virtual void ResetUpdate()
    {
        bindingContext?.ResetUpdate();
    }

    internal static readonly PropertyName UndoGroupPropertyKey = "__UnityUndoGroup";

    protected IBindable m_Field;
    private SerializedObjectBindingContext m_BindingContext;

    private bool m_TooltipWasSet;
    private bool m_IsFieldAttached;

    EventCallback<AttachToPanelEvent> m_OnFieldAttachedToPanel;
    EventCallback<DetachFromPanelEvent> m_OnFieldDetachedFromPanel;

    protected SerializedObjectBindingBase()
    {
        m_OnFieldAttachedToPanel = OnAttachToPanel;
        m_OnFieldDetachedFromPanel = OnDetachFromPanel;
    }

    protected IBindable boundElement
    {
        get => m_Field;
        set
        {
            if (m_Field is VisualElement ve)
            {
                ve.UnregisterCallback(m_OnFieldAttachedToPanel);
                ve.UnregisterCallback(m_OnFieldDetachedFromPanel);
                if (m_TooltipWasSet)
                    ve.tooltip = null;
                m_TooltipWasSet = false;
            }

            m_Field = value;

            UpdateFieldIsAttached();
            if (m_Field != null)
            {
                ve = m_Field as VisualElement;
                if (ve != null)
                {
                    ve.RegisterCallback(m_OnFieldAttachedToPanel);
                    ve.RegisterCallback(m_OnFieldDetachedFromPanel);

                    // used to group undo operations (UUM-32599)
                    if (ve is IEditableElement editableElement)
                    {
                        editableElement.editingStarted += SetUndoGroup;
                        editableElement.editingEnded += UnsetUndoGroup;
                    }

                    if (string.IsNullOrEmpty(ve.tooltip))
                    {
                        ve.tooltip = boundProperty?.tooltip;
                        m_TooltipWasSet = true;
                    }
                }
            }
        }
    }

    protected bool isFieldAttached
    {
        get => m_IsFieldAttached;
        private set
        {
            if (m_IsFieldAttached != value)
            {
                m_IsFieldAttached = value;

                if (m_IsFieldAttached)
                {
                    m_BindingContext?.RegisterBindingObject(this);
                    if (boundProperty != null)
                    {
                        //we make sure the property value is applied
                        OnPropertyValueChanged(boundProperty);
                    }
                }
                else
                {
                    m_BindingContext?.UnregisterBindingObject(this);
                }
            }

        }
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        OnFieldAttached();
    }

    private void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        OnFieldDetached();
    }

    protected virtual void OnFieldAttached()
    {
        isFieldAttached = true;
        ResetCachedValues();
    }

    private void OnFieldDetached()
    {
        isFieldAttached = false;
    }

    void SetUndoGroup()
    {
        Undo.IncrementCurrentGroup();

        var field = m_Field as VisualElement;
        field?.SetProperty(UndoGroupPropertyKey, Undo.GetCurrentGroup());
    }

    void UnsetUndoGroup()
    {
        Undo.IncrementCurrentGroup();

        var field = m_Field as VisualElement;
        field?.SetProperty(UndoGroupPropertyKey, null);
    }

    protected void UpdateFieldIsAttached()
    {
        if (m_Field is VisualElement ve)
        {
            bool attached = ve.panel != null;

            if (isFieldAttached != attached)
            {
                if (attached)
                    OnFieldAttached();
                else
                    OnFieldDetached();
            }
        }
        else
        {
            //we're not dealing with VisualElement
            if (m_Field != null && !isFieldAttached)
            {
                OnFieldAttached();
            }
            else
            {
                OnFieldDetached();
            }
        }
    }

    protected abstract void ResetCachedValues();

    public virtual void OnPropertyValueChanged(SerializedProperty currentPropertyIterator)
    {
    }

    public bool ResolveProperty()
    {

        boundProperty = (bindingContext != null && bindingContext.IsValid()) ? bindingContext.serializedObject.FindProperty(boundPropertyPath) : null;
        return boundProperty != null;
    }

    private ulong m_LastSyncedVersion = 0;

    public void SyncObjectVersion()
    {
        if (m_BindingContext != null)
        {
            m_LastSyncedVersion = m_BindingContext.lastRevision;
        }
    }

    public bool IsSynced()
    {
        return m_BindingContext != null && m_LastSyncedVersion == m_BindingContext.lastRevision;
    }
}
