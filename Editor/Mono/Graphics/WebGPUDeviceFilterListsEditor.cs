// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEditor.Collaboration;
using UnityEditorInternal;
using UnityEngine;
using static UnityEditor.EditorGUI;

namespace UnityEditor
{
    internal static class WebGPUDeviceFilterUI
    {
        internal static class Styles
        {
            public const uint kNumStaticItemsInFilter = 2;
            public const uint kNumDeviceIdItemsInFilter = 3;
            public const uint kNumCapabilityItemsInFilter = 4;
            public const float kHeightBetweenFields = 3.0f;
            public const float kHeightBetweenRows = 10.0f;
            public const float kComparatorFieldWidth = 170.0f;

            public static readonly GUIContent filterList = EditorGUIUtility.TrTextContent("Filters", "List of filters");
            public static readonly GUIContent vendor = EditorGUIUtility.TrTextContent("Vendor", "Use a regular expression to specify the vendor name of a device");
            public static readonly GUIContent browserName = EditorGUIUtility.TrTextContent("Browser Name", "Use a regular expression to specify the name of a browser");
            public static readonly GUIContent browserVersion =
                EditorGUIUtility.TrTextContent("Browser Version", "Specify the browser version using the format MajorVersion.MinorVersion(optional).PatchVersion(optional).PatchMinorVersion(optional)");
            public static readonly GUIContent featureLevel =
                EditorGUIUtility.TrTextContent("WebGPU Feature Level", "Specify the WebGPU feature level for a device using the format MajorVersion.MinorVersion(optional)");
            public static readonly GUIContent graphicsMemory =
                EditorGUIUtility.TrTextContent("Graphics Memory (MB)", "Specify the amount of graphics memory in megabytes");
            public static readonly GUIContent processorCount = EditorGUIUtility.TrTextContent("Processor Count", "Specify the number of processors");
            public static readonly GUIContent deviceType = EditorGUIUtility.TrTextContent("Device Type", "Specify if the GPU is discrete or integrated");
            public static readonly GUIContent browserIdentification = EditorGUIUtility.TrTextContent("Browser", "");
            public static readonly GUIContent capabilityMetrics = EditorGUIUtility.TrTextContent("Capabilities", "");
            public static readonly GUIContent features = EditorGUIUtility.TrTextContent("Required Features", "WebGPU device features required or excluded by this filter");
            public static readonly GUIContent limits = EditorGUIUtility.TrTextContent("Limits", "WebGPU device limits required or excluded by this filter");

            // Text
            public static readonly string filterText = "filter";
            public static readonly string vendorText = "vendorName";
            public static readonly string browserNameText = "browserName";
            public static readonly string browserVersionComparatorText = "browserVersionComparator";
            public static readonly string browserVersionText = "browserVersion";
            public static readonly string featureLevelComparatorText = "featureLevelComparator";
            public static readonly string featureLevelText = "featureLevel";
            public static readonly string graphicsMemoryComparatorText = "graphicsMemoryComparator";
            public static readonly string graphicsMemoryText = "graphicsMemory";
            public static readonly string processorCountComparatorText = "processorCountComparator";
            public static readonly string processorCountText = "processorCount";
            public static readonly string deviceTypeText = "deviceType";
            public static readonly string featuresText = "features";
            public static readonly string limitsText = "limits";

            public static float kElementHeighWithSpace => EditorGUIUtility.singleLineHeight + kHeightBetweenFields;
        }

        internal static class Utils
        {
            private static bool DrawRegexWithErrorCheck(string name, SerializedProperty prop, GUIContent standardContent, string text, ref Rect elementRect, StringBuilder errorBuilder)
            {
                prop.stringValue = EditorGUI.TextField(elementRect, standardContent, prop.stringValue);
                if (WebGPUDeviceFilterUtils.HasErrorRegex(prop.stringValue, text, out var errorString))
                {
                    errorBuilder.Append($"\n{name}: {errorString}");
                    return false;
                }
                return true;
            }

