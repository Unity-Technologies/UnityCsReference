// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEditorInternal;
using UnityEngine;
using static UnityEditor.EditorGUI;

namespace UnityEditor
{
    internal static class DeviceFilterUI
    {
        internal static class Styles
        {
            public const uint kNumItemsInFilter = 7;
            public const float kHeightBetweenFields = 3.0f;
            public const float kHeightBetweenRows = 10.0f;

            public static readonly GUIContent conversionButton = EditorGUIUtility.TrTextContent("Import Legacy Player Settings Filter Lists", "Imports the 'Android Vulkan Deny Filter List' and 'Android Vulkan Allow Filter List' Player Settings fields to a standalone Vulkan Device Filtering Asset. Once imported, this button and the Android Vulkan Deny/Allow Filter List fields will be disabled.");

            public static readonly GUIContent preferredGraphicsJobsMode = EditorGUIUtility.TrTextContent("Preferred Graphics Jobs Mode", "Indicates which graphics jobs mode this filter will enforce at runtime.");

            public static readonly GUIContent filterList = EditorGUIUtility.TrTextContent("Filters", "List of filters");
            public static readonly GUIContent vendor = EditorGUIUtility.TrTextContent("Vendor", "Use a regular expression to specify the vendor name of a device");
            public static readonly GUIContent deviceName = EditorGUIUtility.TrTextContent("Device Name", "Use a regular expression to specify the device name of a device");
            public static readonly GUIContent brand = EditorGUIUtility.TrTextContent("Brand", "Use a regular expression to specify the device brand of a device");
            public static readonly GUIContent product = EditorGUIUtility.TrTextContent("Product Name", "Use a regular expression to specify the product name of a device");
            public static readonly GUIContent osVersion =
                EditorGUIUtility.TrTextContent("Android OS Version", "Use a regular expression to specify the OS version of a device");
            public static readonly GUIContent vulkanApiVersion =
                EditorGUIUtility.TrTextContent("Vulkan API Version", "Specify the Vulkan API version for a device using the format MajorVersion.MinorVersion(optional).PatchVersion(optional)");
            public static readonly GUIContent driverVersion =
                EditorGUIUtility.TrTextContent("Driver Version", "Specify the driver version for a device using the format MajorVersion.MinorVersion(optional).PatchVersion(optional)");

            // Text
            public static readonly string filterText = "filter";
            public static readonly string preferredGraphicsJobsModeText = "preferredMode";
            public static readonly string vendorText = "vendorName";
            public static readonly string deviceNameText = "deviceName";
            public static readonly string brandText = "brandName";
            public static readonly string productText = "productName";
            public static readonly string androidOsVersionText = "androidOsVersionString";
            public static readonly string vulkanApiVersionText = "vulkanApiVersionString";
            public static readonly string driverVersionText = "driverVersionString";

            public static float kElementHeighWithSpace => EditorGUIUtility.singleLineHeight + kHeightBetweenFields;
        }

        internal static class Utils
        {
            private static bool DrawRegexWithErrorCheck(string name, SerializedProperty prop, GUIContent standardContent, string text, ref Rect elementRect, StringBuilder errorBuilder)
            {
                prop.stringValue = EditorGUI.TextField(elementRect, standardContent, prop.stringValue);
                if (VulkanDeviceFilterUtils.HasErrorRegex(prop.stringValue, text, out var errorString))
                {
                    errorBuilder.Append($"\n{name}: {errorString}");
                    return false;
                }
                return true;
            }

            private static bool DrawVersionWithErrorCheck(string name, SerializedProperty prop, GUIContent standardContent, string text, ref Rect elementRect, StringBuilder errorBuilder)
            {
                prop.stringValue = EditorGUI.TextField(elementRect, standardContent, prop.stringValue);
                if (VulkanDeviceFilterUtils.HasErrorVersion(prop.stringValue, text, out var errorString))
                {
                    errorBuilder.Append($"\n{name}: {errorString}");
                    return false;
                }
                return true;
            }

