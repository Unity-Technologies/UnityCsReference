// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Helper methods to clone graph elements.
    /// </summary>
    static class CloneHelpers
    {
        class Holder : ScriptableObject
        {
            [SerializeReference]
            public GraphElementModel m_Model;
        }

        /// <summary>
        /// Clones a graph element model.
        /// </summary>
        /// <param name="element">The element to clone.</param>
        /// <typeparam name="T">The type of the element to clone.</typeparam>
        /// <returns>A clone of the element.</returns>
        public static T Clone<T>(this T element) where T : GraphElementModel
        {
            if (element is ICloneable cloneable)
            {
                T copy = (T)cloneable.Clone();
                copy.AssignNewGuid();
                return copy;
            }

            return CloneUsingScriptableObjectInstantiate(element);
        }

        /// <summary>
        /// Clones a graph element model.
        /// </summary>
        /// <param name="element">The element to clone.</param>
        /// <typeparam name="T">The type of the element to clone.</typeparam>
        /// <returns>A clone of the element.</returns>
        public static T CloneUsingScriptableObjectInstantiate<T>(T element) where T : GraphElementModel
        {
            Holder h = ScriptableObject.CreateInstance<Holder>();
            h.m_Model = element;

            // TODO: wait for CopySerializedManagedFieldsOnly to be able to copy plain c# objects with [SerializeReference] fields
            //            var clone = (T)Activator.CreateInstance(element.GetType());
            //            EditorUtility.CopySerializedManagedFieldsOnly(element, clone);
            var h2 = Object.Instantiate(h);
            var clone = h2.m_Model;
            clone.AssignNewGuid();

            if (clone is IGraphElementContainer container)
                foreach (var subElement in container.GraphElementModels)
                {
                    if (subElement is ICloneable)
                        Debug.LogError("ICloneable is not supported on elements in IGraphElementsContainer");
                    subElement.AssignNewGuid();
                }

            Object.DestroyImmediate(h);
            Object.DestroyImmediate(h2);
            return (T)clone;
        }
    }
}
