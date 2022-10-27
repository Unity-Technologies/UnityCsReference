// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// The cache that appear in front of a node's UI when the zoom is below a certain level.
    class NodeLodCachePart: GraphElementPart
    {
        /// <summary>
        /// Creates a new instance of the <see cref="NodeLodCachePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="NodeLodCachePart"/>.</returns>
        public static NodeLodCachePart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            if (model is IHasTitle)
            {
                return new NodeLodCachePart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        NodeLodCachePart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        Label m_Label;
        VisualElement m_ColorLine;
        VisualElement m_Root;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement();

            m_ColorLine = new VisualElement();
            m_ColorLine.AddToClassList(m_ParentClassName.WithUssElement("color-line"));
            m_Root.Add(m_ColorLine);

            m_Label = new Label();
            m_Label.AddToClassList(m_ParentClassName.WithUssElement("cache-label"));
            m_Root.Add(m_Label);

            parent.Add(m_Root);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement("cache"));
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if( m_Model is IHasTitle titled)
                m_Label.text = titled.Title;
            if (m_Model is AbstractNodeModel node)
            {
                if (node.HasUserColor)
                {
                    m_ColorLine.style.backgroundColor = node.Color;
                }
                else
                {
                    m_ColorLine.style.backgroundColor = StyleKeyword.Null;
                }

                m_Label.tooltip = node.Tooltip;
                m_Root.tooltip = node.Tooltip;
                m_ColorLine.tooltip = node.Tooltip;
            }
        }

        /// <inheritdoc />
        public override void SetLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            base.SetLevelOfDetail(zoom, newZoomMode, oldZoomMode);

            if (newZoomMode != oldZoomMode)
            {
                if (m_OwnerElement.RootView.ClassListContains(GraphView.ussSmallModifierClassName))
                {
                    m_Root.style.visibility = StyleKeyword.Null;
                }
                else
                {
                    m_Root.style.visibility = Visibility.Hidden;
                }

                if (m_OwnerElement.RootView.ClassListContains(GraphView.ussVerySmallModifierClassName))
                {
                    m_Label.style.visibility = Visibility.Hidden;
                }
                else
                {
                    m_Label.style.visibility = StyleKeyword.Null;
                }
            }
        }
    }
}
