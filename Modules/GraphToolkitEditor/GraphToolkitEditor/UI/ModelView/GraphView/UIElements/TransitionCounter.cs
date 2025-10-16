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
                    // We need to wait for the layout to be updated before computing the position, because it uses resolvedStyle.
                    schedule.Execute(() =>
                    {
                        style.position = Position.Absolute;
                        UpdateLayout();
                    }).ExecuteLater(0);
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

        public void UpdateLayout()
        {
            if (style.position == Position.Absolute)
            {
                // Compute position of counter element
                var offset = new Vector2(-resolvedStyle.height * 0.5f, 0);
                var tr = (new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, 0));
                if (parent is TransitionArrow arrow)
                    tr = arrow.GetContentTransform();

                var position = MathUtils.Multiply2X3(tr, new Vector3(offset.x, offset.y, 1));

                style.left = position.x - resolvedStyle.width * 0.5f;
                style.top = position.y - resolvedStyle.height * 0.5f;
            }
        }
    }
}