            public static void DrawDeviceFilterList(SerializedProperty filterListProp, int index, ref Rect elementRect, bool hasPreviousPropFields = false)
            {
                var vendorProp = filterListProp.FindPropertyRelative(Styles.vendorText);
                var deviceNameProp = filterListProp.FindPropertyRelative(Styles.deviceNameText);
                var brandProp = filterListProp.FindPropertyRelative(Styles.brandText);
                var productProp = filterListProp.FindPropertyRelative(Styles.productText);
                var osVersionProp = filterListProp.FindPropertyRelative(Styles.androidOsVersionText);
                var vulkanApiVersionProp = filterListProp.FindPropertyRelative(Styles.vulkanApiVersionText);
                var driverVersionProp = filterListProp.FindPropertyRelative(Styles.driverVersionText);

                if (hasPreviousPropFields)
                    elementRect.y += DeviceFilterUI.Styles.kElementHeighWithSpace;

                var errorBuilder = new StringBuilder($"Errors Detected at index {index}:");

                var result = DrawRegexWithErrorCheck("Vendor", vendorProp, DeviceFilterUI.Styles.vendor, DeviceFilterUI.Styles.vendorText, ref elementRect, errorBuilder);
                elementRect.y += DeviceFilterUI.Styles.kElementHeighWithSpace;

                result &= DrawRegexWithErrorCheck("Device Name", deviceNameProp, DeviceFilterUI.Styles.deviceName, DeviceFilterUI.Styles.deviceNameText, ref elementRect, errorBuilder);
                elementRect.y += DeviceFilterUI.Styles.kElementHeighWithSpace;

                result &= DrawRegexWithErrorCheck("Brand", brandProp, DeviceFilterUI.Styles.brand, DeviceFilterUI.Styles.brandText, ref elementRect, errorBuilder);
                elementRect.y += DeviceFilterUI.Styles.kElementHeighWithSpace;

                result &= DrawRegexWithErrorCheck("Product Name", productProp, DeviceFilterUI.Styles.product, DeviceFilterUI.Styles.productText, ref elementRect, errorBuilder);
                elementRect.y += DeviceFilterUI.Styles.kElementHeighWithSpace;

                result &= DrawRegexWithErrorCheck("Android OS Version", osVersionProp, DeviceFilterUI.Styles.osVersion, DeviceFilterUI.Styles.androidOsVersionText, ref elementRect, errorBuilder);
                elementRect.y += DeviceFilterUI.Styles.kElementHeighWithSpace;

                result &= DrawVersionWithErrorCheck("Vulkan API Version", vulkanApiVersionProp, DeviceFilterUI.Styles.vulkanApiVersion, DeviceFilterUI.Styles.vulkanApiVersionText, ref elementRect, errorBuilder);
                elementRect.y += DeviceFilterUI.Styles.kElementHeighWithSpace;

                result &= DrawVersionWithErrorCheck("Driver Version", driverVersionProp, DeviceFilterUI.Styles.driverVersion, DeviceFilterUI.Styles.driverVersionText, ref elementRect, errorBuilder);

                if (!result)
                    GUILayout.Label(EditorGUIUtility.TempContent(errorBuilder.ToString(), EditorGUIUtility.GetHelpIcon(MessageType.Error)), EditorStyles.helpBox);
            }

            public static void ClearNewElement(SerializedProperty filterListProp)
            {
                filterListProp.FindPropertyRelative(Styles.vendorText).stringValue = null;
                filterListProp.FindPropertyRelative(Styles.deviceNameText).stringValue = null;
                filterListProp.FindPropertyRelative(Styles.brandText).stringValue = null;
                filterListProp.FindPropertyRelative(Styles.productText).stringValue = null;
                filterListProp.FindPropertyRelative(Styles.androidOsVersionText).stringValue = null;
                filterListProp.FindPropertyRelative(Styles.vulkanApiVersionText).stringValue = null;
                filterListProp.FindPropertyRelative(Styles.driverVersionText).stringValue = null;
            }
        }
    }

