// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for conditions on <see cref="TransitionModel"/>.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal abstract class ConditionModel : GraphElementModel, IHasTitle
    {
        [SerializeField]
        string m_Title;

        /// <inheritdoc />
        public string Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        /// <summary>
        /// The index of this condition in its parent.
        /// </summary>
        public int IndexInParent { get; private set; }

        public virtual TransitionModel Transition { get; set; }

        /// <summary>
        /// The parent condition of this <see cref="ConditionModel"/>.
        /// </summary>
        public GroupConditionModel Parent { get; private set; }

        /// <inheritdoc />
        public override IGraphElementContainer Container => (IGraphElementContainer)Parent ?? Transition;

        /// <summary>
        /// Computes a hash code for this <see cref="ConditionModel"/>.
        /// </summary>
        /// <returns>A hash code for this <see cref="ConditionModel"/>.</returns>
        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }

        /// <summary>
        /// Returns a human-readable string representation of this <see cref="ConditionModel"/>.
        /// </summary>
        /// <param name="indentLevel">The level of indentation added.</param>
        /// <returns>A human-readable string representation of this <see cref="ConditionModel"/>.</returns>
        public abstract string ToString(int indentLevel = 0);

        /// <summary>
        /// Gets a string to indent the output of <see cref="ToString"/> based on the specified indent level.
        /// </summary>
        /// <param name="indentLevel">The level of indentation wanted.</param>
        /// <returns>The indentation string.</returns>
        /// <remarks>
        /// 'GetIndentationString' generates a string that contains indentation based on the specified 'indentLevel'.
        /// This method is used within <see cref="ToString"/> to format the output of a <see cref="ConditionModel"/>,
        /// which ensures that its string representation is more readable and structured. Use this method when
        /// indentation improves clarity, such as when displaying hierarchical or nested conditions. The 'indentLevel'
        /// parameter determines how many indentation units are applied. A higher value results in greater indentation,
        /// which makes nested structures more distinguishable.
        /// </remarks>
        protected static string GetIndentationString(int indentLevel)
        {
            string result = string.Empty;
            if (indentLevel > 0)
                result = new string('\t', indentLevel);
            return result;
        }

        protected internal void SetIndexInParent(int index)
        {
            IndexInParent = index;
        }

        protected internal void SetParent(GroupConditionModel parent)
        {
            Parent = parent;
            GraphModel = Parent?.GraphModel;
        }
    }
}
