// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A simple graph tool with one GraphObject type and one GraphModel type.
    /// </summary>
    [UnityRestricted]
    internal abstract class SimpleGraphTool : GraphTool
    {
        /// <summary>
        /// The default GraphTemplate for this tool.
        /// </summary>
        protected GraphTemplate m_DefaultGraphTemplate;

        /// <summary>
        /// The default <see cref="Type"/> of GraphModel for this tool.
        /// </summary>
        public abstract Type DefaultGraphModelType { get; }

        /// <summary>
        /// The default <see cref="Type"/> of GraphObject for this tool.
        /// </summary>
        public abstract Type DefaultGraphObjectType { get; }

        /// <summary>
        /// The name of one the objects of this tool. Will be used when referring generically to objects of this tool.
        /// </summary>
        public string GenericGraphObjectName { get; set; } = "Graph";

        /// <summary>
        /// The default extension for an asset of this tool.
        /// </summary>
        public string DefaultAssetExtension { get; set; } = "asset";

        /// <summary>
        /// The default graph template for this tool.
        /// </summary>
        public virtual GraphTemplate DefaultGraphTemplate
        {
            get
            {
                m_DefaultGraphTemplate ??= new SimpleGraphTemplate(this);
                return m_DefaultGraphTemplate;
            }
        }
    }

    /// <summary>
    /// A simple graph tool with one GraphObject type and one GraphModel type.
    /// </summary>
    /// <typeparam name="TGraphModel">The type of <see cref="GraphModel"/>.</typeparam>
    /// <typeparam name="TGraphObject">The type of <see cref="GraphObject"/>.</typeparam>
    [UnityRestricted]
    internal class SimpleGraphTool<TGraphModel, TGraphObject> : SimpleGraphTool
        where TGraphModel : GraphModel
        where TGraphObject : GraphObject
    {
        /// <summary>
        /// The default <see cref="Type"/> of GraphModel for this tool. Can be null, if the tool has multiple types of GraphModels.
        /// </summary>
        public override Type DefaultGraphModelType => typeof(TGraphModel);

        /// <summary>
        /// The default <see cref="Type"/> of GraphObject for this tool. Can be null, if the tool has multiple types of GraphAssets.
        /// </summary>
        public override Type DefaultGraphObjectType => typeof(TGraphObject);
    }
}
