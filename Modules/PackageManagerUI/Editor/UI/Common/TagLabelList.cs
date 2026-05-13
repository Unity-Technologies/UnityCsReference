// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    [UxmlElement]
    internal partial class TagLabelList : VisualElement
    {
        public TagLabelList()
        {
            m_ListNameLabel = new SelectableLabel
            {
                name = "listNameLabel",
                focusable = true
            };
            Add(m_ListNameLabel);

            m_TagsList = new VisualElement { name = "tagsList" };
            Add(m_TagsList);

            m_ShowAllButton = new Button
            {
                name = "showAllButton",
                text = "Show all"
            }.WithClassList("moreless");
            m_ShowLessButton = new Button
            {
                name = "showLessButton",
                text = "Show less"
            }.WithClassList("moreless");
            m_ShowAllButton.clickable.clicked += ShowAllClicked;
            m_ShowLessButton.clickable.clicked += ShowLessClicked;
        }

        public void Refresh(string listName, IReadOnlyCollection<string> tagNames)
        {
            m_ListNameLabel.text = listName;

            m_TagsList.Clear();
            foreach (var tagName in tagNames)
                m_TagsList.Add(new Label { text = tagName }.WithClassList(PackageBaseTagLabel.ussClassName));

            if (tagNames.Count < 5)
                return;

            m_TagsList.Add(m_ShowAllButton);
            m_TagsList.Add(m_ShowLessButton);
            ShowLessClicked();
        }

        private void ShowAllClicked()
        {
            UIUtils.SetElementDisplay(m_ShowAllButton, false);
            UIUtils.SetElementDisplay(m_ShowLessButton, true);
            RefreshTagsList(true);
        }

        private void ShowLessClicked()
        {
            UIUtils.SetElementDisplay(m_ShowAllButton, true);
            UIUtils.SetElementDisplay(m_ShowLessButton, false);
            RefreshTagsList(false);
        }

        private void RefreshTagsList(bool showMore)
        {
            var labelIndex = 0;
            foreach (var item in m_TagsList.Children())
            {
                if (item is not Label label)
                    continue;
                UIUtils.SetElementDisplay(label, showMore || labelIndex < 3);
                labelIndex++;
            }
        }

        private Label m_ListNameLabel;
        private VisualElement m_TagsList;
        private Button m_ShowAllButton;
        private Button m_ShowLessButton;
    }
}
