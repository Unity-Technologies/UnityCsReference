// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for a button with an icon. It is part of a node toolbar section.
    /// </summary>
    [UnityRestricted]
    internal class NodeToolbarButton : VisualElement, INotifyValueChanged<bool>
    {
        /// <summary>
        /// The USS class name added to a <see cref="NodeToolbarButton"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-node-toolbar-icon-button";

        /// <summary>
        /// The USS class name added to the icon of the button.
        /// </summary>
        public static readonly string iconElementUssClassName = ussClassName.WithUssElement(GraphElementHelper.iconName);

        protected bool m_Value;
        Clickable m_Clickable;
        Action<bool> m_ShowCallBack;

        public VisualElement Icon { get; }

        Clickable Clickable
        {
            get => m_Clickable;
            set => this.ReplaceManipulator(ref m_Clickable, value);
        }

        /// <inheritdoc />
        public bool value
        {
            get => m_Value;
            set
            {
                if (m_Value != value)
                {
                    using (var e = ChangeEvent<bool>.GetPooled(m_Value, value))
                    {
                        e.target = this;
                        SetValueWithoutNotify(value);
                        SendEvent(e);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeToolbarButton"/> class.
        /// </summary>
        /// <param name="buttonName">The name of the button, used to identify it.</param>
        /// <param name="onClick">The action when the button is clicked.</param>
        /// <param name="changeEventCallback">A callback after the button's value is changed.</param>
        /// <param name="showCallback">A callback after the button is shown on the node. Buttons are shown on hover, else they aren't.</param>
        public NodeToolbarButton(string buttonName, Action onClick = null, EventCallback<ChangeEvent<bool>> changeEventCallback = null, Action<bool> showCallback = null)
        {
            name = buttonName;
            m_Value = false;

            this.AddPackageStylesheet("NodeToolbarIconButton.uss");
            AddToClassList(ussClassName);

            Icon = new VisualElement { name = GraphElementHelper.iconName };
            Icon.AddToClassList(iconElementUssClassName);
            Add(Icon);

            Clickable = new Clickable(onClick ?? OnClick);
            m_ShowCallBack = showCallback;

            if (changeEventCallback != null)
                RegisterCallback(changeEventCallback);
        }

        /// <summary>
        /// Invokes a callback after the button is shown on the node.
        /// </summary>
        /// <param name="show">Whether the button is shown on the node.</param>
        public void OnShowButton(bool show)
        {
            m_ShowCallBack?.Invoke(show);
        }

        /// <summary>
        /// The default action when the button is clicked. Is overriden by the onClick param in the constructor if not null.
        /// </summary>
        protected virtual void OnClick()
        {
            value = !m_Value;
        }

        /// <inheritdoc />
        public virtual void SetValueWithoutNotify(bool newValue)
        {
            m_Value = newValue;
        }
    }
}
