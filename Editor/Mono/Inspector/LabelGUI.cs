// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class LabelGUI
    {
        HashSet<Object> m_CurrentAssetsSet;
        PopupList.InputData m_AssetLabels;
        string m_ChangedLabel;
        bool m_CurrentChanged = false;
        bool m_ChangeWasAdd = false;
        bool m_IgnoreNextAssetLabelsChangedCall = false;

        static Action<Object>  s_AssetLabelsForObjectChangedDelegates;
        private static int s_MaxShownLabels = 10;

        public void OnEnable()
        {
            s_AssetLabelsForObjectChangedDelegates += AssetLabelsChangedForObject;
            EditorApplication.projectWindowChanged += InvalidateLabels;
        }

        public void OnDisable()
        {
            s_AssetLabelsForObjectChangedDelegates -= AssetLabelsChangedForObject;
            EditorApplication.projectWindowChanged -= InvalidateLabels;
            SaveLabels();
        }

        public void OnLostFocus()
        {
            SaveLabels();
        }

        public void InvalidateLabels()
        {
            m_AssetLabels = null;
            m_CurrentAssetsSet = null;
        }

        public void AssetLabelsChangedForObject(Object asset)
        {
            if (!m_IgnoreNextAssetLabelsChangedCall && m_CurrentAssetsSet != null && m_CurrentAssetsSet.Contains(asset))
            {
                m_AssetLabels = null; // someone else changed the labels for one of our selected assets, so invalidate cache
            }
            m_IgnoreNextAssetLabelsChangedCall = false;
        }

        public void SaveLabels()
        {
            if (m_CurrentChanged && m_AssetLabels != null && m_CurrentAssetsSet != null)
            {
                bool anyLabelsWereChanged = false;
                foreach (var currentAsset in m_CurrentAssetsSet)
                {
                    bool currentAssetWasChanged = false; // when multi-editing, some assets might e.g. already have the label that was added to all
                    string[] currentLabels = AssetDatabase.GetLabels(currentAsset);
                    List<string> currentLabelList = currentLabels.ToList<string>();
                    if (m_ChangeWasAdd)
                    {
                        if (!currentLabelList.Contains(m_ChangedLabel))
                        {
                            currentLabelList.Add(m_ChangedLabel);
                            currentAssetWasChanged = true;
                        }
                    }
                    else
                    {
                        if (currentLabelList.Contains(m_ChangedLabel))
                        {
                            currentLabelList.Remove(m_ChangedLabel);
                            currentAssetWasChanged = true;
                        }
                    }
                    if (currentAssetWasChanged)
                    {
                        AssetDatabase.SetLabels(currentAsset, currentLabelList.ToArray());
                        if (s_AssetLabelsForObjectChangedDelegates != null)
                        {
                            m_IgnoreNextAssetLabelsChangedCall = true;
                            s_AssetLabelsForObjectChangedDelegates(currentAsset);
                        }
                        anyLabelsWereChanged = true;
                    }
                }
                if (anyLabelsWereChanged)
                    EditorApplication.Internal_CallAssetLabelsHaveChanged();
                m_CurrentChanged = false;
            }
        }

        public void AssetLabelListCallback(PopupList.ListElement element)
        {
            m_ChangedLabel = element.text;
            element.selected = !element.selected;
            m_ChangeWasAdd = element.selected;
            element.partiallySelected = false;
            m_CurrentChanged = true;
            SaveLabels();
            InspectorWindow.RepaintAllInspectors();
        }

        public void InitLabelCache(Object[] assets)
        {
            HashSet<Object> newAssetSet = new HashSet<Object>(assets);
            // Init only if new asset
            if (m_CurrentAssetsSet == null || !m_CurrentAssetsSet.SetEquals(newAssetSet))
            {
                List<string> all;
                List<string> partial;
                GetLabelsForAssets(assets, out all, out partial);

                m_AssetLabels = new PopupList.InputData
                {
                    m_CloseOnSelection = false,
                    m_AllowCustom = true,
                    m_OnSelectCallback = AssetLabelListCallback,
                    m_MaxCount = 15,
                    m_SortAlphabetically = true
                };

                Dictionary<string, float> allLabels = AssetDatabase.GetAllLabels();
                foreach (var pair in allLabels)
                {
                    PopupList.ListElement element = m_AssetLabels.NewOrMatchingElement(pair.Key);
                    if (element.filterScore < pair.Value)
                    {
                        element.filterScore = pair.Value;
                    }
                    element.selected = all.Any(label => string.Equals(label, pair.Key, StringComparison.OrdinalIgnoreCase));
                    element.partiallySelected = partial.Any(label => string.Equals(label, pair.Key, StringComparison.OrdinalIgnoreCase));
                }
            }

            m_CurrentAssetsSet = newAssetSet;
            m_CurrentChanged = false;
        }

        public void OnLabelGUI(Object[] assets)
        {
            InitLabelCache(assets);

            // For the label list as a whole
            // The previous layouting means we've already lost a pixel to the left and couple at the top, so it is an attempt at horizontal padding: 3, verical padding: 5
            // (the rounded sides of labels makes this look like the horizontal and vertical padding is the same)
            float leftPadding = 1.0f;
            float rightPadding = 2.0f;
            float topPadding = 3.0f;
            float bottomPadding = 5.0f;

            GUIStyle labelButton = EditorStyles.assetLabelIcon;

            float buttonWidth = labelButton.margin.left + labelButton.fixedWidth + rightPadding;

            // Assumes we are already in a vertical layout
            GUILayout.Space(topPadding);

            // Create a rect to test how wide the label list can be
            Rect widthProbeRect = GUILayoutUtility.GetRect(0, 10240, 0, 0);
            widthProbeRect.width -= buttonWidth; // reserve some width for the button

            EditorGUILayout.BeginHorizontal();

            // Left padding
            GUILayoutUtility.GetRect(leftPadding, leftPadding, 0, 0);

            // Draw labels (fully selected)
            DrawLabelList(false, widthProbeRect.xMax);

            // Draw labels (partially selected)
            DrawLabelList(true, widthProbeRect.xMax);

            GUILayout.FlexibleSpace();

            Rect r = GUILayoutUtility.GetRect(labelButton.fixedWidth, labelButton.fixedWidth, labelButton.fixedHeight + bottomPadding, labelButton.fixedHeight + bottomPadding);
            r.x = widthProbeRect.xMax + labelButton.margin.left;
            if (EditorGUI.DropdownButton(r, GUIContent.none, FocusType.Passive, labelButton))
            {
                PopupWindow.Show(r, new PopupList(m_AssetLabels), null, ShowMode.PopupMenuWithKeyboardFocus);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLabelList(bool partiallySelected, float xMax)
        {
            GUIStyle labelStyle = partiallySelected ? EditorStyles.assetLabelPartial : EditorStyles.assetLabel;
            Event evt = Event.current;
            foreach (GUIContent content in (from i in m_AssetLabels.m_ListElements where (partiallySelected ? i.partiallySelected : i.selected) orderby i.text.ToLower() select i.m_Content).Take(s_MaxShownLabels))
            {
                Rect rt = GUILayoutUtility.GetRect(content, labelStyle);
                if (Event.current.type == EventType.Repaint && rt.xMax >= xMax)
                    break;
                GUI.Label(rt, content, labelStyle);
                if (rt.xMax <= xMax && evt.type == EventType.MouseDown && rt.Contains(evt.mousePosition) && evt.button == 0 && GUI.enabled)
                {
                    evt.Use();
                    rt.x = xMax;
                    PopupWindow.Show(rt, new PopupList(m_AssetLabels, content.text), null, ShowMode.PopupMenuWithKeyboardFocus);
                }
            }
        }

        private void GetLabelsForAssets(Object[] assets, out List<string> all, out List<string> partial)
        {
            all = new List<string>();
            partial = new List<string>();

            Dictionary<string, int> labelAssetCount = new Dictionary<string, int>();
            foreach (Object asset in assets)
            {
                string[] currentLabels = AssetDatabase.GetLabels(asset);
                foreach (string label in currentLabels)
                {
                    labelAssetCount[label] = labelAssetCount.ContainsKey(label) ? labelAssetCount[label] + 1 : 1;
                }
            }

            foreach (KeyValuePair<string, int> entry in labelAssetCount)
            {
                var list = (entry.Value == assets.Length) ? all : partial;
                list.Add(entry.Key);
            }
        }
    }
}
