// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Available section types in the inspector.
    /// </summary>
    /// <remarks>
    /// 'SectionType' defines the available section types in the inspector, so you can categorize content into distinct groups.
    /// Only four section types are supported: <see cref="SectionType.Options"/>, <see cref="SectionType.Properties"/>, <see cref="SectionType.Advanced"/>, and <see cref="SectionType.StateTransitions"/>.
    /// These sections help organize inspector content, which makes it easier to manage and edit settings.
    /// </remarks>
    [Serializable]
    [UnityRestricted]
    internal enum SectionType
    {
        /// <summary>
        /// The section type for options.
        /// </summary>
        Options = 0,
        /// <summary>
        /// The section type for properties.
        /// </summary>
        /// <remarks>Properties are settings that are present on the model.</remarks>
        Properties,
        /// <summary>
        /// The section type for advanced information.
        /// </summary>
        Advanced,
        /// <summary>
        /// The section type for state transitions.
        /// </summary>
        StateTransitions
    }

    /// <summary>
    /// The view model for an inspector section.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class InspectorSectionModel : Model, IHasTitle, ICollapsible
    {
        [SerializeField]
        string m_Title;

        [SerializeField]
        bool m_Collapsible = true;

        [SerializeField]
        SectionType m_SectionType;

        [SerializeField]
        bool m_Collapsed;

        /// <inheritdoc />
        /// <remarks>Setter implementations should set the <see cref="ChangeHint.Data"/> change hint.</remarks>
        public virtual string Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        /// <summary>
        /// The section type.
        /// </summary>
        public virtual SectionType SectionType
        {
            get => m_SectionType;
            set => m_SectionType = value;
        }

        /// <summary>
        /// Whether the section is collapsible.
        /// </summary>
        public virtual bool Collapsible
        {
            get => m_Collapsible;
            set => m_Collapsible = value;
        }

        /// <inheritdoc />
        public virtual bool Collapsed
        {
            get => m_Collapsed;
            set => m_Collapsed = value;
        }
    }
}