            private static bool DrawPopupAndRegexWithErrorCheck(string name, SerializedProperty compProp, SerializedProperty valueProp, GUIContent standardContent, string text, ref Rect elementRect, StringBuilder errorBuilder)
            {
                EditorGUI.LabelField(elementRect, standardContent);
                Rect remainingRect = elementRect;
                float labelWidth = EditorGUIUtility.labelWidth + kPrefixPaddingRight;

                remainingRect.x += labelWidth;
                remainingRect.width -= labelWidth;

                Rect comparatorRect = remainingRect;
                comparatorRect.width = Styles.kComparatorFieldWidth;

                WebGPUComparator comparator = (WebGPUComparator)compProp.enumValueFlag;
                comparator = (WebGPUComparator)EditorGUI.EnumPopup(comparatorRect, comparator);
                compProp.intValue = (int)comparator;

                remainingRect.x += Styles.kComparatorFieldWidth;
                remainingRect.width -= Styles.kComparatorFieldWidth;

                valueProp.stringValue = EditorGUI.TextField(remainingRect, valueProp.stringValue);
                if (WebGPUDeviceFilterUtils.HasErrorRegex(valueProp.stringValue, text, out var errorString))
                {
                    errorBuilder.Append($"\n{name}: {errorString}");
                    return false;
                }
                return true;
            }

            private static bool DrawPopupAndVersionWithErrorCheck(string name, SerializedProperty compProp, SerializedProperty valueProp, GUIContent standardContent, string text, ref Rect elementRect, StringBuilder errorBuilder)
            {
                EditorGUI.LabelField(elementRect, standardContent);
                Rect remainingRect = elementRect;
                float labelWidth = EditorGUIUtility.labelWidth + kPrefixPaddingRight;

                remainingRect.x += labelWidth;
                remainingRect.width -= labelWidth;

                Rect comparatorRect = remainingRect;
                comparatorRect.width = Styles.kComparatorFieldWidth;

                WebGPUComparator comparator = (WebGPUComparator)compProp.enumValueFlag;
                comparator = (WebGPUComparator)EditorGUI.EnumPopup(comparatorRect, comparator);
                compProp.intValue = (int)comparator;

                remainingRect.x += Styles.kComparatorFieldWidth;
                remainingRect.width -= Styles.kComparatorFieldWidth;

                valueProp.stringValue = EditorGUI.TextField(remainingRect, valueProp.stringValue);

                if (WebGPUDeviceFilterUtils.HasErrorVersion(valueProp.stringValue, text, out var errorString))
                {
                    errorBuilder.Append($"\n{name}: {errorString}");
                    return false;
                }
                return true;
            }

            public static float CalculateElementHeight(uint numStaticItems, bool deviceIdFoldout, bool capabilityFoldout, int featuresCount = 0, int limitsCount = 0)
            {
                // Calculate the height of a single element in the filter list
                uint numFields = numStaticItems + (deviceIdFoldout ? 1u : 0u) * Styles.kNumDeviceIdItemsInFilter + 
                (capabilityFoldout ? 1u : 0u) * (Styles.kNumCapabilityItemsInFilter + (uint)featuresCount * 1u + (uint)limitsCount * 3u);

                return WebGPUDeviceFilterUI.Styles.kElementHeighWithSpace * numFields + WebGPUDeviceFilterUI.Styles.kHeightBetweenFields;
            }

