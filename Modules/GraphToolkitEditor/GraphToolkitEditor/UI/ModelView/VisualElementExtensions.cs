// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Extension methods for <see cref="VisualElement"/>.
    /// </summary>
    [UnityRestricted]
    internal static class VisualElementExtensions
    {
        /// <summary>
        /// Caches the previous class name and replaces it with a new one.
        /// </summary>
        /// <param name="ve">The VisualElement to act upon</param>
        /// <param name="newClassName">The new class name.</param>
        /// <param name="classNameCache">The cache.</param>
        /// <remarks>
        /// 'ReplaceAndCacheClassName' replaces the current class name of a <see cref="VisualElement"/> with a new one and caches the previous class name for potential future use.
        /// This method is useful for managing dynamic class name changes in UI elements, while maintaining a record of the previous state.
        /// </remarks>
        public static void ReplaceAndCacheClassName(this VisualElement ve, string newClassName, ref string classNameCache)
        {
            if (newClassName == classNameCache)
                return;
            if (classNameCache != null)
                ve.RemoveFromClassList(classNameCache);
            classNameCache = newClassName;
            if (classNameCache != null)
                ve.AddToClassList(classNameCache);
        }

        public static void PreallocForMoreClasses(this VisualElement ve, int numNewClasses)
        {
            var classList = (List<string>)ve.GetClasses();
            if (classList.Count + numNewClasses > classList.Capacity)
                classList.Capacity = classList.Count + numNewClasses;
        }

        /// <summary>
        /// Replaces a manipulator by another one.
        /// </summary>
        /// <param name="ve">The VisualElement to act upon.</param>
        /// <param name="manipulator">The manipulator to remove.</param>
        /// <param name="newManipulator">The manipulator to add.</param>
        /// <typeparam name="T">The type of the manipulators.</typeparam>
        public static void ReplaceManipulator<T>(this VisualElement ve, ref T manipulator, T newManipulator) where T : Manipulator
        {
            ve.RemoveManipulator(manipulator);
            manipulator = newManipulator;
            ve.AddManipulator(newManipulator);
        }

        /// <summary>
        /// Queries a VisualElement for a descendant that matches some criteria. Same as VisualElement.Q(),
        /// but behaves better when <paramref name="e"/> is null.
        /// </summary>
        /// <param name="e">The VisualElement to search.</param>
        /// <param name="name">The name of the descendant to find. Null if this criterion should be ignored.</param>
        /// <param name="className">The USS class name of the descendant to find. Null if this criterion should be ignored.</param>
        /// <typeparam name="T">The type of the descendant to find.</typeparam>
        /// <returns>The VisualElement that matches the search criteria.</returns>
        internal static T SafeQ<T>(this VisualElement e, string name = null, string className = null) where T : VisualElement
        {
            return e?.Q<T>(name, className);
        }

        /// <summary>
        /// Queries a VisualElement for a descendant that matches some criteria. Same as VisualElement.Q(),
        /// but behaves better when <paramref name="e"/> is null.
        /// </summary>
        /// <param name="e">The VisualElement to search.</param>
        /// <param name="name">The name of the descendant to find. Null if this criterion should be ignored.</param>
        /// <param name="className">The USS class name of the descendant to find. Null if this criterion should be ignored.</param>
        /// <returns>The VisualElement that matches the search criteria.</returns>
        internal static VisualElement SafeQ(this VisualElement e, string name = null, string className = null)
        {
            return e?.Q(name, className);
        }

        /// <summary>
        /// Queries a VisualElement for a descendant that matches some criteria. Same as VisualElement.SafeQ(),
        /// but throws when no element is found or <paramref name="e"/> is null.
        /// </summary>
        /// <param name="e">The VisualElement to search.</param>
        /// <param name="name">The name of the descendant to find. Null if this criterion should be ignored.</param>
        /// <param name="className">The USS class name of the descendant to find. Null if this criterion should be ignored.</param>
        /// <returns>The VisualElement that matches the search criteria.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="e"/> is null.</exception>
        /// <exception cref="Exception">If no element is found.</exception>
        internal static T MandatoryQ<T>(this VisualElement e, string name = null, string className = null) where T : VisualElement
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            var element = e.Q<T>(name, className);
            if (element == null)
                throw new Exception("Element not found: " + name);
            return element;
        }

        /// <summary>
        /// Queries a VisualElement for a descendant that matches some criteria. Same as VisualElement.SafeQ(),
        /// but throws when no element is found or <paramref name="e"/> is null.
        /// </summary>
        /// <param name="e">The VisualElement to search.</param>
        /// <param name="name">The name of the descendant to find. Null if this criterion should be ignored.</param>
        /// <param name="className">The USS class name of the descendant to find. Null if this criterion should be ignored.</param>
        /// <returns>The VisualElement that matches the search criteria.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="e"/> is null.</exception>
        /// <exception cref="Exception">If no element is found.</exception>
        internal static VisualElement MandatoryQ(this VisualElement e, string name = null, string className = null)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            var element = e.Q<VisualElement>(name, className);
            if (element == null)
                throw new Exception("Element not found: " + name);
            return element;
        }

        internal static VisualElement GetFirstAncestorWhere(this VisualElement ve, Predicate<VisualElement> predicate)
        {
            for (var parent = ve.hierarchy.parent; parent != null; parent = parent.hierarchy.parent)
            {
                if (predicate(parent))
                    return parent;
            }
            return null;
        }

        /// <summary>
        /// Call this on a VisualElement to ensure that any PointerDown events take mouse capture.
        /// This is useful when the VisualElement is a child of something else that captures the mouse as that would prevent other PointerDown/Up events from being invoked.
        /// </summary>
        /// <param name="ve"></param>
        internal static void MakeElementCapturePointer(this VisualElement ve)
        {
            ve.RegisterCallback<PointerDownEvent>(PointerDown);
            ve.RegisterCallback<PointerUpEvent>(PointerUp);

            void OptionsCaptureLost(PointerCaptureOutEvent e)
            {
                ve.ReleaseMouse();
                ve.UnregisterCallback<PointerCaptureOutEvent>(OptionsCaptureLost);
            }

            void PointerUp(PointerUpEvent evt) => ve.ReleaseMouse();

            void PointerDown(PointerDownEvent evt)
            {
                ve.RegisterCallback<PointerCaptureOutEvent>(OptionsCaptureLost);
                ve.CaptureMouse();
            }
        }
    }
}
