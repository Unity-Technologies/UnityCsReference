// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A visual element to display the number of transitions between two states.
    /// </summary>
    [UnityRestricted]
    internal class TransitionCounter : VisualElement
    {
        /// <summary>
        /// The USS class name added to a <see cref="TransitionCounter"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-transition-counter";

        /// <summary>
        /// The USS class name of the <see cref="TransitionCounter"/>'s label.
        /// </summary>
        public static readonly string labelElementUssClassName = ussClassName.WithUssElement(GraphElementHelper.labelName);

        Label m_Label;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransitionCounter"/> class.
        /// </summary>
        public TransitionCounter()
        {
            pickingMode = PickingMode.Ignore;

            AddToClassList(ussClassName);

            m_Label = new Label { name = GraphElementHelper.labelName, pickingMode = PickingMode.Ignore };
            m_Label.AddToClassList(labelElementUssClassName);
            m_Label.text = "0";
            Add(m_Label);

            style.visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Sets the count of transitions.
        /// </summary>
        /// <param name="count">The number of transitions.</param>
        /// <param name="manualCentering">True if the counter position should be computed from its
        /// <see cref="TransitionArrow"/> parent transform. False if the counter position should be computed by the layout system.</param>
        public void SetCount(int count, bool manualCentering)
        {
            m_Label.text = count.ToString();

            if (count > 1)
            {
                style.visibility = StyleKeyword.Null;

                if (manualCentering)
                {
                    style.position = Position.Absolute;
                    style.left = 0;
                    style.top = 0;
                    style.width = Length.Percent(100);
                    style.height = Length.Percent(100);
                }
                else
                {
                    style.position = StyleKeyword.Null;
                    style.left = StyleKeyword.Null;
                    style.top = StyleKeyword.Null;
                }
            }
            else
            {
                style.visibility = Visibility.Hidden;
            }
        }

        public void SetCenteringOffset(Vector2 offset)
        {
            if (style.position == Position.Absolute)
                style.translate = new Translate(offset.x, offset.y);
        }
    }
}
