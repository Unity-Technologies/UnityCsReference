// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Collaboration;
using UnityEditorInternal;
using UnityEngine;
using static UnityEditor.EditorGUI;

namespace UnityEditor
{
    internal static class D3D12DeviceFilterUI
    {
        internal static class Styles
        {
            public const uint kNumStaticItemsInFilter = 2;
            public const uint kNumDeviceIdItemsInFilter = 4;
            public const uint kNumCapabilityItemsInFilter = 3;
            public const float kHeightBetweenFields = 3.0f;
            public const float kHeightBetweenRows = 10.0f;
            public const float kComparatorFieldWidth = 170.0f;

            public static readonly GUIContent preferredGraphicsJobsMode = EditorGUIUtility.TrTextContent("Preferred Graphics Jobs Mode", "Indicates which graphics jobs mode this filter will enforce at runtime.");

            public static readonly GUIContent filterList = EditorGUIUtility.TrTextContent("Filters", "List of filters");
            public static readonly GUIContent vendor = EditorGUIUtility.TrTextContent("Vendor", "Use a regular expression to specify the vendor name of a device");
            public static readonly GUIContent deviceName = EditorGUIUtility.TrTextContent("Device Name", "Use a regular expression to specify the device name of a device");
            public static readonly GUIContent driverVersion =
                EditorGUIUtility.TrTextContent("Driver Version", "Specify the driver version for a device using the format MajorVersion.MinorVersion(optional).PatchVersion(optional).PatchMinorVersion(optional)");
            public static readonly GUIContent featureLevel =
                EditorGUIUtility.TrTextContent("D3D12 Feature Level", "Specify the D3D12 feature level for a device using the format MajorVersion.MinorVersion(optional)");
            public static readonly GUIContent graphicsMemory =
                EditorGUIUtility.TrTextContent("Graphics Memory (MB)", "Specify the amount of graphics memory in megabytes");
            public static readonly GUIContent processorCount = EditorGUIUtility.TrTextContent("Processor Count", "Specify the number of processors");
            public static readonly GUIContent deviceType = EditorGUIUtility.TrTextContent("Device Type", "Specify if the GPU is discrete or integrated");
            public static readonly GUIContent deviceIdentification = EditorGUIUtility.TrTextContent("Device Identification", "");
            public static readonly GUIContent capabilityMetrics = EditorGUIUtility.TrTextContent("Performance & Capability Metrics", "");

            // Text
            public static readonly string filterText = "filter";
            public static readonly string preferredGraphicsJobsModeText = "preferredMode";
            public static readonly string vendorText = "vendorName";
            public static readonly string deviceNameText = "deviceName";
            public static readonly string driverVersionComparatorText = "driverVersionComparator";
            public static readonly string driverVersionText = "driverVersion";
            public static readonly string featureLevelComparatorText = "featureLevelComparator";
            public static readonly string featureLevelText = "featureLevel";
            public static readonly string graphicsMemoryComparatorText = "graphicsMemoryComparator";
            public static readonly string graphicsMemoryText = "graphicsMemory";
            public static readonly string processorCountComparatorText = "processorCountComparator";
            public static readonly string processorCountText = "processorCount";
            public static readonly string deviceTypeText = "deviceType";

            public static float kElementHeighWithSpace => EditorGUIUtility.singleLineHeight + kHeightBetweenFields;
        }

        internal static class Utils
        {
            private static bool DrawRegexWithErrorCheck(string name, SerializedProperty prop, GUIContent standardContent, string text, ref Rect elementRect, StringBuilder errorBuilder)
            {
                prop.stringValue = EditorGUI.TextField(elementRect, standardContent, prop.stringValue);
                if (D3D12DeviceFilterUtils.HasErrorRegex(prop.stringValue, text, out var errorString))
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

                D3D12Comparator comparator = (D3D12Comparator)compProp.enumValueFlag;
                comparator = (D3D12Comparator)EditorGUI.EnumPopup(comparatorRect, comparator);
                compProp.intValue = (int)comparator;

                remainingRect.x += Styles.kComparatorFieldWidth;
                remainingRect.width -= Styles.kComparatorFieldWidth;

                valueProp.stringValue = EditorGUI.TextField(remainingRect, valueProp.stringValue);
                if (D3D12DeviceFilterUtils.HasErrorRegex(valueProp.stringValue, text, out var errorString))
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

                D3D12Comparator comparator = (D3D12Comparator)compProp.enumValueFlag;
                comparator = (D3D12Comparator)EditorGUI.EnumPopup(comparatorRect, comparator);
                compProp.intValue = (int)comparator;

                remainingRect.x += Styles.kComparatorFieldWidth;
                remainingRect.width -= Styles.kComparatorFieldWidth;

                valueProp.stringValue = EditorGUI.TextField(remainingRect, valueProp.stringValue);

                if (D3D12DeviceFilterUtils.HasErrorVersion(valueProp.stringValue, text, out var errorString))
                {
                    errorBuilder.Append($"\n{name}: {errorString}");
                    return false;
                }
                return true;
            }

