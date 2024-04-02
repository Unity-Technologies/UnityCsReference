// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings;

internal abstract class SerializedObjectBindingBase : IBinding
{
    public SerializedObjectBindingContext bindingContext
    {
        get => m_BindingContext;
        private set
        {
            if (m_BindingContext != null)
            {
                m_BindingContext.UnregisterBindingObject(this);
            }

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

    protected bool isReleased { get; set; }
    public abstract void Update();
    public abstract void Release();

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

    public void PreUpdate()
    {
        bindingContext?.UpdateIfNecessary();
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
        get { return m_Field; }
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

            FieldBinding = null;

            m_Field = value;
            FieldBinding = this;
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

    protected IBinding FieldBinding
    {
        get
        {
            var bindable = m_Field as IBindable;
            return bindable?.binding;
        }
        set
        {
            if (m_Field != null)
            {
                var previousBinding = m_Field.binding;
                m_Field.binding = value;
                if (previousBinding != this)
                {
                    previousBinding?.Release();
                }
                (m_Field as VisualElement)?.IncrementVersion(VersionChangeType.Bindings);
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
