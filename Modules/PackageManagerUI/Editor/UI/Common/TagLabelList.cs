// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class TagLabelList : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<TagLabelList> {}

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
                classList = { "link", "moreless" },
                text = "Show all"
            };
            m_ShowLessButton = new Button
            {
                name = "showLessButton",
                classList = { "link", "moreless" },
                text = "Show less"
            };
            m_ShowAllButton.clickable.clicked += ShowAllClicked;
            m_ShowLessButton.clickable.clicked += ShowLessClicked;
        }

        public void Refresh(string listName, IEnumerable<string> tagNames)
        {
            m_ListNameLabel.text = listName;

            m_TagsList.Clear();
            foreach (var tagName in tagNames)
                m_TagsList.Add(new PackageTagLabel { text = tagName });

            if (tagNames.Count() < 5)
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
            var tagLabelEnumerable = m_TagsList.Children().OfType<PackageTagLabel>();
            foreach (var (tagLabel, i) in tagLabelEnumerable.Select((tagLabel, i) => ( tagLabel, i )))
            {
                // We skip the first 3 because we want them to be always visible.
                if (i < 3)
                    continue;

                UIUtils.SetElementDisplay(tagLabel, showMore);
            }
        }

        private Label m_ListNameLabel;
        private VisualElement m_TagsList;
        private Button m_ShowAllButton;
        private Button m_ShowLessButton;
    }
}
