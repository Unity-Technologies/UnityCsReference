// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    class TokenLodCachePart: GraphElementPart
    {
        /// <summary>
        /// Creates a new instance of the <see cref="TokenLodCachePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="TokenLodCachePart"/>.</returns>
        public static TokenLodCachePart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            if (model is ISingleInputPortNodeModel or ISingleOutputPortNodeModel)
            {
                return new TokenLodCachePart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        TokenLodCachePart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        Image m_Icon;
        VisualElement m_Background;
        VisualElement m_Root;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement();

            m_Icon = new Image();
            m_Icon.AddToClassList(m_ParentClassName.WithUssElement("cache-icon"));

            m_Background = new VisualElement();
            m_Background.AddToClassList(m_ParentClassName.WithUssElement("cache-background"));

            m_Root.Add(m_Background);
            m_Root.Add(m_Icon);

            parent.Add(m_Root);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement("cache"));
        }

        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();

            m_Icon.AddStylesheet_Internal("TypeIcons.uss");
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (m_Model is InputOutputPortsNodeModel node)
            {
                var output = node.Ports.First();

                Color color = output.GetView<Port>(m_OwnerElement.RootView)?.PortColor ?? Color.white;

                m_Icon.tintColor = color;
                m_Icon.PrefixEnableInClassList(Port.dataTypeClassPrefix, Port.GetClassNameSuffixForDataType_Internal(output.PortDataType));
                m_Background.style.backgroundColor = color;
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
                    m_Background.style.visibility = StyleKeyword.Null;
                    m_Icon.style.visibility = Visibility.Hidden;
                }
                else
                {
                    m_Background.style.visibility = Visibility.Hidden;
                    m_Icon.style.visibility = StyleKeyword.Null;
                }
            }

            if (oldZoomMode == GraphViewZoomMode.Unknown) // This is the first time this is called, in that case schedule
                                                          // another UpdatePartFromModel() because the port color is not known yet
            {
                m_Root.schedule.Execute(UpdatePartFromModel).ExecuteLater(0);
            }
        }
    }
}
