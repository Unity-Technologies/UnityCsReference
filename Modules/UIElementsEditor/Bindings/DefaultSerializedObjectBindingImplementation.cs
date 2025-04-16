// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings;

internal class DefaultSerializedObjectBindingImplementation : ISerializedObjectBindingImplementation
{
    public void Bind(VisualElement element, SerializedObject obj)
    {
        element.SetProperty(BindingExtensions.s_DataSourceProperty, obj);
        if (element.panel != null || element is EditorElement || element is InspectorElement)
        {
            var context = FindOrCreateBindingContext(element, obj);
            context.Bind(element);
            return;
        }

        //wait later
        CreateBindingRequest(element, obj);
    }

    public void Unbind(VisualElement element)
    {
        element?.SetProperty(BindingExtensions.s_DataSourceProperty, null);
        UnbindTree(element);
    }

    private void UnbindTree(VisualElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        var context = SerializedObjectBindingContext.GetBindingContextFromElement(element);
        RemoveBindingRequest(element);
        (element.GetBinding(BindingExtensions.s_SerializedBindingId) as SerializedObjectBindingBase)?.OnRelease();
        (element.GetBinding(BindingExtensions.s_SerializedBindingContextUpdaterId) as SerializedObjectBindingBase)?.OnRelease();
        element.ClearBinding(BindingExtensions.s_SerializedBindingId);
        element.ClearBinding(BindingExtensions.s_SerializedBindingContextUpdaterId);

        if (context != null)
        {
            context.Unbind(element);
        }
        else
        {
            var childCount = element.hierarchy.childCount;

            for (int i = 0; i < childCount; ++i)
            {
                UnbindTree(element.hierarchy[i]);
            }
        }
    }

    static Dictionary<object, object> GetTemporaryCache(VisualElement element)
    {
        var updater = element.elementPanel?.GetUpdater(VisualTreeUpdatePhase.Bindings) as VisualTreeBindingsUpdater;
        return updater?.temporaryObjectCache;
    }

    static SerializedObjectBindingContext FindOrCreateBindingContext(VisualElement element, SerializedObject obj)
    {
        var context = SerializedObjectBindingContext.FindBindingContext(element, obj);

        if (context == null || !context.TargetsSerializedObject(obj))
        {
            //we couldn't find the context, let's try to find it elsewhere
            var cookie = GetTemporaryCache(element);

            if (cookie != null && obj != null)
            {
                if (cookie.TryGetValue(obj, out var c))
                {
                    context = c as SerializedObjectBindingContext;
                }
            }

            if (context == null)
            {
                context = new SerializedObjectBindingContext(obj);

                if (obj != null)
                {
                    cookie?.Add(obj, context);
                }
            }
        }

        return context;
    }

    public SerializedProperty BindProperty(IBindable field, SerializedObject obj)
    {
        var property = obj?.FindProperty(field.bindingPath);

        if (property != null)
        {
            var element = field as VisualElement;

            if (element != null)
            {
                if (element.panel == null)
                {
                    // wait until element is attached
                    CreateBindingRequest(element, property.serializedObject);
                }
                else
                {
                    var context = FindOrCreateBindingContext(element, obj);
                    context.Bind(element);
                }
            }
        }

        return property;
    }

    public void BindProperty(IBindable field, SerializedProperty property)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        field.bindingPath = property.propertyPath;

        var element = field as VisualElement;

