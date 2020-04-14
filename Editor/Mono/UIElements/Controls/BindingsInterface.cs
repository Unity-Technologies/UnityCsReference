// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class SerializedObjectBindEvent : EventBase<SerializedObjectBindEvent>
    {
        private SerializedObject m_BindObject;
        public SerializedObject bindObject
        {
            get
            {
                return m_BindObject;
            }
        }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            this.propagation = EventPropagation.Cancellable; // Also makes it not propagatable.
            m_BindObject = null;
        }

        public static SerializedObjectBindEvent GetPooled(SerializedObject obj)
        {
            SerializedObjectBindEvent e = GetPooled();
            e.m_BindObject = obj;
            return e;
        }

        public SerializedObjectBindEvent()
        {
            LocalInit();
        }
    }

    internal class SerializedPropertyBindEvent : EventBase<SerializedPropertyBindEvent>
    {
        private SerializedProperty m_BindProperty;
        public SerializedProperty bindProperty
        {
            get
            {
                return m_BindProperty;
            }
        }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            this.propagation = EventPropagation.Cancellable; // Also makes it not propagatable.
            m_BindProperty = null;
        }

        public static SerializedPropertyBindEvent GetPooled(SerializedProperty obj)
        {
            SerializedPropertyBindEvent e = GetPooled();
            e.m_BindProperty = obj;
            return e;
        }

        public SerializedPropertyBindEvent()
        {
            LocalInit();
        }
    }

    internal interface ISerializedObjectBindingImplementation
    {
        void Bind(VisualElement element, SerializedObject obj);
        void Unbind(VisualElement element);
        SerializedProperty BindProperty(IBindable field, SerializedObject obj);
        void BindProperty(IBindable field, SerializedProperty property);

        void Bind(VisualElement element, object bindingContext, SerializedProperty parentProperty);

        void HandleStyleUpdate(VisualElement element);
    }

    public static class BindingExtensions
    {
        public static readonly string prefabOverrideUssClassName = "unity-binding--prefab-override";
        internal static readonly string prefabOverrideBarName = "unity-binding-prefab-override-bar";
        internal static readonly string prefabOverrideBarContainerName = "unity-prefab-override-bars-container";
        internal static readonly string prefabOverrideBarUssClassName = "unity-binding__prefab-override-bar";
        internal static readonly string animationAnimatedUssClassName = "unity-binding--animation-animated";
        internal static readonly string animationRecordedUssClassName = "unity-binding--animation-recorded";
        internal static readonly string animationCandidateUssClassName = "unity-binding--animation-candidate";

        internal static ISerializedObjectBindingImplementation bindingImpl =
            new DefaultSerializedObjectBindingImplementation();

        public static void Bind(this VisualElement element, SerializedObject obj)
        {
            bindingImpl.Bind(element, obj);
        }

        public static void Unbind(this VisualElement element)
        {
            bindingImpl.Unbind(element);
        }

        public static SerializedProperty BindProperty(this IBindable field, SerializedObject obj)
        {
            return bindingImpl.BindProperty(field, obj);
        }

        public static void BindProperty(this IBindable field, SerializedProperty property)
        {
            bindingImpl.BindProperty(field, property);
        }

        internal static void HandleStyleUpdate(VisualElement element)
        {
            bindingImpl.HandleStyleUpdate(element);
        }
    }
}
