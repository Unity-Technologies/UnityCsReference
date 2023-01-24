// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The section type. The inspector only supports those three types.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    enum SectionType
    {
        [Obsolete("SectionType.Settings has been deprecated. Use SectionType.Options instead")]
        Settings = 0,
        Options = 0,
        Properties,
        Advanced
    }

    /// <summary>
    /// The view model for an inspector section.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class InspectorSectionModel : Model, IHasTitle, ICollapsible
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
        public virtual string Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        /// <inheritdoc />
        public virtual string DisplayTitle => Title;

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