            public static void DrawDeviceFilterList(SerializedProperty filterListProp, int index, ref Rect elementRect, ref List<bool> deviceIdFoldouts, ref List<bool> capabilityMetricsFoldouts, bool hasPreviousPropFields = false)
            {
                var browserNameProp = filterListProp.FindPropertyRelative(Styles.browserNameText);
                var browserVersionComparatorProp = filterListProp.FindPropertyRelative(Styles.browserVersionComparatorText);
                var browserVersionProp = filterListProp.FindPropertyRelative(Styles.browserVersionText);
                var featureLevelComparatorProp = filterListProp.FindPropertyRelative(Styles.featureLevelComparatorText);
                var featureLevelProp = filterListProp.FindPropertyRelative(Styles.featureLevelText);
                var graphicsMemoryComparatorProp = filterListProp.FindPropertyRelative(Styles.graphicsMemoryComparatorText);
                var graphicsMemoryProp = filterListProp.FindPropertyRelative(Styles.graphicsMemoryText);
                var processorCountComparatorProp = filterListProp.FindPropertyRelative(Styles.processorCountComparatorText);
                var processorCountProp = filterListProp.FindPropertyRelative(Styles.processorCountText);
                var deviceTypeProp = filterListProp.FindPropertyRelative(Styles.deviceTypeText);
                var featuresProp = filterListProp.FindPropertyRelative(Styles.featuresText);
                var limitsProp = filterListProp.FindPropertyRelative(Styles.limitsText);
                WebGPUDeviceType type = (WebGPUDeviceType)deviceTypeProp.enumValueFlag;

                if (hasPreviousPropFields)
                    elementRect.y += Styles.kElementHeighWithSpace;

                var errorBuilder = new StringBuilder($"Errors Detected at index {index}:");

                bool result = true;

                deviceIdFoldouts[index] = EditorGUI.Foldout(elementRect, deviceIdFoldouts[index], Styles.browserIdentification);
                elementRect.y += Styles.kElementHeighWithSpace;

                if (deviceIdFoldouts[index])
                {
                    result &= DrawRegexWithErrorCheck("Browser Name", browserNameProp, Styles.browserName, Styles.browserNameText, ref elementRect, errorBuilder);
                    elementRect.y += Styles.kElementHeighWithSpace;

                    result &= DrawPopupAndVersionWithErrorCheck("Browser Version", browserVersionComparatorProp, browserVersionProp, Styles.browserVersion, Styles.browserVersionText, ref elementRect, errorBuilder);
                    elementRect.y += Styles.kElementHeighWithSpace;

                    type = (WebGPUDeviceType)EditorGUI.EnumPopup(elementRect, Styles.deviceType, type);
                    deviceTypeProp.intValue = (int)type;
                    elementRect.y += Styles.kElementHeighWithSpace;
                }

                capabilityMetricsFoldouts[index] = EditorGUI.Foldout(elementRect, capabilityMetricsFoldouts[index], Styles.capabilityMetrics);
                elementRect.y += Styles.kElementHeighWithSpace;

                if (capabilityMetricsFoldouts[index])
                {
                    // Draw Features array
                    EditorGUI.LabelField(elementRect, Styles.features);
                    elementRect.y += Styles.kElementHeighWithSpace;

                    EditorGUI.indentLevel++;
                    for (int i = 0; i < featuresProp.arraySize; i++)
                    {
                        Rect featureRect = elementRect;
                        featureRect.width -= 60; // Space for remove button

                        var featureProp = featuresProp.GetArrayElementAtIndex(i);
                        WebGPUDeviceFeature feature = (WebGPUDeviceFeature)featureProp.intValue;
                        feature = (WebGPUDeviceFeature)EditorGUI.EnumPopup(featureRect, $"Feature {i}", feature);
                        featureProp.intValue = (int)feature;

                        Rect removeRect = featureRect;
                        removeRect.x += featureRect.width;
                        removeRect.width = 60;
                        if (GUI.Button(removeRect, "-"))
                        {
                            featuresProp.DeleteArrayElementAtIndex(i);
                            break;
                        }

                        elementRect.y += Styles.kElementHeighWithSpace;
                    }
                    EditorGUI.indentLevel--;

                    if (GUI.Button(elementRect, "Add Feature"))
                    {
                        featuresProp.arraySize++;
                    }
                    elementRect.y += Styles.kElementHeighWithSpace;

                    // Draw Limits array
                    EditorGUI.LabelField(elementRect, Styles.limits);
                    elementRect.y += Styles.kElementHeighWithSpace;

                    EditorGUI.indentLevel++;
                    for (int i = 0; i < limitsProp.arraySize; i++)
                    {
                        var limitProp = limitsProp.GetArrayElementAtIndex(i);
                        var limitEnumProp = limitProp.FindPropertyRelative("limit");
                        var comparatorProp = limitProp.FindPropertyRelative("comparator");
                        var valueProp = limitProp.FindPropertyRelative("value");

                        // Add visual separator between limit entries
                        if (i > 0)
                        {
                            Rect separatorRect = new Rect(elementRect.x, elementRect.y - Styles.kHeightBetweenFields / 2, elementRect.width, 1);
                            EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
                        }

                        Rect limitRect = elementRect;
                        limitRect.width -= 60; // Space for remove button

                        // Limit Type
                        WebGPUDeviceLimit limit = (WebGPUDeviceLimit)limitEnumProp.intValue;
                        limit = (WebGPUDeviceLimit)EditorGUI.EnumPopup(limitRect, $"Limit {i}", limit);
                        limitEnumProp.intValue = (int)limit;
                        elementRect.y += Styles.kElementHeighWithSpace;

                        // Comparator
                        limitRect = elementRect;
                        limitRect.width -= 60;
                        WebGPUComparator comparator = (WebGPUComparator)comparatorProp.intValue;
                        comparator = (WebGPUComparator)EditorGUI.EnumPopup(limitRect, "Comparison", comparator);
                        comparatorProp.intValue = (int)comparator;
                        elementRect.y += Styles.kElementHeighWithSpace;

                        // Value
                        limitRect = elementRect;
                        limitRect.width -= 60;
                        valueProp.ulongValue = (ulong)EditorGUI.LongField(limitRect, "Value", (long)valueProp.ulongValue);

                        // Remove button (aligned with last field)
                        Rect removeRect = limitRect;
                        removeRect.x += limitRect.width;
                        removeRect.width = 60;
                        if (GUI.Button(removeRect, "-"))
                        {
                            limitsProp.DeleteArrayElementAtIndex(i);
                            break;
                        }

                        elementRect.y += Styles.kElementHeighWithSpace;

                        // Add extra spacing between limit entries
                        if (i < limitsProp.arraySize - 1)
                        {
                            elementRect.y += Styles.kHeightBetweenFields;
                        }
                    }
                    EditorGUI.indentLevel--;

                    if (GUI.Button(elementRect, "Add Limit"))
                    {
                        limitsProp.arraySize++;
                    }
                    elementRect.y += Styles.kElementHeighWithSpace;
                }

                if (!result)
                    GUILayout.Label(EditorGUIUtility.TempContent(errorBuilder.ToString(), EditorGUIUtility.GetHelpIcon(MessageType.Error)), EditorStyles.helpBox);
            }

