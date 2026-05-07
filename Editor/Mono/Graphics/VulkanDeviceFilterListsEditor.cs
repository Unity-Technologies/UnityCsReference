// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal static class DeviceFilterUI
    {
        internal static class Styles
        {
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

            // Localized plain strings
            public static readonly string gfxJobsFilterOrderInfo = L10n.Tr("The order of the Graphics Jobs Filters is important. Filtering will use the first passing filter to determine Graphics Jobs Mode at runtime.");

            // Styling
            public const float kItemPaddingTop = 4f;
            public const float kItemPaddingBottom = 2f;
            public const float kItemPaddingHorizontal = 2f;
            public const float kErrorBoxMarginTop = 1f;
        }

        internal static class Utils
        {
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
        VulkanDeviceFilterLists assetObject => serializedObject.targetObject as VulkanDeviceFilterLists;

        class DeviceFilterItemElement : VisualElement
        {
            private TextField vendorField;
            private TextField deviceNameField;
            private TextField brandField;
            private TextField productField;
            private TextField osVersionField;
            private TextField vulkanApiVersionField;
            private TextField driverVersionField;
            private HelpBox errorBox;

            public DeviceFilterItemElement()
            {
                style.paddingTop = DeviceFilterUI.Styles.kItemPaddingTop;
                style.paddingBottom = DeviceFilterUI.Styles.kItemPaddingBottom;
                style.paddingLeft = DeviceFilterUI.Styles.kItemPaddingHorizontal;
                style.paddingRight = DeviceFilterUI.Styles.kItemPaddingHorizontal;

                vendorField = CreateTextField(DeviceFilterUI.Styles.vendor);
                deviceNameField = CreateTextField(DeviceFilterUI.Styles.deviceName);
                brandField = CreateTextField(DeviceFilterUI.Styles.brand);
                productField = CreateTextField(DeviceFilterUI.Styles.product);
                osVersionField = CreateTextField(DeviceFilterUI.Styles.osVersion);
                vulkanApiVersionField = CreateTextField(DeviceFilterUI.Styles.vulkanApiVersion);
                driverVersionField = CreateTextField(DeviceFilterUI.Styles.driverVersion);

                errorBox = new HelpBox("", HelpBoxMessageType.Error);
                errorBox.style.display = DisplayStyle.None;
                errorBox.style.marginTop = DeviceFilterUI.Styles.kErrorBoxMarginTop;
                errorBox.style.whiteSpace = WhiteSpace.Normal;
                Add(errorBox);
            }

            TextField CreateTextField(GUIContent content)
            {
                var field = new TextField(content.text) { tooltip = content.tooltip };
                field.AddToClassList(BaseField<string>.alignedFieldUssClassName);
                Add(field);
                return field;
            }

            void BindAndTrack(TextField field, SerializedProperty prop)
            {
                field.BindProperty(prop);
                this.TrackPropertyValue(prop, _ => ValidateFields());
            }

            public virtual void Bind(SerializedProperty filterProp)
            {
                BindAndTrack(vendorField, filterProp.FindPropertyRelative(DeviceFilterUI.Styles.vendorText));
                BindAndTrack(deviceNameField, filterProp.FindPropertyRelative(DeviceFilterUI.Styles.deviceNameText));
                BindAndTrack(brandField, filterProp.FindPropertyRelative(DeviceFilterUI.Styles.brandText));
                BindAndTrack(productField, filterProp.FindPropertyRelative(DeviceFilterUI.Styles.productText));
                BindAndTrack(osVersionField, filterProp.FindPropertyRelative(DeviceFilterUI.Styles.androidOsVersionText));
                BindAndTrack(vulkanApiVersionField, filterProp.FindPropertyRelative(DeviceFilterUI.Styles.vulkanApiVersionText));
                BindAndTrack(driverVersionField, filterProp.FindPropertyRelative(DeviceFilterUI.Styles.driverVersionText));

                ValidateFields();
            }

            public virtual void Unbind()
            {
                vendorField.Unbind();
                deviceNameField.Unbind();
                brandField.Unbind();
                productField.Unbind();
                osVersionField.Unbind();
                vulkanApiVersionField.Unbind();
                driverVersionField.Unbind();
            }

            protected void ValidateFields()
            {
                var sb = new StringBuilder();
                CheckRegexField(sb, vendorField.value, vendorField.label, DeviceFilterUI.Styles.vendorText);
                CheckRegexField(sb, deviceNameField.value, deviceNameField.label, DeviceFilterUI.Styles.deviceNameText);
                CheckRegexField(sb, brandField.value, brandField.label, DeviceFilterUI.Styles.brandText);
                CheckRegexField(sb, productField.value, productField.label, DeviceFilterUI.Styles.productText);
                CheckRegexField(sb, osVersionField.value, osVersionField.label, DeviceFilterUI.Styles.androidOsVersionText);
                CheckVersionField(sb, vulkanApiVersionField.value, vulkanApiVersionField.label, DeviceFilterUI.Styles.vulkanApiVersionText);
                CheckVersionField(sb, driverVersionField.value, driverVersionField.label, DeviceFilterUI.Styles.driverVersionText);

                bool hasErrors = sb.Length > 0;
                errorBox.style.display = hasErrors ? DisplayStyle.Flex : DisplayStyle.None;
                if (hasErrors)
                    errorBox.text = sb.ToString();
            }

            void CheckRegexField(StringBuilder sb, string value, string label, string fieldName)
            {
                if (VulkanDeviceFilterUtils.HasErrorRegex(value, fieldName, out var errorString))
                    sb.AppendLine($"{label}: {errorString}");
            }

            void CheckVersionField(StringBuilder sb, string value, string label, string fieldName)
            {
                if (VulkanDeviceFilterUtils.HasErrorVersion(value, fieldName, out var errorString))
                    sb.AppendLine($"{label}: {errorString}");
            }
        }

        class GfxJobsFilterItemElement : DeviceFilterItemElement
        {
            private EnumField preferredModeField;

            public GfxJobsFilterItemElement()
            {
                preferredModeField = new EnumField(DeviceFilterUI.Styles.preferredGraphicsJobsMode.text, GraphicsJobsFilterMode.Off)
                {
                    tooltip = DeviceFilterUI.Styles.preferredGraphicsJobsMode.tooltip
                };
                preferredModeField.AddToClassList(BaseField<Enum>.alignedFieldUssClassName);
                Insert(0, preferredModeField);
            }

            public override void Bind(SerializedProperty filterProp)
            {
                preferredModeField.BindProperty(
                    filterProp.FindPropertyRelative(DeviceFilterUI.Styles.preferredGraphicsJobsModeText));

                var innerFilterProp = filterProp.FindPropertyRelative(DeviceFilterUI.Styles.filterText);
                base.Bind(innerFilterProp);
            }

            public override void Unbind()
            {
                preferredModeField.Unbind();
                base.Unbind();
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // Legacy import button
            SetupImportButton(root);

            // Allow Filters
            var allowFoldout = CreateFilterFoldout(root, "Allow Filters",
                "vulkan-allow-filters-foldout", "m_VulkanAllowFilterList", isGfxJobs: false);

            // Deny Filters
            var denyFoldout = CreateFilterFoldout(root, "Deny Filters",
                "vulkan-deny-filters-foldout", "m_VulkanDenyFilterList", isGfxJobs: false);

            // Preferred Graphics Jobs Filters
            var gfxJobsFoldout = CreateFilterFoldout(root, "Preferred Graphics Jobs Filters",
                "vulkan-gfxjobs-filters-foldout", "m_GfxJobFilterList", isGfxJobs: true,
                infoText: DeviceFilterUI.Styles.gfxJobsFilterOrderInfo);

            root.Bind(serializedObject);
            return root;
        }

        void SetupImportButton(VisualElement root)
        {
// Disable obsolete warning. Users should see obsolete warnings when trying to use the API but
// this is to ensure the user is warned that the settings are going to be ignored and that they
// should perform the conversion.
#pragma warning disable 612, 618
            var allowCount = PlayerSettings.Android.androidVulkanAllowFilterList.Length;
            var denyCount = PlayerSettings.Android.androidVulkanDenyFilterList.Length;
#pragma warning restore 612, 618

            if (allowCount + denyCount <= 0)
                return;

            var importButton = new Button
            {
                text = DeviceFilterUI.Styles.conversionButton.text,
                tooltip = DeviceFilterUI.Styles.conversionButton.tooltip
            };
            importButton.clicked += () =>
            {
                assetObject.ImportPlayerSettingsFiltersToAsset();
                serializedObject.Update();
                importButton.style.display = DisplayStyle.None;
            };
            root.Add(importButton);
        }

        Foldout CreateFilterFoldout(VisualElement root, string title, string viewDataKey,
            string propertyPath, bool isGfxJobs, string infoText = null)
        {
            var foldout = new Foldout { text = title, name = viewDataKey, value = false, viewDataKey = viewDataKey };

            if (infoText != null)
            {
                var infoBox = new HelpBox(infoText, HelpBoxMessageType.Info);
                infoBox.style.marginLeft = 0;
                infoBox.style.marginRight = 0;
                foldout.Add(infoBox);
            }

            var listView = new ListView
            {
                reorderable = true,
                showAddRemoveFooter = true,
                reorderMode = ListViewReorderMode.Animated,
                showBorder = true,
                showFoldoutHeader = false,
                showBoundCollectionSize = false,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            };

            SetupFilterList(listView, propertyPath, isGfxJobs);
            foldout.Add(listView);
            root.Add(foldout);
            return foldout;
        }

        void SetupFilterList(ListView listView, string propertyPath, bool isGfxJobs)
        {
            var arrayProp = serializedObject.FindProperty(propertyPath);

            listView.makeItem = isGfxJobs
                ? () => new GfxJobsFilterItemElement()
                : () => new DeviceFilterItemElement();

            listView.bindItem = (element, index) =>
            {
                var itemElement = (DeviceFilterItemElement)element;
                itemElement.Unbind();

                if (index >= arrayProp.arraySize)
                    return;

                var elementProp = arrayProp.GetArrayElementAtIndex(index);
                itemElement.Bind(elementProp);
            };

            listView.unbindItem = (element, index) =>
            {
                var itemElement = (DeviceFilterItemElement)element;
                itemElement.Unbind();
            };

            listView.itemsAdded += indices =>
            {
                foreach (var idx in indices)
                {
                    if (idx >= arrayProp.arraySize)
                        continue;

                    var elemProp = arrayProp.GetArrayElementAtIndex(idx);
                    if (isGfxJobs)
                    {
                        elemProp.FindPropertyRelative(DeviceFilterUI.Styles.preferredGraphicsJobsModeText)
                            .intValue = 0;
                        DeviceFilterUI.Utils.ClearNewElement(
                            elemProp.FindPropertyRelative(DeviceFilterUI.Styles.filterText));
                    }
                    else
                    {
                        DeviceFilterUI.Utils.ClearNewElement(elemProp);
                    }
                }

                serializedObject.ApplyModifiedProperties();
                listView.RefreshItems();
            };

            listView.BindProperty(arrayProp);
        }
    }
}
