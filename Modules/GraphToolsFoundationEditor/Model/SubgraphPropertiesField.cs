// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Holds information about a subgraph properties field.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    struct SubgraphPropertiesField
    {
        [SerializeField]
        bool m_ShouldShowInLibrary;

        [SerializeField]
        string m_DisplayedPath;

        [SerializeField]
        string m_Description;

        string m_WarningMessage;

        /// <summary>
        /// Creates a new instance of the <see cref="SubgraphPropertiesField"/> class.
        /// </summary>
        /// <param name="warningMessage">The warning message to be shown if the 'show in node library option' is checked but the conditions for the graph to be usable as a subgraph are not met.</param>
        /// <param name="defaultCategoryPath">The default path to the subgraph in the node library.</param>
        /// <returns>A new instance of <see cref="SubgraphPropertiesField"/>.</returns>
        public SubgraphPropertiesField(string warningMessage, string defaultCategoryPath = "Subgraphs/")
        {
            m_WarningMessage = warningMessage;
            m_DisplayedPath = defaultCategoryPath;
            m_ShouldShowInLibrary = true;
            m_Description = "";
        }

        /// <summary>
        /// Whether the graph should be shown in the node library or not.
        /// </summary>
        public bool ShouldShowInLibrary
        {
            get => m_ShouldShowInLibrary;
            set => m_ShouldShowInLibrary = value;
        }

        /// <summary>
        /// Gets the category path to the subgraph in the node library.
        /// </summary>
        /// <remarks>Any '/' character at the end of the string is removed to avoid the creation of empty categories in the node library.</remarks>
        /// <returns>The category path to the subgraph in the node library.</returns>
        public string GetCategoryPath() => DisplayedPath.EndsWith("/") ? DisplayedPath.TrimEnd('/') : DisplayedPath;

        /// <summary>
        /// The displayed path to the subgraph in the node library.
        /// </summary>
        public string DisplayedPath
        {
            get => m_DisplayedPath;
            set => m_DisplayedPath = value;
        }

        /// <summary>
        /// The graph's description.
        /// </summary>
        public string Description
        {
            get => m_Description;
            set => m_Description = value;
        }

        /// <summary>
        /// The warning message to be shown if the 'show in node library option' is checked but the conditions for the graph to be usable as a subgraph are not met.
        /// </summary>
        public string WarningMessage => m_WarningMessage;
    }
}
