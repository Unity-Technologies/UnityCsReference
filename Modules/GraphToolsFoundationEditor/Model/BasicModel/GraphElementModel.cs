// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for a model that represents an element in a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    abstract class GraphElementModel : Model, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        Color m_Color;

        [SerializeField, HideInInspector]
        bool m_HasUserColor;

        [SerializeField, HideInInspector]
        SerializationVersion m_Version;

        protected List<Capabilities> m_Capabilities = new();

        /// <summary>
        /// Serialized version, used for backward compatibility
        /// </summary>
        public SerializationVersion Version => m_Version;

        /// <summary>
        /// The graph model to which the element belongs.
        /// </summary>
        public virtual GraphModel GraphModel { get; set; }

        /// <summary>
        /// The list of capabilities of the element.
        /// </summary>
        public IReadOnlyList<Capabilities> Capabilities => m_Capabilities;

        /// <summary>
        /// Used for backward compatibility
        /// </summary>
        protected internal Color SerializedColor_Internal => m_Color;

        /// <summary>
        /// Default Color to use when no user color is provided
        /// </summary>
        public virtual Color DefaultColor => Color.clear;

        /// <summary>
        /// Color for the element.
        /// </summary>
        /// <remarks>
        /// Setting a color should set HasUserColor to true.
        /// </remarks>
        public virtual Color Color
        {
            get => HasUserColor ? m_Color : DefaultColor;
            set
            {
                if (this.IsColorable())
                {
                    m_HasUserColor = true;
                    m_Color = value;
                }
            }
        }

        /// <summary>
        /// True if the color was changed.
        /// </summary>
        public virtual bool HasUserColor => m_HasUserColor;

        /// <summary>
        /// The container for this graph element.
        /// </summary>
        public virtual IGraphElementContainer Container => GraphModel;

        /// <summary>
        /// Version number for serialization.
        /// </summary>
        /// <remarks>
        /// Useful for models backward compatibility
        /// </remarks>
        public enum SerializationVersion
        {
            // Use package release number as the name of the version.

            // ReSharper disable once InconsistentNaming
            GTF_V_0_8_2 = 0,

            // ReSharper disable once InconsistentNaming
            GTF_V_0_13_0 = 1,

            /// <summary>
            /// Keep Latest as the highest value in this enum
            /// </summary>
            Latest
        }

        /// <summary>
        /// Reset the color to its original state.
        /// </summary>
        /// <remarks>
        /// Resetting a color should set HasUserColor to false.
        /// </remarks>
        public virtual void ResetColor()
        {
            m_HasUserColor = false;
        }

        /// <inheritdoc />
        public virtual void OnBeforeSerialize()
        {
            m_Version = SerializationVersion.Latest;
        }

        public virtual void OnAfterDeserialize()
        {
            GraphModel = null;
        }
    }
}