            public static void ClearNewElement(SerializedProperty filterListProp, int defaultComparator)
            {
                filterListProp.FindPropertyRelative(Styles.browserVersionComparatorText).enumValueFlag = defaultComparator;
            }
        }
    }

    [CustomEditor(typeof(UnityEngine.WebGPUDeviceFilterLists))]
    internal class WebGPUDeviceFilterListsEditor : Editor
    {
        internal class ReorderableFilterList
        {
            public delegate void ElementCallbackDelegate(
                ReorderableList list,
                Rect rect, int index, bool isActive, bool isFocused);
            public delegate void AddCallbackDelegate(ReorderableList list);
            public delegate void ReorderCallbackDelegate(ReorderableList list, int oldIndex, int newIndex);
            public delegate void ElementHeightCallbackDelegate(ReorderableList list, int index, out float height);

            public SerializedObject serializedObject { get; private set; }
            public SerializedProperty serializedProperty { get; private set; }
            public ReorderableList reorderableList { get; private set; }

            private ElementCallbackDelegate m_DrawElementCallback;
            private AddCallbackDelegate m_AddElementCallback;
            private ReorderCallbackDelegate m_ReorderElementsCallback;
            private ElementHeightCallbackDelegate m_ElementHeightCallback;

            public ReorderableFilterList(
                SerializedObject serializedObject, string propertyName,
                ElementCallbackDelegate drawElementCallback, AddCallbackDelegate onAddCallback,
                ReorderCallbackDelegate onReorderCallback, ElementHeightCallbackDelegate onGetElementHeight,
                float elementHeight, float headerHeight = 0.0f)
            {
                this.serializedObject = serializedObject;
                this.serializedProperty = serializedObject.FindProperty(propertyName);
                this.reorderableList = new ReorderableList(serializedObject, serializedProperty, true, false, true, true);
                m_DrawElementCallback = drawElementCallback;
                this.reorderableList.drawElementCallback = DrawListElement;
                m_AddElementCallback = onAddCallback;
                this.reorderableList.onAddCallback = AddElement;
                this.reorderableList.onReorderCallback = (_) => { };
                this.reorderableList.onReorderCallbackWithDetails = ReorderElements;
                m_ReorderElementsCallback = onReorderCallback;
                this.reorderableList.elementHeight = elementHeight;
                this.reorderableList.headerHeight = headerHeight;
                this.reorderableList.elementHeightCallback = GetElementHeight;
                m_ElementHeightCallback = onGetElementHeight;
            }

            public void DoList(Rect listRect)
            {
                reorderableList.DoList(listRect);
            }

            private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                m_DrawElementCallback(reorderableList, rect, index, isActive, isFocused);
            }

            private void AddElement(ReorderableList filterList)
            {
                m_AddElementCallback(filterList);
            }

            private void ReorderElements(ReorderableList filterList, int oldIndex, int newIndex)
            {
                m_ReorderElementsCallback(filterList, oldIndex, newIndex);
            }

            private float GetElementHeight(int index)
            {
                float height = 0.0f;
                m_ElementHeightCallback(reorderableList, index, out height);
                return height;
            }
        }