        if (element != null)
        {
            if (element.panel == null)
            {
                // wait until element is attached
                CreateBindingRequest(element, property.serializedObject);
            }
            else
            {
                var context = FindOrCreateBindingContext(element, property.serializedObject);
                context.Bind(element);
            }
        }
    }

    public void TrackPropertyValue(VisualElement element, SerializedProperty property, Action<object, SerializedProperty> callback)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        if (element != null)
        {
            var request = BindingRequest.Create(BindingRequest.RequestType.TrackProperty, property.serializedObject);
            request.parentProperty = property.Copy();
            request.obj = property.serializedObject;

            if (callback != null)
            {
                request.callback = callback;
            }
            else
            {
                request.callback = SendPropertyChangeCallback;
            }

            if (element.panel == null)
            {
                // wait until element is attached
                VisualTreeBindingsUpdater.AddBindingRequest(element, request);
            }
            else
            {
                request.Bind(element);
                request.Release();
            }
        }
    }

    private static void SendPropertyChangeCallback(object cookie, SerializedProperty prop)
    {
        if (cookie is VisualElement element)
        {
            using (SerializedPropertyChangeEvent evt = SerializedPropertyChangeEvent.GetPooled())
            {
                // Event can be dispatched async so we need a copy of the SerializedProperty as this one
                // will be disposed soon by the SerializedObjectChangeTracker
                evt.changedProperty = prop.Copy();
                evt.elementTarget = element;
                element.SendEvent(evt);
            }
        }
    }

    private static void SendObjectChangeCallback(object cookie, SerializedObject obj)
    {
        if (cookie is VisualElement element)
        {
            using (SerializedObjectChangeEvent evt = SerializedObjectChangeEvent.GetPooled(obj))
            {
                evt.elementTarget = element;
                element.SendEvent(evt);
            }
        }
    }

    public void TrackSerializedObjectValue(VisualElement element, SerializedObject serializedObject, Action<SerializedObject> callback)
    {
        if (serializedObject == null)
        {
            throw new ArgumentNullException(nameof(serializedObject));
        }

        if (element != null)
        {
            var request = BindingRequest.Create(BindingRequest.RequestType.TrackObject, serializedObject);

            request.obj = serializedObject;

            if (callback != null)
            {
                request.objectChangedCallback = (e, o) => callback(o);
            }
            else
            {
                request.objectChangedCallback = (o, obj) => SendObjectChangeCallback(o, obj);
            }

            if (element.panel == null)
            {
                // wait until element is attached
                VisualTreeBindingsUpdater.AddBindingRequest(element, request);
            }
            else
            {
                request.Bind(element);
                request.Release();
            }
        }
    }

    // visual element style changes wrt its property state

    void ISerializedObjectBindingImplementation.Bind(VisualElement element, object objWrapper, SerializedProperty parentProperty)
    {
        var context = objWrapper as SerializedObjectBindingContext;

        if (context == null)
        {
            if (element.panel == null)
            {
                CreateBindingRequest(element, parentProperty.serializedObject);
                return;
            }
            context = FindOrCreateBindingContext(element, parentProperty.serializedObject);
        }

        context.BindTree(element, parentProperty);
    }

    private static void CreateBindingRequest(VisualElement element, SerializedObject obj)
    {
        var request = BindingRequest.Create(BindingRequest.RequestType.Bind, obj);
        VisualTreeBindingsUpdater.AddBindingRequest(element, request);
    }

    private static void RemoveBindingRequest(VisualElement element)
    {
        VisualTreeBindingsUpdater.ClearBindingRequests(element);
    }

    public void HandleStyleUpdate(VisualElement element)
    {
        var binding = element.GetBinding(BindingExtensions.s_SerializedBindingId) as SerializedObjectBindingBase;
        if (binding?.boundProperty == null)
            return;

        BindingsStyleHelpers.UpdateElementStyle(element, binding.boundProperty);
    }

    internal class BindingRequest : IBindingRequest
    {
        public static ObjectPool<BindingRequest> s_Pool =
            new ObjectPool<BindingRequest>(() => new BindingRequest(), 32);

        public enum RequestType
        {
            Bind,
            DelayBind,
            TrackProperty,
            TrackObject
        }

        public SerializedObject obj;
        public SerializedObjectBindingContext context;
        public SerializedProperty parentProperty;
        public Action<object, SerializedProperty> callback;
        public Action<object, SerializedObject> objectChangedCallback;
        public RequestType requestType;

        public static BindingRequest Create(RequestType reqType, SerializedObject obj)
        {
            var req = s_Pool.Get();
            req.requestType = reqType;
            req.obj = obj;
            return req;
        }

        public static BindingRequest CreateDelayBinding(SerializedObjectBindingContext context, SerializedProperty parentProperty)
        {
            var req = s_Pool.Get();
            req.requestType = RequestType.DelayBind;
            req.context = context;
            req.parentProperty = parentProperty;
            return req;
        }

        public void Bind(VisualElement element)
        {
            context ??= FindOrCreateBindingContext(element, obj);

            if (!context.IsValid()) // Sometimes our serializedObject might have vanished, after a domain reload
            {
                return;
            }

            switch (requestType)
            {
                case RequestType.Bind:
                    context.Bind(element);
                    break;
                case RequestType.DelayBind:
                    if (parentProperty == null || parentProperty.isValid)
                    {
                        context.ContinueBinding(element, parentProperty);
                    }
                    break;
                case RequestType.TrackProperty:
                {
                    if (parentProperty != null && parentProperty.isValid)
                    {
                        var contextUpdater = context.AddBindingUpdater(element);
                        contextUpdater.AddTracking(parentProperty, callback as Action<object, SerializedProperty>);
                    }
                }
                    break;
                case RequestType.TrackObject:
                {
                    var contextUpdater = context.AddBindingUpdater(element);
                    contextUpdater.registeredCallbacks += objectChangedCallback;
                }
                    break;
                default:
                    break;
            }
        }

        public void Release()
        {
            obj = null;
            context = null;
            parentProperty = null;
            callback = null;
            objectChangedCallback = null;
            requestType = RequestType.Bind;
            s_Pool.Release(this);
        }
    }
}
