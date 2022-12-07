// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The <see cref="GraphView.Layer"/> that contains all placemats.
    /// </summary>
    class PlacematContainer : GraphView.Layer
    {
        public static readonly string ussClassName = "ge-placemat-container";

        GraphView m_GraphView;

        public static int PlacematsLayer => Int32.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlacematContainer"/> class.
        /// </summary>
        /// <param name="graphView">The parent graph view.</param>
        public PlacematContainer(GraphView graphView)
        {
            m_GraphView = graphView;

            this.AddStylesheet_Internal("PlacematContainer.uss");
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Ignore;
        }

        /// <summary>
        /// Sort the placemat visual elements by their model Z order.
        /// </summary>
        public void UpdateElementsOrder()
        {
            Sort((a, b) => ((Placemat)a).PlacematModel.GetZOrder().CompareTo(((Placemat)b).PlacematModel.GetZOrder()));
        }
    }
}
