// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Extension methods for <see cref="VisualElement"/>.
    /// </summary>
    static class VisualElementExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="ve"></param>
        /// <param name="newClassName"></param>
        /// <param name="classNameCache"></param>
        public static void ReplaceAndCacheClassName(this VisualElement ve, string newClassName, ref string classNameCache)
        {
            if (newClassName == classNameCache)
                return;
            if (classNameCache != null)
                ve.RemoveFromClassList(classNameCache);
            classNameCache = newClassName;
            if( classNameCache != null)
                ve.AddToClassList(classNameCache);
        }

        public static void PreallocForMoreClasses(this VisualElement ve, int numNewClasses)
        {
            var classList = (List<string>)ve.GetClasses();
            if (classList.Count + numNewClasses > classList.Capacity)
                classList.Capacity = classList.Count + numNewClasses;
        }

        static List<string> s_PrefixEnableInClassListToRemove = new List<string>();

        /// <summary>
        /// Removes all USS classes that start with <paramref name="classNamePrefix"/> and add
        /// a USS class name <paramref name="classNamePrefix"/> + <paramref name="classNameSuffix"/>.
        /// </summary>
        /// <param name="ve">The VisualElement to act upon.</param>
        /// <param name="classNamePrefix">The class name prefix.</param>
        /// <param name="classNameSuffix">The class name suffix.</param>
        public static void PrefixEnableInClassList(this VisualElement ve, string classNamePrefix, string classNameSuffix)
        {
            var classAlreadyPresent = false;
            s_PrefixEnableInClassListToRemove.Clear();

            foreach (var c in ve.GetClasses())
            {
                if (c.StartsWith(classNamePrefix))
                {
                    // Note: string.Length is a stored value.
                    if (c.Length == classNamePrefix.Length + classNameSuffix.Length && c.EndsWith(classNameSuffix))
                    {
                        classAlreadyPresent = true;
                    }
                    else
                    {
                        s_PrefixEnableInClassListToRemove.Add(c);
                    }
                }
            }

            foreach (var c in s_PrefixEnableInClassListToRemove)
            {
                ve.RemoveFromClassList(c);
            }

            if (!classAlreadyPresent)
            {
                ve.AddToClassList(classNamePrefix + classNameSuffix);
            }
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
        public static T SafeQ<T>(this VisualElement e, string name = null, string className = null) where T : VisualElement
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
        public static VisualElement SafeQ(this VisualElement e, string name = null, string className = null)
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
        public static T MandatoryQ<T>(this VisualElement e, string name = null, string className = null) where T : VisualElement
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            return UQueryExtensions.MandatoryQ<T>(e, name, className);
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
        public static VisualElement MandatoryQ(this VisualElement e, string name = null, string className = null)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            return UQueryExtensions.MandatoryQ(e, name, className);
        }
    }
}