        WebGPUDeviceFilterLists assetObject => serializedObject.targetObject as WebGPUDeviceFilterLists;

        ReorderableFilterList m_AllowReorderableFilterList;
        ReorderableFilterList m_DenyReorderableFilterList;

        bool m_ShowAllow = false;
        bool m_ShowDeny = false;

        List<bool> m_AllowDeviceIdFoldouts = new List<bool>();
        List<bool> m_DenyDeviceIdFoldouts = new List<bool>();

        List<bool> m_AllowCapabilityMetricsFoldouts = new List<bool>();
        List<bool> m_DenyCapabilityMetricsFoldouts = new List<bool>();

        private void DrawDeviceFilterListElement(ReorderableList list, Rect rect, int index, bool isActive, bool isFocused)
        {
            var filterListProp = list.serializedProperty.GetArrayElementAtIndex(index);

            var elementRect = new Rect(rect);
            elementRect.height = EditorGUIUtility.singleLineHeight;
            elementRect.y += WebGPUDeviceFilterUI.Styles.kHeightBetweenFields;
            elementRect.x += EditorGUI.kIndentPerLevel; // Indent for the foldout
            elementRect.width -= EditorGUI.kIndentPerLevel;

            if (list.serializedProperty.name == "m_AllowFilterList")
                WebGPUDeviceFilterUI.Utils.DrawDeviceFilterList(filterListProp, index, ref elementRect, ref m_AllowDeviceIdFoldouts, ref m_AllowCapabilityMetricsFoldouts);
            else if (list.serializedProperty.name == "m_DenyFilterList")
                WebGPUDeviceFilterUI.Utils.DrawDeviceFilterList(filterListProp, index, ref elementRect, ref m_DenyDeviceIdFoldouts, ref m_DenyCapabilityMetricsFoldouts);
        }

        private void AddDeviceFilterListElement(ReorderableList filterList, WebGPUComparator defaultComparator, ref List<bool> deviceIdFoldouts, ref List<bool> capabilityMetricsFoldout)
        {
            UnityEngine.Debug.Log($"Adding new WebGPU Device Filter List Element: {filterList != null} {filterList.serializedProperty != null} {defaultComparator} {deviceIdFoldouts != null} {capabilityMetricsFoldout != null}");
            var list = filterList.serializedProperty;
            var index = list.arraySize++;
            filterList.index = index;
            var filterListProp = list.GetArrayElementAtIndex(index);

            // For new items, expand the foldouts by default
            deviceIdFoldouts.Add(true);
            capabilityMetricsFoldout.Add(true);

            // Helper for clearing the regex strings
            WebGPUDeviceFilterUI.Utils.ClearNewElement(filterListProp, (int)defaultComparator);
        }

        private void AddAllowFilterListElement(ReorderableList filterList)
        {
            AddDeviceFilterListElement(filterList, WebGPUComparator.GreaterThanOrEqualTo, ref m_AllowDeviceIdFoldouts, ref m_AllowCapabilityMetricsFoldouts);
        }

        private void AddDenyFilterListElement(ReorderableList filterList)
        {
            AddDeviceFilterListElement(filterList, WebGPUComparator.LessThanOrEqualTo, ref m_DenyDeviceIdFoldouts, ref m_DenyCapabilityMetricsFoldouts);
        }

        private void Swap(ref List<bool> list, int oldIndex, int newIndex)
        {
            bool tmp = list[oldIndex];
            list[oldIndex] = list[newIndex];
            list[newIndex] = tmp;
        }

        private void ReorderAllowFilterList(ReorderableList filterList, int oldIndex, int newIndex)
        {
            Swap(ref m_AllowDeviceIdFoldouts, oldIndex, newIndex);
            Swap(ref m_AllowCapabilityMetricsFoldouts, oldIndex, newIndex);
        }
        private void ReorderDenyFilterList(ReorderableList filterList, int oldIndex, int newIndex)
        {
            Swap(ref m_DenyDeviceIdFoldouts, oldIndex, newIndex);
            Swap(ref m_DenyCapabilityMetricsFoldouts, oldIndex, newIndex);
        }