            public static float CalculateElementHeight(uint numStaticItems, bool deviceIdFoldout, bool capabilityFoldout)
            {
                // Calculate the height of a single element in the filter list
                uint numFields = numStaticItems + (deviceIdFoldout ? 1u : 0u) * Styles.kNumDeviceIdItemsInFilter + (capabilityFoldout ? 1u : 0u) * Styles.kNumCapabilityItemsInFilter;
                return D3D12DeviceFilterUI.Styles.kElementHeighWithSpace * numFields + D3D12DeviceFilterUI.Styles.kHeightBetweenFields;
            }

            public static void DrawDeviceFilterList(SerializedProperty filterListProp, int index, ref Rect elementRect, ref List<bool> deviceIdFoldouts, ref List<bool> capabilityMetricsFoldouts, bool hasPreviousPropFields = false)
            {
                var vendorProp = filterListProp.FindPropertyRelative(Styles.vendorText);
                var deviceNameProp = filterListProp.FindPropertyRelative(Styles.deviceNameText);
                var driverVersionComparatorProp = filterListProp.FindPropertyRelative(Styles.driverVersionComparatorText);
                var driverVersionProp = filterListProp.FindPropertyRelative(Styles.driverVersionText);
                var featureLevelComparatorProp = filterListProp.FindPropertyRelative(Styles.featureLevelComparatorText);
                var featureLevelProp = filterListProp.FindPropertyRelative(Styles.featureLevelText);
                var graphicsMemoryComparatorProp = filterListProp.FindPropertyRelative(Styles.graphicsMemoryComparatorText);
                var graphicsMemoryProp = filterListProp.FindPropertyRelative(Styles.graphicsMemoryText);
                var processorCountComparatorProp = filterListProp.FindPropertyRelative(Styles.processorCountComparatorText);
                var processorCountProp = filterListProp.FindPropertyRelative(Styles.processorCountText);
                var deviceTypeProp = filterListProp.FindPropertyRelative(Styles.deviceTypeText);
                D3D12GraphicsDeviceType type = (D3D12GraphicsDeviceType)deviceTypeProp.enumValueFlag;

                if (hasPreviousPropFields)
                    elementRect.y += Styles.kElementHeighWithSpace;

                var errorBuilder = new StringBuilder($"Errors Detected at index {index}:");

                bool result = true;

                deviceIdFoldouts[index] = EditorGUI.Foldout(elementRect, deviceIdFoldouts[index], Styles.deviceIdentification);
                elementRect.y += Styles.kElementHeighWithSpace;

                if (deviceIdFoldouts[index])
                {
                    result &= DrawRegexWithErrorCheck("Device Name", deviceNameProp, Styles.deviceName, Styles.deviceNameText, ref elementRect, errorBuilder);
                    elementRect.y += Styles.kElementHeighWithSpace;

                    type = (D3D12GraphicsDeviceType)EditorGUI.EnumPopup(elementRect, Styles.deviceType, type);
                    deviceTypeProp.intValue = (int)type;
                    elementRect.y += Styles.kElementHeighWithSpace;

                    result &= DrawPopupAndVersionWithErrorCheck("Driver Version", driverVersionComparatorProp, driverVersionProp, Styles.driverVersion, Styles.driverVersionText, ref elementRect, errorBuilder);
                    elementRect.y += Styles.kElementHeighWithSpace;

                    result &= DrawRegexWithErrorCheck("Vendor", vendorProp, Styles.vendor, Styles.vendorText, ref elementRect, errorBuilder);
                    elementRect.y += Styles.kElementHeighWithSpace;

                }

                capabilityMetricsFoldouts[index] = EditorGUI.Foldout(elementRect, capabilityMetricsFoldouts[index], Styles.capabilityMetrics);
                elementRect.y += Styles.kElementHeighWithSpace;

                if (capabilityMetricsFoldouts[index])
                {
                    result &= DrawPopupAndRegexWithErrorCheck("Graphics Memory", graphicsMemoryComparatorProp, graphicsMemoryProp, Styles.graphicsMemory, Styles.graphicsMemoryText, ref elementRect, errorBuilder);
                    elementRect.y += Styles.kElementHeighWithSpace;

                    result &= DrawPopupAndRegexWithErrorCheck("Processor Count", processorCountComparatorProp, processorCountProp, Styles.processorCount, Styles.processorCountText, ref elementRect, errorBuilder);
                    elementRect.y += Styles.kElementHeighWithSpace;

                    result &= DrawPopupAndVersionWithErrorCheck("Feature Level", featureLevelComparatorProp, featureLevelProp, Styles.featureLevel, Styles.featureLevelText, ref elementRect, errorBuilder);
                    elementRect.y += Styles.kElementHeighWithSpace;
                }

                if (!result)
                    GUILayout.Label(EditorGUIUtility.TempContent(errorBuilder.ToString(), EditorGUIUtility.GetHelpIcon(MessageType.Error)), EditorStyles.helpBox);
            }

