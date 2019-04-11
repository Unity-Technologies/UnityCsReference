// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
{
    public class BlackboardField : GraphElement
    {
        private VisualElement m_ContentItem;
        private Pill m_Pill;
        private TextField m_TextField;
        private Label m_TypeLabel;

        public string text
        {
            get { return m_Pill.text; }
            set { m_Pill.text = value; }
        }

        public string typeText
        {
            get { return m_TypeLabel.text; }
            set { m_TypeLabel.text = value; }
        }

        public Texture icon
        {
            get { return m_Pill.icon; }
            set { m_Pill.icon = value; }
        }

        public bool highlighted
        {
            get { return m_Pill.highlighted; }
            set { m_Pill.highlighted = value; }
        }

        public BlackboardField() : this(null, "", "") {}
        public BlackboardField(Texture icon, string text, string typeText)
        {
            var tpl = EditorGUIUtility.Load("UXML/GraphView/BlackboardField.uxml") as VisualTreeAsset;
            VisualElement mainContainer = tpl.CloneTree();
            AddStyleSheetPath(Blackboard.StyleSheetPath);
            mainContainer.AddToClassList("mainContainer");
            mainContainer.pickingMode = PickingMode.Ignore;

            m_ContentItem = mainContainer.Q("contentItem");
            Assert.IsTrue(m_ContentItem != null);

            m_Pill = mainContainer.Q<Pill>("pill");
            Assert.IsTrue(m_Pill != null);

            m_TypeLabel = mainContainer.Q<Label>("typeLabel");
            Assert.IsTrue(m_TypeLabel != null);

            m_TextField = mainContainer.Q<TextField>("textField");
            Assert.IsTrue(m_TextField != null);

            m_TextField.style.display = DisplayStyle.None;

            var textinput = m_TextField.Q(TextField.textInputUssName);
            textinput.RegisterCallback<FocusOutEvent>(e => { OnEditTextFinished(); });

            Add(mainContainer);

            RegisterCallback<MouseDownEvent>(OnMouseDownEvent);

            capabilities |= Capabilities.Selectable | Capabilities.Droppable | Capabilities.Deletable | Capabilities.Renamable;

            ClearClassList();
            AddToClassList("blackboardField");

            this.text = text;
            this.icon = icon;
            this.typeText = typeText;

            this.AddManipulator(new SelectionDropper());
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.eventTypeId == AttachToPanelEvent.TypeId())
            {
                var graphView = GetFirstAncestorOfType<GraphView>();
                if (graphView != null)
                    graphView.RestorePersitentSelectionForElement(this);
            }
        }

        private void OnEditTextFinished()
        {
            m_ContentItem.visible = true;
            m_TextField.style.display = DisplayStyle.None;

            if (text != m_TextField.text)
            {
                Blackboard blackboard = GetFirstAncestorOfType<Blackboard>();

                if (blackboard.editTextRequested != null)
                {
                    blackboard.editTextRequested(blackboard, this, m_TextField.text);
                }
                else
                {
                    text = m_TextField.text;
                }
            }
        }

        private void OnMouseDownEvent(MouseDownEvent e)
        {
            if ((e.clickCount == 2) && e.button == (int)MouseButton.LeftMouse && IsRenamable())
            {
                OpenTextEditor();
                e.PreventDefault();
            }
        }

        public void OpenTextEditor()
        {
            m_TextField.SetValueWithoutNotify(text);
            m_TextField.style.display = DisplayStyle.Flex;
            m_ContentItem.visible = false;
            m_TextField.Q(TextField.textInputUssName).Focus();
            m_TextField.SelectAll();
        }

        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Rename", (a) => OpenTextEditor(), DropdownMenuAction.AlwaysEnabled);
        }
    }
}