    [CustomEditor(typeof(UnityEngine.VulkanDeviceFilterLists))]
    internal class VulkanDeviceFilterListsEditor : Editor
    {
        internal class ReorderableFilterList
        {
            public delegate void ElementCallbackDelegate(
                ReorderableList list,
                Rect rect, int index, bool isActive, bool isFocused);
            public delegate void AddCallbackDelegate(ReorderableList list);

            public SerializedObject serializedObject { get; private set; }
            public SerializedProperty serializedProperty { get; private set; }
            public ReorderableList reorderableList { get; private set; }

            private ElementCallbackDelegate m_DrawElementCallback;
            private AddCallbackDelegate m_AddElementCallback;

            public ReorderableFilterList(
                SerializedObject serializedObject, string propertyName,
                ElementCallbackDelegate drawElementCallback, AddCallbackDelegate onAddCallback,
                float elementHeight, float headerHeight = 0.0f)
            {
                this.serializedObject = serializedObject;
                this.serializedProperty = serializedObject.FindProperty(propertyName);
                this.reorderableList = new ReorderableList(serializedObject, serializedProperty, true, false, true, true);
                m_DrawElementCallback = drawElementCallback;
                this.reorderableList.drawElementCallback = DrawListElement;
                m_AddElementCallback = onAddCallback;
                this.reorderableList.onAddCallback = AddElement;
                this.reorderableList.onReorderCallback = (_)=>{};
                this.reorderableList.elementHeight = elementHeight;
                this.reorderableList.headerHeight = headerHeight;
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
        }

        VulkanDeviceFilterLists assetObject => serializedObject.targetObject as VulkanDeviceFilterLists;

        ReorderableFilterList m_AllowReorderableFilterList;
        ReorderableFilterList m_DenyReorderableFilterList;
        ReorderableFilterList m_GfxJobsReorderableFilterList;

        bool m_ShowAllow = false;
        bool m_ShowDeny = false;
        bool m_ShowGfxJobs = false;

        private void DrawDeviceFilterListElement(ReorderableList list, Rect rect, int index, bool isActive, bool isFocused)
        {
            var filterListProp = list.serializedProperty.GetArrayElementAtIndex(index);

            var elementRect = new Rect(rect);
            elementRect.height = EditorGUIUtility.singleLineHeight;
            elementRect.y += DeviceFilterUI.Styles.kHeightBetweenFields;

            DeviceFilterUI.Utils.DrawDeviceFilterList(filterListProp, index, ref elementRect);
        }

        private void AddDeviceFilterListElement(ReorderableList filterList)
        {
            var list = filterList.serializedProperty;
            var index = list.arraySize++;
            filterList.index = index;
            var filterListProp = list.GetArrayElementAtIndex(index);

            // Helper for clearing the regex strings
            DeviceFilterUI.Utils.ClearNewElement(filterListProp);
        }

        private void DrawGfxJobsFilterListElement(ReorderableList list, Rect rect, int index, bool isActive, bool isFocused)
        {
            var filterListProp = list.serializedProperty.GetArrayElementAtIndex(index);

            var elementRect = new Rect(rect);
            elementRect.height = EditorGUIUtility.singleLineHeight;
            elementRect.y += DeviceFilterUI.Styles.kHeightBetweenFields;

            var graphicsJobsPreferenceProp = filterListProp.FindPropertyRelative(DeviceFilterUI.Styles.preferredGraphicsJobsModeText);
            GraphicsJobsFilterMode mode = (GraphicsJobsFilterMode)graphicsJobsPreferenceProp.enumValueFlag;

            mode = (GraphicsJobsFilterMode)EditorGUI.EnumPopup(
                elementRect, DeviceFilterUI.Styles.preferredGraphicsJobsMode, mode);
            graphicsJobsPreferenceProp.intValue = (int)mode;

            var filterProp = filterListProp.FindPropertyRelative(DeviceFilterUI.Styles.filterText);

            // Helper for drawing the actual filter list
            DeviceFilterUI.Utils.DrawDeviceFilterList(filterProp, index, ref elementRect, true);
        }

