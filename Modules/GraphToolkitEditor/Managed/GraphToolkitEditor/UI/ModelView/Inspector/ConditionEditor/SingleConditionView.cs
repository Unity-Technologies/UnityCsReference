// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The base class for a single line condition view.
    /// </summary>
    [UnityRestricted]
    internal abstract class SingleConditionView : ConditionView
    {
        /// <summary>
        /// The USS class name added to this element.
        /// </summary>
        public static readonly string singleUssClassName = ussClassName.WithUssModifier("single");

        /// <summary>
        /// The USS class name added to the container element.
        /// </summary>
        public static readonly string containerUssClassName = ussClassName.WithUssElement(GraphElementHelper.containerName);

        /// <summary>
        /// The container element for the condition view fields.
        /// </summary>
        protected VisualElement m_Container;

        VisualElement m_IndentationSpacer;

        /// <inheritdoc />
        protected override VisualElement IndentationSpacer => m_IndentationSpacer;

        /// <inheritdoc />
        protected override void BuildUI()
        {
            base.BuildUI();

            var dragHandle = new VisualElement();
            dragHandle.AddToClassList(dragHandleUssClassName);
            Add(dragHandle);

            m_IndentationSpacer = new VisualElement();
            Add(m_IndentationSpacer);

            m_Container = new VisualElement();
            m_Container.AddToClassList(containerUssClassName);

            Add(m_Container);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            AddToClassList(singleUssClassName);
        }
    }
}
