// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // Must match UnityEditor.AndroidDeviceFilterData and AndroidDeviceFilterData in PlayerSettings.h
    public struct VulkanDeviceFilterData
    {
        public string vendorName;
        public string deviceName;
        public string brandName;
        public string productName;
        public string androidOsVersionString;
        public string vulkanApiVersionString;
        public string driverVersionString;
    }

    // Must match VulkanGraphicsJobsDeviceFilterData in PlayerSettings.h
    public struct VulkanGraphicsJobsDeviceFilterData
    {
        public GraphicsJobsFilterMode preferredMode;
        public VulkanDeviceFilterData filter;
    }

    internal static class VulkanDeviceFilterUtils
    {
        private static readonly string vendorNameString = "vendorName";
        private static readonly string deviceNameString = "deviceName";
        private static readonly string brandNameString = "brandName";
        private static readonly string productNameString = "productName";
        private static readonly string androidOsVersionString = "androidOsVersionString";
        private static readonly string vulkanApiVersionStringValue = "vulkanApiVersionString";
        private static readonly string driverVersionStringValue = "driverVersionString";

        // Keep in sync with versionErrorMessage in PlayerSettingsAndroid.binding.cs
        private static readonly string versionErrorMessage = "Version information should be formatted as:" +
            "\n1. 'MajorVersion.MinorVersion.PatchVersion' where MinorVersion and PatchVersion are optional and must only " +
            "contain numbers, or \n2. Hex number beginning with '0x' (max 4-bytes)";

        // Keep in sync with validVersionString in PlayerSettingsAndroid.binding.cs
        private static readonly Regex validVersionString = new Regex(@"(^[0-9]+(\.[0-9]+){0,2}$)|(^0(x|X)([A-Fa-f0-9]{1,8})$)", RegexOptions.Compiled);

        internal static void CheckVersion(string value, string filterName, string fieldName)
        {
            if (!validVersionString.IsMatch(value))
                throw new ArgumentException($"Invalid version string in {filterName} for {fieldName}=\"{value}\": {versionErrorMessage}");
        }

        internal static void CheckRegex(string value, string filterName, string fieldName)
        {
            try
            {
                // Try to create a regex from the input string to determine if it is a valid regex
                Regex regex = new Regex(value);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException($"Invalid Regular Expression in {filterName} for {fieldName}=\"{value}\": {e.Message}");
            }
        }

        public static bool HasErrorRegex(string val, string fieldName, out string errorString)
        {
            if (!string.IsNullOrEmpty(val))
            {
                try
                {
                    // Try to create a regex from the input string to determine if it is a valid regex
                    Regex regex = new Regex(val);
                }
                catch (ArgumentException e)
                {
                    errorString = $"Invalid Regular Expression for {fieldName}=\"{val}\": {e.Message}";
                    return true;
                }
            }

            errorString = null;
            return false;
        }

        public static bool HasErrorVersion(string val, string fieldName, out string errorString)
        {
            if (!string.IsNullOrEmpty(val) && !validVersionString.IsMatch(val))
            {
                errorString = $"Invalid version string for {fieldName}={val}: {versionErrorMessage}";
                return true;
            }

            errorString = null;
            return false;
        }

        internal static void CheckFilterData(VulkanDeviceFilterData filterData, string filterName)
        {
            // The check will throw an exception if there's an issue with the data.
            // We need to check the data here, as an invalid regex on the native side, can crash the game.
            if (!string.IsNullOrEmpty(filterData.vendorName))
                CheckRegex(filterData.vendorName, filterName, vendorNameString);
            if (!string.IsNullOrEmpty(filterData.deviceName))
                CheckRegex(filterData.deviceName, filterName, deviceNameString);
            if (!string.IsNullOrEmpty(filterData.brandName))
                CheckRegex(filterData.brandName, filterName, brandNameString);
            if (!string.IsNullOrEmpty(filterData.productName))
                CheckRegex(filterData.productName, filterName, productNameString);
            if (!string.IsNullOrEmpty(filterData.androidOsVersionString))
                CheckRegex(filterData.androidOsVersionString, filterName, androidOsVersionString);

            if (!string.IsNullOrEmpty(filterData.vulkanApiVersionString))
                CheckVersion(filterData.vulkanApiVersionString, filterName, vulkanApiVersionStringValue);
            if (!string.IsNullOrEmpty(filterData.driverVersionString))
                CheckVersion(filterData.driverVersionString, filterName, driverVersionStringValue);
        }

        internal static void CheckAllFilterData(VulkanDeviceFilterData[] filterDataList, string filterName)
        {
            // The check will throw an exception if there's an issue with the data.
            // We need to check the data here, as an invalid regex on the native side, can crash the game.
            foreach (var filterData in filterDataList)
            {
                CheckFilterData(filterData, filterName);
            }
        }

        internal static void CheckAllGraphicsJobsFilterData(VulkanGraphicsJobsDeviceFilterData[] filterDataList, string filterName)
        {
            // The check will throw an exception if there's an issue with the data.
            // We need to check the data here, as an invalid regex on the native side, can crash the game.
            foreach (var filterData in filterDataList)
            {
                CheckFilterData(filterData.filter, filterName);
            }
        }
    }

    // Note: This asset is immutable at runtime but is needed to ensure that the asset can be read in native code.
    // We don't use the UnityEditor namespace for it but we do only allow it's entries to be used in the editor.
    [NativeHeader("Runtime/Graphics/Vulkan/VulkanDeviceFilterLists.h")]
    public sealed class VulkanDeviceFilterLists : UnityEngine.Object
    {
        private static extern void Internal_CreateVulkanDeviceFilterLists([Writable] VulkanDeviceFilterLists obj, string name);
        private static extern void Internal_ConvertPlayerSettingsToAsset(VulkanDeviceFilterLists obj);

        // Used in PlayerSettingsEditorExtensions.cs
        internal void ImportPlayerSettingsFiltersToAsset()
        {
            Internal_ConvertPlayerSettingsToAsset(this);
        }

        // The Vulkan Filters
        public extern VulkanDeviceFilterData[] vulkanDeviceAllowFilters { get; set; }
        public extern VulkanDeviceFilterData[] vulkanDeviceDenyFilters { get; set; }

        // The Graphics Jobs Filters
        public extern VulkanGraphicsJobsDeviceFilterData[] vulkanGraphicsJobsDeviceFilters { get; set; }

        public VulkanDeviceFilterLists(string name = "VulkanDeviceFilterLists")
        {
            Internal_CreateVulkanDeviceFilterLists(this, name);
        }

        public void EnsureValidOrThrow()
        {
            VulkanDeviceFilterUtils.CheckAllFilterData(vulkanDeviceAllowFilters, "Vulkan Allow Device Filter List");
            VulkanDeviceFilterUtils.CheckAllFilterData(vulkanDeviceDenyFilters, "Vulkan Deny Device Filter List");

            foreach (var filterData in vulkanGraphicsJobsDeviceFilters)
            {
                VulkanDeviceFilterUtils.CheckFilterData(filterData.filter, "Vulkan Graphics Jobs Device Filter List");
            }
        }
    }
}