            public static void ClearNewElement(SerializedProperty filterListProp, int defaultComparator)
            {
                filterListProp.FindPropertyRelative(Styles.vendorText).stringValue = null;
                filterListProp.FindPropertyRelative(Styles.deviceNameText).stringValue = null;
                filterListProp.FindPropertyRelative(Styles.driverVersionComparatorText).enumValueFlag = defaultComparator;
                filterListProp.FindPropertyRelative(Styles.driverVersionText).stringValue = null;
                filterListProp.FindPropertyRelative(Styles.featureLevelComparatorText).enumValueFlag = defaultComparator;
                filterListProp.FindPropertyRelative(Styles.featureLevelText).stringValue = null;
                filterListProp.FindPropertyRelative(Styles.graphicsMemoryComparatorText).enumValueFlag = defaultComparator;
                filterListProp.FindPropertyRelative(Styles.graphicsMemoryText).stringValue = null;
                filterListProp.FindPropertyRelative(Styles.processorCountComparatorText).enumValueFlag = defaultComparator;
                filterListProp.FindPropertyRelative(Styles.processorCountText).stringValue = null;
                filterListProp.FindPropertyRelative(Styles.deviceTypeText).enumValueFlag = 0x0;
            }
        }
    }

    [CustomEditor(typeof(UnityEngine.D3D12DeviceFilterLists))]
    internal class D3D12DeviceFilterListsEditor : Editor
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
                ElementCallbackDelegate drawElementCallback, AddCallbackDelegate onAddCallback, ReorderCallbackDelegate onReorderCallback, ElementHeightCallbackDelegate onGetElementHeight,
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

        D3D12DeviceFilterLists assetObject => serializedObject.targetObject as D3D12DeviceFilterLists;

        ReorderableFilterList m_AllowReorderableFilterList;
        ReorderableFilterList m_DenyReorderableFilterList;
        ReorderableFilterList m_GfxJobsReorderableFilterList;

        bool m_ShowAllow = false;
        bool m_ShowDeny = false;
        bool m_ShowGfxJobs = false;

        List<bool> m_AllowDeviceIdFoldouts = new List<bool>();
        List<bool> m_DenyDeviceIdFoldouts = new List<bool>();
        List<bool> m_GfxJobsDeviceIdFoldouts = new List<bool>();

        List<bool> m_AllowCapabilityMetricsFoldouts = new List<bool>();
        List<bool> m_DenyCapabilityMetricsFoldouts = new List<bool>();
        List<bool> m_GfxJobsCapabilityMetricsFoldouts = new List<bool>();

        private void DrawDeviceFilterListElement(ReorderableList list, Rect rect, int index, bool isActive, bool isFocused)
        {
            var filterListProp = list.serializedProperty.GetArrayElementAtIndex(index);

            var elementRect = new Rect(rect);
            elementRect.height = EditorGUIUtility.singleLineHeight;
            elementRect.y += D3D12DeviceFilterUI.Styles.kHeightBetweenFields;
            elementRect.x += EditorGUI.kIndentPerLevel; // Indent for the foldout
            elementRect.width -= EditorGUI.kIndentPerLevel;

            if (list.serializedProperty.name == "m_AllowFilterList")
            {
                //filterListProp.
                D3D12DeviceFilterUI.Utils.DrawDeviceFilterList(filterListProp, index, ref elementRect, ref m_AllowDeviceIdFoldouts, ref m_AllowCapabilityMetricsFoldouts);
            }
            else if (list.serializedProperty.name == "m_DenyFilterList")
                D3D12DeviceFilterUI.Utils.DrawDeviceFilterList(filterListProp, index, ref elementRect, ref m_DenyDeviceIdFoldouts, ref m_DenyCapabilityMetricsFoldouts);
        }

