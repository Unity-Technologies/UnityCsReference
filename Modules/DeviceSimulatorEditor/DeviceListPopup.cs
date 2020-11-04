// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.DeviceSimulation
{
    internal class DeviceListPopup : PopupWindowContent
    {
        private struct IndexedDevice
        {
            public IndexedDevice(int index, DeviceInfo device)
            {
                Index = index;
                Device = device;
            }

            public int Index;
            public DeviceInfo Device;
        }

        private const int k_ItemHeight = 17;
        private const int k_SpaceWidth = 15;
        private const int k_SpaceHeight = 2;

        private readonly DeviceInfo[] m_DeviceList;
        private int m_SelectedDeviceIndex;
        private int m_HoverDeviceIndex = -1;

        private int m_MaximumVisibleDeviceCount; // Not including the search field.

        private List<IndexedDevice> m_FilteredDevices;
        private int m_SelectedDeviceIndexInFilteredList;

        private Texture2D m_CheckedTexture;

        public string m_SearchContent;
        private SearchField m_SearchField;

        private Vector2 m_ScrollPosition = Vector2.zero;

        public Action<int> OnDeviceSelected { get; set; }

        public Action<string> OnSearchInput { get; set; }

        public DeviceListPopup(DeviceInfo[] deviceList, int selectedDeviceIndex, int maximumVisibleDeviceCount, string lastSearchContent)
        {
            m_DeviceList = deviceList;
            m_SelectedDeviceIndex = selectedDeviceIndex;
            m_MaximumVisibleDeviceCount = maximumVisibleDeviceCount;

            m_CheckedTexture = (Texture2D)EditorGUIUtility.Load($"Icons/{(EditorGUIUtility.isProSkin ? "d_" : string.Empty)}FilterSelectedOnly.png");

            m_SearchContent = lastSearchContent;
            m_SearchField = new SearchField();

            FilterDevicesBySearchContent();
            CalculateScrollPosition();
        }

        private void FilterDevicesBySearchContent()
        {
            m_SelectedDeviceIndexInFilteredList = -1;
            if (string.IsNullOrEmpty(m_SearchContent))
            {
                m_FilteredDevices = m_DeviceList.Select((deviceInfo, index) => new IndexedDevice(index, deviceInfo)).ToList();
                m_SelectedDeviceIndexInFilteredList = m_SelectedDeviceIndex;
                return;
            }

            m_FilteredDevices = new List<IndexedDevice>();

            var lowercaseSearchContent = m_SearchContent.ToLower();
            for (int index = 0; index < m_DeviceList.Length; ++index)
            {
                if (!m_DeviceList[index].friendlyName.ToLower().Contains(lowercaseSearchContent))
                    continue;

                if (index == m_SelectedDeviceIndex)
                    m_SelectedDeviceIndexInFilteredList = m_FilteredDevices.Count;

                m_FilteredDevices.Add(new IndexedDevice(index, m_DeviceList[index]));
            }
        }

        private void CalculateScrollPosition()
        {
            m_ScrollPosition = Vector2.zero;
            if (m_SelectedDeviceIndexInFilteredList < 0 || m_FilteredDevices.Count <= m_MaximumVisibleDeviceCount)
                return;

            // The idea is to show the selected device as top as possible in the list.
            // But we don't need to consider the case that the device count after the selected device is less than the k_MaximumVisibleDeviceCount,
            // as ScrollView will take care of this case by itself.
            m_ScrollPosition.y = m_SelectedDeviceIndexInFilteredList * (k_ItemHeight + k_SpaceHeight);
        }

        public override Vector2 GetWindowSize()
        {
            // Add 1 for search filter.
            return new Vector2(220, (GetVisibleDeviceCount() + 1) * (k_ItemHeight + k_SpaceHeight) + 5);
        }

        private int GetVisibleDeviceCount()
        {
            var currentDeviceCount = Math.Max(m_FilteredDevices.Count, 1); // Reserve on item for the case that no matched result is returned.
            return Math.Min(currentDeviceCount, m_MaximumVisibleDeviceCount);
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(k_ItemHeight));
            EditorGUILayout.Space(k_SpaceWidth);
            HandleSearchField();
            EditorGUILayout.EndHorizontal();

            // Calculate the visible rect and total rect for ScrollView.
            var visibleRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label, GUILayout.ExpandWidth(true), GUILayout.Height(0));
            visibleRect.height = (GetVisibleDeviceCount()) * (k_ItemHeight + k_SpaceHeight);
            var totalRect = new Rect(visibleRect.x, visibleRect.y, visibleRect.width, m_FilteredDevices.Count * (k_ItemHeight + k_SpaceHeight));

            m_ScrollPosition = GUI.BeginScrollView(visibleRect, m_ScrollPosition, totalRect, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

            if (m_FilteredDevices.Count > 0)
            {
                // Compute the start index and the count of device to draw.
                int startIndex = 0;
                int drawDeviceCount = m_FilteredDevices.Count;
                if (m_FilteredDevices.Count > m_MaximumVisibleDeviceCount)
                {
                    startIndex = (int)(m_ScrollPosition.y / (totalRect.height) * m_FilteredDevices.Count);
                    // We need to draw one more device than k_MaximumVisibleDeviceCount,
                    // as there is the case that we draw a portion of the device items at top and the bottom.
                    drawDeviceCount = m_MaximumVisibleDeviceCount + 1;
                }

                OnDeviceListGUI(startIndex, drawDeviceCount, totalRect);
            }
            else
            {
                EditorGUILayout.BeginHorizontal(GUILayout.Height(k_ItemHeight));
                GUILayout.Label("", new[] { GUILayout.Width(k_SpaceWidth), GUILayout.Height(k_ItemHeight) });
                GUILayout.Label("No matched result", GUILayout.Height(k_ItemHeight));
                EditorGUILayout.EndHorizontal();
            }
            GUI.EndScrollView();
        }

        private void OnDeviceListGUI(int startIndex, int drawDeviceCount, Rect totalRect)
        {
            var currentEvent = Event.current;
            var startHeight = startIndex * (k_ItemHeight + k_SpaceHeight) + totalRect.y;

            // Only draw the visible devices, as we probably will have hundreds of devices.
            for (int index = startIndex; index < Math.Min(startIndex + drawDeviceCount, m_FilteredDevices.Count); ++index)
            {
                var device = m_FilteredDevices[index];

                var prefixRect = new Rect(totalRect.x, startHeight, k_SpaceWidth, k_ItemHeight);
                var deviceNameRect = new Rect(prefixRect.x + prefixRect.width, prefixRect.y, totalRect.width - k_SpaceWidth, k_ItemHeight);
                if (currentEvent.type == EventType.Repaint)
                {
                    if (m_HoverDeviceIndex == device.Index)
                    {
                        if (deviceNameRect.Contains(currentEvent.mousePosition))
                        {
                            var hoverRect = new Rect(prefixRect.x, prefixRect.y, deviceNameRect.x + deviceNameRect.width, prefixRect.height);
                            DrawRect(hoverRect, (EditorGUIUtility.isProSkin) ? new Color(0.32f, 0.32f, 0.32f, 1.333f) : new Color(0.6f, 0.6f, 0.6f, 1.333f));
                        }
                        else
                        {
                            // We need to reset it in case we move the mouse out of the window,
                            // as sometimes if you move too fast, the EventType.MouseMove won't be triggered.
                            m_HoverDeviceIndex = -1;
                        }
                    }

                    if (device.Index == m_SelectedDeviceIndex)
                    {
                        GUI.Label(prefixRect, m_CheckedTexture);
                    }
                    else
                    {
                        GUI.Label(prefixRect, "");
                    }
                    GUI.Label(deviceNameRect, device.Device.friendlyName);
                }
                else
                {
                    DoMouseEvent(deviceNameRect, device.Index);
                }

                startHeight += (k_ItemHeight + k_SpaceHeight);
            }
        }

        private void DoMouseEvent(Rect rect, int deviceIndex)
        {
            var currentEvent = Event.current;
            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (currentEvent.button == 0 && rect.Contains(currentEvent.mousePosition))
                    {
                        m_SelectedDeviceIndex = deviceIndex;
                        currentEvent.Use();

                        editorWindow.Close();
                        OnDeviceSelected?.Invoke(m_SelectedDeviceIndex);
                    }
                    break;
                case EventType.MouseMove:
                    if (rect.Contains(currentEvent.mousePosition))
                    {
                        if (m_HoverDeviceIndex != deviceIndex)
                        {
                            m_HoverDeviceIndex = deviceIndex;
                            editorWindow.Repaint();
                        }
                        currentEvent.Use();
                    }
                    else if (m_HoverDeviceIndex == deviceIndex)
                    {
                        m_HoverDeviceIndex = -1;
                        editorWindow.Repaint();
                    }
                    break;
            }
        }

        private static void DrawRect(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Color orgColor = GUI.color;
            GUI.color = GUI.color * color;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            GUI.color = orgColor;
        }

        private void HandleSearchField()
        {
            m_SearchField.SetFocus();
            var newSearchContent = m_SearchField.OnToolbarGUI(m_SearchContent, null);
            if (newSearchContent == m_SearchContent)
                return;

            m_SearchContent = newSearchContent;
            OnSearchInput?.Invoke(m_SearchContent);
            FilterDevicesBySearchContent();
            CalculateScrollPosition();
        }
    }
}
