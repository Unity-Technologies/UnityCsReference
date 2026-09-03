// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.Animations.AnimationWindow.Widgets
{
    internal class ClipDropdownWindowContent : PopupWindowContent
    {
        const string k_UssClassName = "clip-dropdown-window";
        const string k_SearchFieldClassName = "clip-dropdown-window__search-field";
        const string k_ScrollViewClassName = "clip-dropdown-window__scroll-view";
        const string k_ItemClassName = "clip-dropdown-window__item";
        const string k_ItemLabelClassName = "clip-dropdown-window__item-label";
        const string k_ItemSpecialClassName = "clip-dropdown-window__item--special";
        const string k_ItemReadOnlyClassName = "clip-dropdown-window__item--readonly";
        const string k_ItemInvalidClassName = "clip-dropdown-window__item--invalid";
        const string k_SelectedClassName = "selected";

        static readonly string s_CreateNewClip = L10n.Tr("Create New Clip...", null);

        private ToolbarSearchField m_SearchField;
        private ScrollView m_ScrollView;
        private List<IAnimationWindowClip> m_AllClips;
        private List<IAnimationWindowClip> m_FilteredClips;
        private IAnimationWindowClip m_CurrentValue;
        private bool m_CanCreateNewClip;
        private int m_SelectedIndex = -1;

        private Action<IAnimationWindowClip> m_OnSelectionChanged;
        private Action<string> m_OnCreateNewClip;
        private float m_MaxHeight;

        public ClipDropdownWindowContent(
            List<IAnimationWindowClip> clips,
            IAnimationWindowClip currentValue,
            bool canCreateNewClip,
            float maxHeight,
            Action<IAnimationWindowClip> onSelectionChanged,
            Action<string> onCreateNewClip)
        {
            m_AllClips = new List<IAnimationWindowClip>(clips);
            m_CurrentValue = currentValue;
            m_CanCreateNewClip = canCreateNewClip;
            m_MaxHeight = maxHeight;
            m_OnSelectionChanged = onSelectionChanged;
            m_OnCreateNewClip = onCreateNewClip;

            // Sort using natural compare (same as original)
            m_AllClips.Sort((a, b) => EditorUtility.NaturalCompare(GetDisplayName(a), GetDisplayName(b)));

            m_FilteredClips = new List<IAnimationWindowClip>(m_AllClips);
            // Add "Create New Clip..." marker if enabled
            if (m_CanCreateNewClip)
            {
                m_FilteredClips.Add(null);
            }
        }

        public override VisualElement CreateGUI()
        {
            var root = new VisualElement();
            root.AddToClassList(k_UssClassName);

            // Force fixed width via inline style to prevent shrinking
            root.style.width = 250f;
            root.style.minWidth = 250f;

            // Load USS stylesheet
            var styleSheet = EditorGUIUtility.Load("StyleSheets/Animation/ClipDropdownWindowContent.uss") as StyleSheet;
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            // Create search field
            m_SearchField = new ToolbarSearchField();
            m_SearchField.AddToClassList(k_SearchFieldClassName);
            m_SearchField.RegisterValueChangedCallback(OnSearchChanged);
            root.Add(m_SearchField);

            // Create scroll view with dynamic max height
            m_ScrollView = new ScrollView();
            m_ScrollView.AddToClassList(k_ScrollViewClassName);
            m_ScrollView.style.maxHeight = m_MaxHeight;
            m_ScrollView.style.width = 250f;
            m_ScrollView.style.minWidth = 250f;
            root.Add(m_ScrollView);

            // Build initial list
            RebuildList();

            // Focus search field
            m_SearchField.schedule.Execute(() => m_SearchField.Focus());

            // Register keyboard handler
            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            return root;
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            FilterClips(evt.newValue);
        }

        private void FilterClips(string searchText)
        {
            m_FilteredClips.Clear();

            var matches = new List<(IAnimationWindowClip clip, List<int> indices)>(m_AllClips.Count);

            if (string.IsNullOrEmpty(searchText))
            {
                foreach (var clip in m_AllClips)
                    matches.Add((clip, null));
            }
            else
            {
                var scored = new List<(IAnimationWindowClip clip, long score, List<int> indices)>();
                foreach (var clip in m_AllClips)
                {
                    long score = 0;
                    var indices = ListPool<int>.Get();
                    if (FuzzySearch.FuzzyMatch(searchText, GetDisplayName(clip), ref score, indices))
                        scored.Add((clip, score, indices));
                    else
                        ListPool<int>.Release(indices);
                }
                scored.Sort((a, b) => b.score.CompareTo(a.score));
                foreach (var (clip, _, indices) in scored)
                    matches.Add((clip, indices));
            }

            // Always add "Create New Clip..." if enabled (represented as null)
            if (m_CanCreateNewClip)
                matches.Add((null, null));

            foreach (var (clip, _) in matches)
                m_FilteredClips.Add(clip);

            RebuildList(matches);

            foreach (var (_, indices) in matches)
            {
                if (indices != null)
                    ListPool<int>.Release(indices);
            }

            // Auto-select current value or first item
            int newIndex = m_FilteredClips.FindIndex(c => c != null && c.Equals(m_CurrentValue));
            if (newIndex >= 0)
            {
                SetSelection(newIndex);
            }
            else if (m_FilteredClips.Count > 0)
            {
                SetSelection(0);
            }
            else
            {
                SetSelection(-1);
            }
        }

        private void RebuildList(List<(IAnimationWindowClip clip, List<int> indices)> matches = null)
        {
            m_ScrollView.Clear();

            for (int i = 0; i < m_FilteredClips.Count; i++)
            {
                var indices = matches != null ? matches[i].indices : null;
                var item = CreateListItem(m_FilteredClips[i], i, indices);
                m_ScrollView.Add(item);
            }
        }

        private VisualElement CreateListItem(IAnimationWindowClip clip, int index, List<int> matches)
        {
            var container = new VisualElement();
            container.AddToClassList(k_ItemClassName);

            var displayName = GetDisplayName(clip);
            var label = new Label(BuildLabelText(displayName, matches));
            label.enableRichText = matches != null;
            label.AddToClassList(k_ItemLabelClassName);
            // Text-overflow ellipsis operates on the raw string and can split rich text tags.
            // Use visual clipping instead when highlights are present.
            if (matches != null)
                label.style.textOverflow = TextOverflow.Clip;
            container.Add(label);

            // Add special class for "Create New Clip..."
            if (clip == null && m_CanCreateNewClip)
            {
                container.AddToClassList(k_ItemSpecialClassName);
            }
            // Add class for read-only clips
            else if (clip != null && clip.isReadOnly)
            {
                container.AddToClassList(k_ItemReadOnlyClassName);
            }
            // Add class for invalid clips
            else if (clip != null && !clip.isValid)
            {
                container.AddToClassList(k_ItemInvalidClassName);
            }

            // Handle selection
            if (clip != null && clip.Equals(m_CurrentValue))
            {
                m_SelectedIndex = index;
                container.AddToClassList(k_SelectedClassName);
            }

            // Register click handler
            container.RegisterCallback<MouseDownEvent>(evt =>
            {
                OnItemClicked(index);
                evt.StopPropagation();
            });

            return container;
        }

        private void OnItemClicked(int index)
        {
            if (index < 0 || index >= m_FilteredClips.Count)
                return;

            var clip = m_FilteredClips[index];

            // "Create New Clip..." selected (null marker)
            if (clip == null && m_CanCreateNewClip)
            {
                m_OnCreateNewClip?.Invoke(m_SearchField.value);
                editorWindow.Close();
                return;
            }

            m_OnSelectionChanged?.Invoke(clip);
            editorWindow.Close();
        }

        private void SetSelection(int index)
        {
            var content = m_ScrollView.contentContainer;

            // Clear previous selection
            if (m_SelectedIndex >= 0 && m_SelectedIndex < content.childCount)
            {
                content.ElementAt(m_SelectedIndex).RemoveFromClassList(k_SelectedClassName);
            }

            m_SelectedIndex = index;

            // Set new selection
            if (m_SelectedIndex >= 0 && m_SelectedIndex < content.childCount)
            {
                var item = content.ElementAt(m_SelectedIndex);
                item.AddToClassList(k_SelectedClassName);

                // Scroll to selection
                m_ScrollView.ScrollTo(item);
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.UpArrow:
                    if (m_SelectedIndex > 0)
                    {
                        SetSelection(m_SelectedIndex - 1);
                        evt.StopPropagation();
                    }
                    break;

                case KeyCode.DownArrow:
                    if (m_SelectedIndex < m_FilteredClips.Count - 1)
                    {
                        SetSelection(m_SelectedIndex + 1);
                        evt.StopPropagation();
                    }
                    break;

                case KeyCode.PageUp:
                    SetSelection(Mathf.Max(0, m_SelectedIndex - 10));
                    evt.StopPropagation();
                    break;

                case KeyCode.PageDown:
                    SetSelection(Mathf.Min(m_FilteredClips.Count - 1, m_SelectedIndex + 10));
                    evt.StopPropagation();
                    break;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (m_SelectedIndex >= 0)
                    {
                        OnItemClicked(m_SelectedIndex);
                        evt.StopPropagation();
                    }
                    break;

                case KeyCode.Escape:
                    editorWindow?.Close();
                    evt.StopPropagation();
                    break;

                case KeyCode.F:
                    if (evt.actionKey)
                    {
                        m_SearchField.Focus();
                        evt.StopPropagation();
                    }
                    break;
            }
        }

        private static string BuildLabelText(string name, List<int> matches)
        {
            if (matches == null || matches.Count == 0)
                return name;

            var sb = new StringBuilder();
            int i = 0;
            while (i < name.Length)
            {
                if (matches.Contains(i))
                {
                    sb.Append("<b>");
                    while (i < name.Length && matches.Contains(i))
                        sb.Append(name[i++]);
                    sb.Append("</b>");
                }
                else
                {
                    sb.Append(name[i++]);
                }
            }
            return sb.ToString();
        }

        private string GetDisplayName(IAnimationWindowClip clip)
        {
            // Special marker for "Create New Clip..."
            if (clip == null && m_CanCreateNewClip)
                return s_CreateNewClip;

            if (clip == null || !clip.isValid)
                return L10n.Tr("[No Clip]", null);

            string name = clip.name;

            if (clip.isReadOnly)
                name = string.Format(L10n.Tr("{0} (Read-Only)", null), name);

            return name;
        }
    }
}
