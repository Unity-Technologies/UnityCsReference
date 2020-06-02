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
