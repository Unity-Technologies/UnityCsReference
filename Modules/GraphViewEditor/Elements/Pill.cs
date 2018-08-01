// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public class Pill : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<Pill, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlBoolAttributeDescription m_Highlighted = new UxmlBoolAttributeDescription { name = "highlighted" };
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((Pill)ve).highlighted = m_Highlighted.GetValueFromBag(bag, cc);
                ((Pill)ve).text = m_Text.GetValueFromBag(bag, cc);
            }
        }

        private readonly Label m_TitleLabel;
        private readonly Image m_Icon;
        private VisualElement m_Left;
        private readonly VisualElement m_LeftContainer;
        private VisualElement m_Right;
        private readonly VisualElement m_RightContainer;
        private bool m_Highlighted = false;

        public bool highlighted
        {
            get { return m_Highlighted; }
            set
            {
                if (m_Highlighted == value)
                {
                    return;
                }

                m_Highlighted = value;

                if (m_Highlighted)
                    AddToClassList("highlighted");
                else
                    RemoveFromClassList("highlighted");
            }
        }

        public string text
        {
            get { return m_TitleLabel.text; }
            set { m_TitleLabel.text = value; }
        }

        public Texture icon
        {
            get { return m_Icon != null ? m_Icon.image : null; }
            set
            {
                if (m_Icon == null || m_Icon.image == value)
                    return;

                m_Icon.image = value;

                UpdateIconVisibility();
            }
        }

        public VisualElement left
        {
            get { return m_Left; }
            set
            {
                if (m_Left == value)
                    return;

                if (m_Left != null)
                    m_LeftContainer.Remove(m_Left);

                m_Left = value;

                if (m_Left != null)
                    m_LeftContainer.Add(m_Left);

                UpdateVisibility();
            }
        }

        public VisualElement right
        {
            get { return m_Right; }
            set
            {
                if (m_Right == value)
                    return;

                if (m_Right != null)
                    m_RightContainer.Remove(m_Right);

                m_Right = value;

                if (m_Right != null)
                    m_RightContainer.Add(m_Right);

                UpdateVisibility();
            }
        }

        void UpdateIconVisibility()
        {
            if (icon == null)
            {
                RemoveFromClassList("has-icon");
                m_Icon.visible = false;
            }
            else
            {
                AddToClassList("has-icon");
                m_Icon.visible = true;
            }
        }

        void UpdateVisibility()
        {
            if (m_Left != null)
            {
                AddToClassList("has-left");
                m_LeftContainer.visible = true;
            }
            else
            {
                RemoveFromClassList("has-left");
                m_LeftContainer.visible = false;
            }

            if (m_Right != null)
            {
                AddToClassList("has-right");
                m_RightContainer.visible = true;
            }
            else
            {
                RemoveFromClassList("has-right");
                m_RightContainer.visible = false;
            }
        }

        public Pill()
        {
            AddStyleSheetPath("StyleSheets/GraphView/Pill.uss");

            var tpl = EditorGUIUtility.Load("UXML/GraphView/Pill.uxml") as VisualTreeAsset;
            VisualElement mainContainer = tpl.CloneTree(null);

            m_TitleLabel = mainContainer.Q<Label>("title-label");
            m_Icon = mainContainer.Q<Image>("icon");
            m_LeftContainer = mainContainer.Q("input");
            m_RightContainer = mainContainer.Q("output");

            Add(mainContainer);

            AddToClassList("pill");

            UpdateVisibility();
            UpdateIconVisibility();
        }

        public Pill(VisualElement left, VisualElement right) : this()
        {
            this.left = left;
            this.right = right;

            UpdateVisibility();
            UpdateIconVisibility();
        }
    }
}
