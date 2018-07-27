// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public partial class Group : Scope
    {
        private Label m_TitleItem;
        private TextField m_TitleEditor;
        private GroupDropArea m_DropArea;
        private bool m_EditTitleCancelled = false;
        private const int k_TitleEditorFocusDelay = 300;

        public override string title
        {
            get { return m_TitleItem.text; }
            set
            {
                if (m_TitleItem.text == value)
                    return;

                m_TitleItem.text = value;

                GraphView gv = GetFirstAncestorOfType<GraphView>();

                if (gv != null && gv.groupTitleChanged != null)
                {
                    gv.groupTitleChanged(this, value);
                }

                ScheduleUpdateGeometryFromContent();
            }
        }

        public Group()
        {
            AddStyleSheetPath("StyleSheets/GraphView/Group.uss");

            m_DropArea = new GroupDropArea();
            m_DropArea.ClearClassList();
            m_DropArea.name = "dropArea";

            var visualTree = EditorGUIUtility.Load("UXML/GraphView/GroupTitle.uxml") as VisualTreeAsset;
            VisualElement titleContainer = visualTree.CloneTree(null);

            titleContainer.name = "titleContainer";

            m_TitleItem = titleContainer.Q<Label>(name: "titleLabel");

            m_TitleEditor = titleContainer.Q(name: "titleField") as TextField;
            m_TitleEditor.visible = false;

            m_TitleEditor.RegisterCallback<FocusOutEvent>(e => { OnEditTitleFinished(); });
            m_TitleEditor.RegisterCallback<KeyDownEvent>(TitleEditorOnKeyDown);

            VisualElement contentContainerPlaceholder = this.Q(name: "contentContainerPlaceholder");
            contentContainerPlaceholder.Insert(0, m_DropArea);

            headerContainer.Add(titleContainer);

            AddToClassList("group");

            capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable;

            RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
        }

        public override bool AcceptsElement(GraphElement element, ref string reasonWhyNotAccepted)
        {
            if (element is Group)
            {
                reasonWhyNotAccepted = "Nested group is not supported yet.";
                return false;
            }
            else if (element is Scope)
            {
                reasonWhyNotAccepted = "Nested scope is not supported yet.";
                return false;
            }

            return true;
        }

        protected override void OnElementsAdded(IEnumerable<GraphElement> elements)
        {
            GraphView gv = GetFirstAncestorOfType<GraphView>();

            if (gv != null && gv.elementsAddedToGroup != null)
            {
                gv.elementsAddedToGroup(this, elements);
            }
        }

        protected override void OnElementsRemoved(IEnumerable<GraphElement> elements)
        {
            GraphView gv = GetFirstAncestorOfType<GraphView>();

            if (gv != null && gv.elementsRemovedFromGroup != null)
            {
                gv.elementsRemovedFromGroup(this, elements);
            }
        }

        private void TitleEditorOnKeyDown(KeyDownEvent e)
        {
            switch (e.keyCode)
            {
                case KeyCode.Escape:
                    m_EditTitleCancelled = true;
                    m_TitleEditor.Blur();
                    break;
                case KeyCode.Return:
                    m_TitleEditor.Blur();
                    break;
                default:
                    break;
            }
        }

        private void OnEditTitleFinished()
        {
            m_TitleItem.visible = true;
            m_TitleEditor.visible = false;

            if (!m_EditTitleCancelled)
            {
                string oldName = title;
                title = m_TitleEditor.text;
                OnGroupRenamed(oldName, title);
            }

            m_EditTitleCancelled = false;
        }

        private void OnMouseDownEvent(MouseDownEvent e)
        {
            if (e.clickCount == 2)
            {
                if (HitTest(e.localMousePosition))
                {
                    m_TitleEditor.value = title;
                    m_TitleEditor.visible = true;
                    m_TitleItem.visible = false;
                    this.schedule.Execute(GiveFocusToTitleEditor).StartingIn(k_TitleEditorFocusDelay);
                }
            }
        }

        private void GiveFocusToTitleEditor()
        {
            m_TitleEditor.SelectAll();
            m_TitleEditor.Focus();
        }

        protected virtual void OnGroupRenamed(string oldName, string newName)
        {
        }

        internal void OnStartDragging(IMouseEvent evt, IEnumerable<GraphElement> elements)
        {
            m_DropArea.OnStartDragging(evt, elements);
        }
    }
}
