// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Class that handles the color of a graph element model that is colorable.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal struct ElementColor
    {
        [SerializeField, HideInInspector]
        Color m_Color;

        [SerializeField, HideInInspector]
        bool m_HasUserColor;

        Color DefaultColor => (OwnerElementModel as IHasElementColor)?.DefaultColor ?? default;

        GraphElementModel m_OwnerElementModel;

        /// <summary>
        /// The owner of the color.
        /// </summary>
        public GraphElementModel OwnerElementModel
        {
            get => m_OwnerElementModel;
            set => m_OwnerElementModel = value;
        }

        /// <summary>
        /// True if the color was changed.
        /// </summary>
        /// <remarks>Setter implementations should set the <see cref="ChangeHint.Style"/> change hints.</remarks>
        public bool HasUserColor => m_HasUserColor;

        /// <summary>
        /// Colors for the element.
        /// </summary>
        /// <remarks>
        /// Setting a color different from the default one sets <see cref="HasUserColor"/> to true. Setting it to the default color sets it to false.
        /// </remarks>
        public Color Color
        {
            get => HasUserColor ? m_Color : DefaultColor;
            set
            {
                if (m_OwnerElementModel == null || !m_OwnerElementModel.IsColorable())
                    return;

                m_HasUserColor = DefaultColor != value;

                if (m_Color == value)
                    return;

                m_Color = value;
                m_OwnerElementModel.GraphModel?.CurrentGraphChangeDescription.AddChangedModel(m_OwnerElementModel, ChangeHint.Style);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementColor"/> class.
        /// </summary>
        public ElementColor(GraphElementModel ownerElementModel, Color color = default, bool hasUserColor = false)
        {
            m_OwnerElementModel = ownerElementModel;
            m_Color = color;
            m_HasUserColor = hasUserColor;
        }
    }
}