        private void AddDeviceFilterListElement(ReorderableList filterList, D3D12Comparator defaultComparator, ref List<bool> deviceIdFoldouts, ref List<bool> capabilityMetricsFoldout)
        {
            var list = filterList.serializedProperty;
            var index = list.arraySize++;
            filterList.index = index;
            var filterListProp = list.GetArrayElementAtIndex(index);

            // For new items, expand the foldouts by default
            deviceIdFoldouts.Add(true);
            capabilityMetricsFoldout.Add(true);

            // Helper for clearing the regex strings
            D3D12DeviceFilterUI.Utils.ClearNewElement(filterListProp, (int)defaultComparator);
        }

        private void AddAllowFilterListElement(ReorderableList filterList)
        {
            AddDeviceFilterListElement(filterList, D3D12Comparator.GreaterThanOrEqualTo, ref m_AllowDeviceIdFoldouts, ref m_AllowCapabilityMetricsFoldouts);
        }

        private void AddDenyFilterListElement(ReorderableList filterList)
        {
            AddDeviceFilterListElement(filterList, D3D12Comparator.LessThanOrEqualTo, ref m_DenyDeviceIdFoldouts, ref m_DenyCapabilityMetricsFoldouts);
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
        private void ReorderGfxJobsFilterList(ReorderableList filterList, int oldIndex, int newIndex)
        {
            Swap(ref m_GfxJobsDeviceIdFoldouts, oldIndex, newIndex);
            Swap(ref m_GfxJobsCapabilityMetricsFoldouts, oldIndex, newIndex);
        }

        private void GetAllowElementHeight(ReorderableList list, int index, out float height)
        {
            height = D3D12DeviceFilterUI.Utils.CalculateElementHeight(D3D12DeviceFilterUI.Styles.kNumStaticItemsInFilter, m_AllowDeviceIdFoldouts[index], m_AllowCapabilityMetricsFoldouts[index]);
        }

        private void GetDenyElementHeight(ReorderableList list, int index, out float height)
        {
            height = D3D12DeviceFilterUI.Utils.CalculateElementHeight(D3D12DeviceFilterUI.Styles.kNumStaticItemsInFilter, m_DenyDeviceIdFoldouts[index], m_DenyCapabilityMetricsFoldouts[index]);
        }

        private void GetGfxJobsElementHeight(ReorderableList list, int index, out float height)
        {
            height = D3D12DeviceFilterUI.Utils.CalculateElementHeight(D3D12DeviceFilterUI.Styles.kNumStaticItemsInFilter + 1, m_GfxJobsDeviceIdFoldouts[index], m_GfxJobsCapabilityMetricsFoldouts[index]);
        }

        private void DrawGfxJobsFilterListElement(ReorderableList list, Rect rect, int index, bool isActive, bool isFocused)
        {
            var filterListProp = list.serializedProperty.GetArrayElementAtIndex(index);

            var elementRect = new Rect(rect);
            elementRect.height = EditorGUIUtility.singleLineHeight;
            elementRect.y += D3D12DeviceFilterUI.Styles.kHeightBetweenFields;

            var graphicsJobsPreferenceProp = filterListProp.FindPropertyRelative(D3D12DeviceFilterUI.Styles.preferredGraphicsJobsModeText);
            GraphicsJobsFilterMode mode = (GraphicsJobsFilterMode)graphicsJobsPreferenceProp.enumValueFlag;

            mode = (GraphicsJobsFilterMode)EditorGUI.EnumPopup(
                elementRect, D3D12DeviceFilterUI.Styles.preferredGraphicsJobsMode, mode);
            graphicsJobsPreferenceProp.intValue = (int)mode;

            var filterProp = filterListProp.FindPropertyRelative(D3D12DeviceFilterUI.Styles.filterText);

            // Helper for drawing the actual filter list
            D3D12DeviceFilterUI.Utils.DrawDeviceFilterList(filterProp, index, ref elementRect, ref m_GfxJobsDeviceIdFoldouts, ref m_GfxJobsCapabilityMetricsFoldouts, true);
        }

        private void AddGfxJobsFilterListElement(ReorderableList filterList)
        {
            var list = filterList.serializedProperty;
            var index = list.arraySize++;
            filterList.index = index;
            var filterListProp = list.GetArrayElementAtIndex(index);

            filterListProp.FindPropertyRelative(D3D12DeviceFilterUI.Styles.preferredGraphicsJobsModeText).enumValueFlag = 0x0;

            m_GfxJobsDeviceIdFoldouts.Add(true);
            m_GfxJobsCapabilityMetricsFoldouts.Add(true);

            // Helper for clearing the regex strings
            D3D12DeviceFilterUI.Utils.ClearNewElement(filterListProp.FindPropertyRelative(D3D12DeviceFilterUI.Styles.filterText), (int)D3D12Comparator.LessThanOrEqualTo);
        }

        public void OnEnable()
        {
            var allowDenyListElementHeight = D3D12DeviceFilterUI.Utils.CalculateElementHeight(D3D12DeviceFilterUI.Styles.kNumStaticItemsInFilter, false, false);
            m_AllowReorderableFilterList = new ReorderableFilterList(
                serializedObject, "m_AllowFilterList", DrawDeviceFilterListElement, AddAllowFilterListElement, ReorderAllowFilterList, GetAllowElementHeight, allowDenyListElementHeight);
            m_DenyReorderableFilterList = new ReorderableFilterList(
                serializedObject, "m_DenyFilterList", DrawDeviceFilterListElement, AddDenyFilterListElement, ReorderDenyFilterList, GetDenyElementHeight, allowDenyListElementHeight);

            var gfxJobsElementHeight = D3D12DeviceFilterUI.Utils.CalculateElementHeight(D3D12DeviceFilterUI.Styles.kNumStaticItemsInFilter + 1, false, false);
            m_GfxJobsReorderableFilterList = new ReorderableFilterList(
                serializedObject, "m_GraphicsJobsFilterList", DrawGfxJobsFilterListElement, AddGfxJobsFilterListElement, ReorderGfxJobsFilterList, GetGfxJobsElementHeight, gfxJobsElementHeight);

            for (int i = 0; i < m_AllowReorderableFilterList.reorderableList.count; i++)
            {
                m_AllowDeviceIdFoldouts.Add(false);
                m_AllowCapabilityMetricsFoldouts.Add(false);
            }
            for (int i = 0; i < m_DenyReorderableFilterList.reorderableList.count; i++)
            {
                m_DenyDeviceIdFoldouts.Add(false);
                m_DenyCapabilityMetricsFoldouts.Add(false);
            }
            for (int i = 0; i < m_GfxJobsReorderableFilterList.reorderableList.count; i++)
            {
                m_GfxJobsDeviceIdFoldouts.Add(false);
                m_GfxJobsCapabilityMetricsFoldouts.Add(false);
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

        private float DrawGfxJobsExtraNotice(float startingHeight)
        {
            var content = EditorGUIUtility.TempContent("The order of the Graphics Jobs Filters is important. Filtering will use the first passing filter to determine Graphics Jobs Mode at runtime.", EditorGUIUtility.GetHelpIcon(MessageType.Info));

            var rect = GUILayoutUtility.GetRect(0.0f, 0.0f, GUILayout.ExpandWidth(true));
            rect.x += EditorGUI.kIndentPerLevel;
            rect.width -= EditorGUI.kIndentPerLevel;
            var height = EditorStyles.helpBox.CalcHeight(content, rect.width) + (D3D12DeviceFilterUI.Styles.kElementHeighWithSpace * 2);

            rect = GUILayoutUtility.GetRect(0.0f, height, GUILayout.ExpandWidth(true));
            rect.x += EditorGUI.kIndentPerLevel;
            rect.width -= EditorGUI.kIndentPerLevel;

            EditorGUI.HelpBox(rect, content);
            return height;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var targetObj = serializedObject.targetObject as D3D12DeviceFilterLists;

            if (targetObj == null)
                throw new InvalidCastException("Unable to case object to D3D12DeviceFilterLists!");

            using (var changed = new ChangeCheckScope())
            {
                var height = 0.0f;

                height = DoList(m_AllowReorderableFilterList, "Allow Filters", ref m_ShowAllow, height);
                height = DoList(m_DenyReorderableFilterList, "Deny Filters", ref m_ShowDeny, height);
                height = DoList(m_GfxJobsReorderableFilterList, "Preferred Graphics Jobs Filters", ref m_ShowGfxJobs, height, DrawGfxJobsExtraNotice);

                if (changed.changed)
                    serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