        private void GetAllowElementHeight(ReorderableList list, int index, out float height)
        {
            var filterProp = list.serializedProperty.GetArrayElementAtIndex(index);
            var featuresProp = filterProp.FindPropertyRelative(WebGPUDeviceFilterUI.Styles.featuresText);
            var limitsProp = filterProp.FindPropertyRelative(WebGPUDeviceFilterUI.Styles.limitsText);
            int featuresCount = featuresProp != null ? featuresProp.arraySize : 0;
            int limitsCount = limitsProp != null ? limitsProp.arraySize : 0;
            height = WebGPUDeviceFilterUI.Utils.CalculateElementHeight(WebGPUDeviceFilterUI.Styles.kNumStaticItemsInFilter, m_AllowDeviceIdFoldouts[index], m_AllowCapabilityMetricsFoldouts[index], featuresCount, limitsCount);
        }

        private void GetDenyElementHeight(ReorderableList list, int index, out float height)
        {
            var filterProp = list.serializedProperty.GetArrayElementAtIndex(index);
            var featuresProp = filterProp.FindPropertyRelative(WebGPUDeviceFilterUI.Styles.featuresText);
            var limitsProp = filterProp.FindPropertyRelative(WebGPUDeviceFilterUI.Styles.limitsText);
            int featuresCount = featuresProp != null ? featuresProp.arraySize : 0;
            int limitsCount = limitsProp != null ? limitsProp.arraySize : 0;
            height = WebGPUDeviceFilterUI.Utils.CalculateElementHeight(WebGPUDeviceFilterUI.Styles.kNumStaticItemsInFilter, m_DenyDeviceIdFoldouts[index], m_DenyCapabilityMetricsFoldouts[index], featuresCount, limitsCount);
        }

        public void OnEnable()
        {
            var allowDenyListElementHeight = WebGPUDeviceFilterUI.Utils.CalculateElementHeight(WebGPUDeviceFilterUI.Styles.kNumStaticItemsInFilter, false, false);

            m_AllowReorderableFilterList = new ReorderableFilterList(
                serializedObject, "m_AllowFilterList", DrawDeviceFilterListElement, AddAllowFilterListElement, ReorderAllowFilterList, GetAllowElementHeight, allowDenyListElementHeight);

            m_DenyReorderableFilterList = new ReorderableFilterList(
                serializedObject, "m_DenyFilterList", DrawDeviceFilterListElement, AddDenyFilterListElement, ReorderDenyFilterList, GetDenyElementHeight, allowDenyListElementHeight);

            for (int i = 0; i < m_AllowReorderableFilterList.reorderableList.count; i++)
            {
                m_AllowDeviceIdFoldouts.Add(true);
                m_AllowCapabilityMetricsFoldouts.Add(true);
            }

            for (int i = 0; i < m_DenyReorderableFilterList.reorderableList.count; i++)
            {
                m_DenyDeviceIdFoldouts.Add(true);
                m_DenyCapabilityMetricsFoldouts.Add(true);
            }
        }

        struct ErrorInfo
        {
            public bool hasErrors;
            public string errorString;

            ErrorInfo(bool hasErrors, string errorString)
            {
                this.hasErrors = hasErrors;
                this.errorString = errorString;
            }
        };

        private float DoListInternal(ReorderableFilterList list, float startingHeight)
        {
            var listRect = GUILayoutUtility.GetRect(startingHeight, list.reorderableList.GetHeight(), GUILayout.ExpandWidth(true));
            listRect.x += EditorGUI.kIndentPerLevel;
            listRect.width -= EditorGUI.kIndentPerLevel;
            list.DoList(listRect);
            return list.reorderableList.GetHeight();
        }

        private float DoList(ReorderableFilterList list, string name, ref bool showPosition, float startingHeight, Func<float, float> onBeforeListDraw = null, Func<float, float> onAfterListDraw = null)
        {
            var height = startingHeight;
            showPosition = EditorGUILayout.BeginFoldoutHeaderGroup(showPosition, name);
            if (showPosition)
            {
                using (var scopedHeight = new IndentLevelScope())
                {
                    height += onBeforeListDraw?.Invoke(height) ?? 0.0f;
                    height += DoListInternal(list, height);
                    height += onAfterListDraw?.Invoke(height) ?? 0.0f;
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            return height;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var targetObj = serializedObject.targetObject as WebGPUDeviceFilterLists;

            if (targetObj == null)
                throw new InvalidCastException("Unable to case object to WebGPUDeviceFilterLists!");

            using (var changed = new ChangeCheckScope())
            {
                var height = 0.0f;

                height = DoList(m_AllowReorderableFilterList, "Allow Filters", ref m_ShowAllow, height);
                height = DoList(m_DenyReorderableFilterList, "Deny Filters", ref m_ShowDeny, height);

                if (changed.changed)
                    serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