        private void AddGfxJobsFilterListElement(ReorderableList filterList)
        {
            var list = filterList.serializedProperty;
            var index = list.arraySize++;
            filterList.index = index;
            var filterListProp = list.GetArrayElementAtIndex(index);

            var graphicsJobsPreferenceProp = filterListProp.FindPropertyRelative(DeviceFilterUI.Styles.preferredGraphicsJobsModeText).enumValueFlag = 0x0;

            // Helper for clearing the regex strings
            DeviceFilterUI.Utils.ClearNewElement(filterListProp.FindPropertyRelative(DeviceFilterUI.Styles.filterText));
        }

        public void OnEnable()
        {
            var allowDenyListElementHeight = DeviceFilterUI.Styles.kElementHeighWithSpace * DeviceFilterUI.Styles.kNumItemsInFilter + DeviceFilterUI.Styles.kHeightBetweenFields;
            m_AllowReorderableFilterList = new ReorderableFilterList(
                serializedObject,  "m_VulkanAllowFilterList", DrawDeviceFilterListElement, AddDeviceFilterListElement, allowDenyListElementHeight);
            m_DenyReorderableFilterList = new ReorderableFilterList(
                serializedObject,  "m_VulkanDenyFilterList", DrawDeviceFilterListElement, AddDeviceFilterListElement, allowDenyListElementHeight);

            var gfxJobsElementHeight = DeviceFilterUI.Styles.kElementHeighWithSpace * (DeviceFilterUI.Styles.kNumItemsInFilter + 1) + DeviceFilterUI.Styles.kHeightBetweenFields;
            m_GfxJobsReorderableFilterList = new ReorderableFilterList(
                serializedObject,  "m_GfxJobFilterList", DrawGfxJobsFilterListElement, AddGfxJobsFilterListElement, gfxJobsElementHeight);
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

            var rect = GUILayoutUtility.GetRect(startingHeight, 1.0f, GUILayout.ExpandWidth(true));
            rect.x += EditorGUI.kIndentPerLevel;
            rect.width -= EditorGUI.kIndentPerLevel;
            var height = EditorStyles.helpBox.CalcHeight(content, rect.width) + (DeviceFilterUI.Styles.kElementHeighWithSpace * 2);

            rect = GUILayoutUtility.GetRect(startingHeight, height, GUILayout.ExpandWidth(true));
            rect.x += EditorGUI.kIndentPerLevel;
            rect.width -= EditorGUI.kIndentPerLevel;

            EditorGUI.HelpBox(rect, content);
            return height;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var targetObj = serializedObject.targetObject as VulkanDeviceFilterLists;

            if (targetObj == null)
                throw new InvalidCastException("Unable to case object to VulkanDeviceFilterLists!");

            using (var changed = new ChangeCheckScope())
            {
                var height = 0.0f;

// Disable obsolete warning. Users should see obsolete warnings when trying to use the API but
// this is to ensure the user is warned that the settings are going to be ignored and that they
// should perform the conversion.
#pragma warning disable 612, 618
                var allowListCount = PlayerSettings.Android.androidVulkanAllowFilterList.Length;
                var denyListCount = PlayerSettings.Android.androidVulkanDenyFilterList.Length;

                if (allowListCount + denyListCount > 0)
                {
                    var buttonPressed = GUILayout.Button(DeviceFilterUI.Styles.conversionButton);
                    if (buttonPressed)
                        assetObject.ImportPlayerSettingsFiltersToAsset();
                }
#pragma warning restore 612, 618

                height = DoList(m_AllowReorderableFilterList, "Allow Filters", ref m_ShowAllow, height);
                height = DoList(m_DenyReorderableFilterList, "Deny Filters", ref m_ShowDeny, height);
                height = DoList(m_GfxJobsReorderableFilterList, "Preferred Graphics Jobs Filters", ref m_ShowGfxJobs, height, DrawGfxJobsExtraNotice);

                if (changed.changed)
                    serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
