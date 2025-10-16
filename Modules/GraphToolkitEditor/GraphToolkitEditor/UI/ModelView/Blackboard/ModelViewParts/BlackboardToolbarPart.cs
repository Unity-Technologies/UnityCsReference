// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A part to build the toolbar of the blackboard.
    /// </summary>
    [UnityRestricted]
    internal class BlackboardToolbarPart : BaseModelViewPart
    {
        /// <summary>
        /// The USS class of this part.
        /// </summary>
        public static readonly string ussClassName = "ge-blackboard-toolbar-part";

        /// <summary>
        /// The USS class of the toolbar.
        /// </summary>
        public static readonly string toolbarUssClassName = ussClassName.WithUssElement("toolbar");

        /// <summary>
        /// The USS class of the add button.
        /// </summary>
        public static readonly string addButtonUssClassname = ussClassName.WithUssElement("add-button");

        /// <summary>
        /// The USS class of the menu button.
        /// </summary>
        public static readonly string menuButtonUssClassname = ussClassName.WithUssElement("menu-button");

        /// <summary>
        /// Creates an instance of the <see cref="BlackboardToolbarPart"/>
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        public BlackboardToolbarPart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        /// <summary>
        /// THe root element.
        /// </summary>
        protected VisualElement m_Root;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildUI(VisualElement parent)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);

            void DisableContextualMenu(Button button)
            {
                button.RegisterCallback<MouseUpEvent>(e =>
                {
                    if (e.button == (int)MouseButton.RightMouse)
                        e.StopImmediatePropagation();
                });
                button.RegisterCallback<MouseDownEvent>(e =>
                {
                    if (e.button == (int)MouseButton.RightMouse)
                        e.StopImmediatePropagation();
                });
            }

            var blackboard = (Blackboard)m_OwnerElement;

            var toolBar = new VisualElement();
            toolBar.AddToClassList(toolbarUssClassName);

            var button = new Button();
            button.AddToClassList(addButtonUssClassname);
            toolBar.Add(button);
            DisableContextualMenu(button);

            var blackboardContentModel = (BlackboardContentModel)m_Model;
            if (blackboardContentModel.HasDefaultButton())
            {
                button.clickable.clicked += blackboard.CreateVariable;
                var icon = EditorGUIUtility.FindTexture("icon dropdown");
                button = new Button(Background.FromTexture2D(icon));
                button.AddToClassList(menuButtonUssClassname);
                toolBar.Add(button);
                DisableContextualMenu(button);
            }
            button.clickable.clicked += () => { blackboard?.ShowCreateVariableLibrary(new Vector2(button.worldBound.x, button.worldBound.yMax), null); };

            m_Root.Add(toolBar);

            parent.Add(m_Root);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
        }
    }
}
