// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class CapsuleNodeLodCachePart : GraphElementPart
    {
        /// <summary>
        /// Creates a new instance of the <see cref="CapsuleNodeLodCachePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="CapsuleNodeLodCachePart"/>.</returns>
        public static CapsuleNodeLodCachePart Create(string name, Model model, ChildView ownerElement, string parentClassName)
        {
            if (model is ISingleInputPortNodeModel or ISingleOutputPortNodeModel)
            {
                return new CapsuleNodeLodCachePart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CapsuleNodeLodCachePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected CapsuleNodeLodCachePart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        protected Image m_Icon;
        protected VisualElement m_Background;
        protected VisualElement m_Root;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildUI(VisualElement parent)
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

        TypeHandle m_CurrentTypeHandle;

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            if (m_Model is InputOutputPortsNodeModel node)
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var output = node.GetPorts().First();
#pragma warning restore RS0030

                Color color = output.GetView<Port>(m_OwnerElement.RootView)?.PortColor ?? Color.white;

                m_Icon.tintColor = color;
                if (m_CurrentTypeHandle != output.DataTypeHandle)
                {
                    m_OwnerElement.RootView.TypeHandleInfos.RemoveUssClasses(GraphElementHelper.iconDataTypeClassPrefix, m_Icon, output.DataTypeHandle);
                    m_CurrentTypeHandle = output.DataTypeHandle;
                    m_OwnerElement.RootView.TypeHandleInfos.AddUssClasses(GraphElementHelper.iconDataTypeClassPrefix, m_Icon, output.DataTypeHandle);
                }

                m_Background.style.backgroundColor = color;
            }
        }

        /// <inheritdoc />
        public override void SetLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            base.SetLevelOfDetail(zoom, newZoomMode, oldZoomMode);

            if (newZoomMode != oldZoomMode)
            {
                if (newZoomMode >= GraphViewZoomMode.Small)
                {
                    m_Root.style.visibility = StyleKeyword.Null;
                }
                else
                {
                    m_Root.style.visibility = Visibility.Hidden;
                }

                if (newZoomMode >= GraphViewZoomMode.VerySmall)
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
                m_Root.schedule.Execute(DoCompleteUpdate).ExecuteLater(0);
            }
        }

        /// <inheritdoc />
        public override bool SupportsCulling()
        {
            return false;
        }
    }
}
