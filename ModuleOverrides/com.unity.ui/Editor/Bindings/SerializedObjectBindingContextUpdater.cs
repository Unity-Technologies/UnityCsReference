// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings;

internal sealed class SerializedObjectBindingContextUpdater : SerializedObjectBindingBase
{
    private VisualElement owner;

    private UInt64 lastTrackedObjectRevision = 0xFFFFFFFFFFFFFFFF;

    public event Action<object, SerializedObject> registeredCallbacks;

    public static SerializedObjectBindingContextUpdater Create(VisualElement owner, SerializedObjectBindingContext context)
    {
        var b = new SerializedObjectBindingContextUpdater();

        b.isReleased = false;
        b.SetContext(context, null);
        b.owner = owner;
        b.lastTrackedObjectRevision = context.lastRevision;
        return b;
    }

    public SerializedObjectBindingContextUpdater()
    {
        trackedPropertiesHash = new List<int>();
    }

    private List<int> trackedPropertiesHash { get; }

    public void AddTracking(SerializedProperty prop)
    {
        trackedPropertiesHash.Add(prop.hashCodeForPropertyPath);
    }

    public override void Update()
    {
        if (isReleased)
            return;

        if (!bindingContext.IsValid())
        {
            Release();
            return;
        }

        if (bindingContext != null)
        {
            ResetUpdate();

            if (lastTrackedObjectRevision != bindingContext.lastRevision)
            {
                lastTrackedObjectRevision = bindingContext.lastRevision;

                registeredCallbacks?.Invoke(owner, bindingContext.serializedObject);
                return;
            }
        }
    }

    public override void Release()
    {
        if (isReleased)
            return;

        if (owner != null && bindingContext != null)
        {
            foreach (var propHash in trackedPropertiesHash)
            {
                bindingContext.UnregisterSerializedPropertyChangeCallback(owner, propHash);
            }
        }

        trackedPropertiesHash.Clear();
        owner = null;

        ResetContext();

        registeredCallbacks = null;
        ResetCachedValues();
        isReleased = true;
    }

    protected override void ResetCachedValues()
    {
        lastTrackedObjectRevision = 0xFFFFFFFFFFFFFFFF;
    }
}
