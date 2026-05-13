// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings;

internal sealed class SerializedObjectBindingContextUpdater : SerializedObjectBindingBase
{
    protected override string bindingId { get; } = BindingExtensions.s_SerializedBindingContextUpdaterId;

    private VisualElement contextUpdaterOwner;

    private UInt64 lastTrackedObjectRevision = 0xFFFFFFFFFFFFFFFF;
    private bool isActivated;

    public event Action<object, SerializedObject> registeredCallbacks;

    private struct TrackedPropertyRegistration
    {
        public int propertyPathHash;
        public string propertyPath;
        public Action<object, SerializedProperty> callback;

        public TrackedPropertyRegistration(int pathHash, string path, Action<object, SerializedProperty> cb)
        {
            propertyPathHash = pathHash;
            propertyPath = path;
            callback = cb;
        }

    }
    private List<TrackedPropertyRegistration> trackedProperties { get; }


    public static SerializedObjectBindingContextUpdater Create(VisualElement owner, SerializedObjectBindingContext context)
    {
        var b = new SerializedObjectBindingContextUpdater();

        b.isReleased = false;
        b.SetContext(context, null);
        b.contextUpdaterOwner = owner;
        b.lastTrackedObjectRevision = context.lastRevision;
        owner.SetBinding(BindingExtensions.s_SerializedBindingContextUpdaterId, b);
        return b;
    }

    protected override VisualElement owner => contextUpdaterOwner;


    public SerializedObjectBindingContextUpdater()
    {
        trackedProperties = new ();
        isActivated = false;
    }

    // Override the update for the context updater to propagate changes when we detect them.
    protected internal override BindingResult Update(in BindingContext context)
    {
        if (IsBindingContextUninitialized())
        {
            Unbind();
            return default;
        }
        bindingContext?.UpdateIfNecessary(context.targetElement);
        return OnUpdate(in context);
    }

    public override BindingResult OnUpdate(in BindingContext context)
    {
        if (isReleased)
        {
            return new BindingResult(BindingStatus.Pending);
        }

        if (bindingContext != null)
        {
            ResetUpdate();

            if (lastTrackedObjectRevision != bindingContext.lastRevision)
            {
                lastTrackedObjectRevision = bindingContext.lastRevision;

                if (bindingContext.RequestSerializedObject(out var so))
                    registeredCallbacks?.Invoke(owner, so);
            }
            return default;
        }

        return new BindingResult(BindingStatus.Pending);
    }

    public override void OnRelease()
    {
        if (isReleased)
            return;

        if (owner != null && bindingContext != null)
        {
            UnregisterTrackedProperties();
        }

        trackedProperties.Clear();
        contextUpdaterOwner = null;
        isActivated = false;

        ResetContext();

        registeredCallbacks = null;
        ResetCachedValues();
        isReleased = true;
    }

    public void AddTracking(SerializedProperty prop, Action<object, SerializedProperty> callback)
    {
        trackedProperties.Add(new TrackedPropertyRegistration(prop.hashCodeForPropertyPath, prop.propertyPath, callback));

        if(isActivated)
            bindingContext?.RegisterSerializedPropertyChangeCallback(owner, prop, callback);
    }

    public void RemoveTracking(SerializedProperty prop, Action<object, SerializedProperty> callback)
    {
        var propertyPathHash = prop.hashCodeForPropertyPath;

        for (var i = trackedProperties.Count - 1; i >= 0; i--)
        {
            if (trackedProperties[i].propertyPathHash == propertyPathHash && trackedProperties[i].callback == callback)
            {
                trackedProperties.RemoveAt(i);

                if (isActivated && bindingContext != null)
                {
                    // Unregister the specific callback, and stop native tracking if this was the last callback
                    bindingContext.UnregisterSerializedPropertyChangeCallback(owner, propertyPathHash, callback);
                }

                // Auto-cleanup: If no more properties are tracked and no object callbacks are registered,
                // remove the updater to allow tracking properties from a different SerializedObject
                if (trackedProperties.Count == 0 && registeredCallbacks == null)
                {
                    // Clear the binding from the element BEFORE calling OnRelease() to ensure
                    // the element no longer references this updater
                    owner?.ClearBinding(BindingExtensions.s_SerializedBindingContextUpdaterId);
                    OnRelease();
                }

                break;
            }
        }
    }

    private void RegisterTrackedProperties()
    {
        if (bindingContext != null)
        {
            foreach (var trackedProp in trackedProperties)
            {
                SerializedProperty property = bindingContext.FindProperty(trackedProp.propertyPath);
                if(property != null)
                    bindingContext.RegisterSerializedPropertyChangeCallback(owner, property, trackedProp.callback);
            }
        }
    }

    private void UnregisterTrackedProperties()
    {
        if (bindingContext != null)
        {
            foreach (var trackedProp in trackedProperties)
            {
                bindingContext.UnregisterSerializedPropertyChangeCallback(owner, trackedProp.propertyPathHash);
            }
        }
    }

    protected override void ResetCachedValues()
    {
        lastTrackedObjectRevision = 0xFFFFFFFFFFFFFFFF;
    }

    private void Reset(in UndoRedoInfo undo)
    {
        ResetUpdate();
    }

    protected internal override void OnActivated(in BindingActivationContext context)
    {
        isActivated = true;
        RegisterTrackedProperties();

        base.OnActivated(in context);
        Undo.undoRedoEvent += Reset;
    }

    protected internal override void OnDeactivated(in BindingActivationContext context)
    {
        isActivated = false;
        UnregisterTrackedProperties();

        base.OnDeactivated(in context);
        Undo.undoRedoEvent -= Reset;
    }
}
