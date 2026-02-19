// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // Must match D3D12DeviceFilterData in Runtime/Graphics/D3D12/D3D12DeviceFilterData.h
    public enum D3D12GraphicsDeviceType
    {
        DoNotCare = 0,
        Discrete = 1,
        Integrated = 2
    }

    // Must match D3D12DeviceFilterData in Runtime/Graphics/D3D12/D3D12DeviceFilterData.h
    public enum D3D12Comparator
    {
        EqualTo = 0,
        NotEqualTo = 1,
        LessThan = 2,
        LessThanOrEqualTo = 3,
        GreaterThan = 4,
        GreaterThanOrEqualTo = 5
    }

    // Must match D3D12DeviceFilterData in Runtime/Graphics/D3D12/D3D12DeviceFilterData.h
    public struct D3D12DeviceFilterData
    {
        public string vendorName;
        public string deviceName;
        public D3D12Comparator driverVersionComparator;
        public string driverVersion;
        public D3D12Comparator featureLevelComparator;
        public string featureLevel;
        public D3D12Comparator graphicsMemoryComparator;
        public string graphicsMemory;
        public D3D12Comparator processorCountComparator;
        public string processorCount;
        public D3D12GraphicsDeviceType deviceType;
    }

    // Must match D3D12GraphicsJobsDeviceFilterData in Runtime/Graphics/D3D12/D3D12DeviceFilterData.h
    public struct D3D12GraphicsJobsDeviceFilterData
    {
        public GraphicsJobsFilterMode preferredMode;
        public D3D12DeviceFilterData filter;
    }

    internal static class D3D12DeviceFilterUtils
    {
        private static readonly string vendorNameString = "vendorName";
        private static readonly string deviceNameString = "deviceName";
        private static readonly string driverVersionStringValue = "driverVersion";
        private static readonly string featureLevelStringValue = "featureLevel";
        private static readonly string graphicsMemoryString = "graphicsMemory";
        private static readonly string processorCountString = "processorCount";

        // Keep in sync with versionErrorMessage in PlayerSettingsAndroid.binding.cs (TODO)
        private static readonly string versionErrorMessage = "Version information should be formatted as:" +
            "\n1. 'MajorVersion.MinorVersion.PatchVersion.MinorPatvhVersion' where MinorVersion, PatchVersion and MinorPatchVersion " +
            "are optional and must only contain numbers";

        // Keep in sync with validVersionString in PlayerSettingsAndroid.binding.cs (TODO)
        private static readonly Regex validVersionString = new Regex(@"(^[0-9]+(\.[0-9]+){0,3}$)", RegexOptions.Compiled);

        internal static void CheckVersion(string value, string filterName, string fieldName)
        {
            if (!validVersionString.IsMatch(value))
                throw new ArgumentException($"Invalid version string in {filterName} for {fieldName}=\"{value}\": {versionErrorMessage}");
        }

        internal static void CheckRegex(string value, string filterName, string fieldName)
        {
            try
            {
                // NOTE: C++ side regular expressions are ECMAScript.
                // Try to create a regex from the input string to determine if it is a valid regex
                Regex regex = new Regex(value, RegexOptions.ECMAScript);
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

        internal static void CheckFilterData(D3D12DeviceFilterData filterData, string filterName)
        {
            // The check will throw an exception if there's an issue with the data.
            // We need to check the data here, as an invalid regex on the native side, can crash the game.
            if (!string.IsNullOrEmpty(filterData.vendorName))
                CheckRegex(filterData.vendorName, filterName, vendorNameString);
            if (!string.IsNullOrEmpty(filterData.deviceName))
                CheckRegex(filterData.deviceName, filterName, deviceNameString);
            if (!string.IsNullOrEmpty(filterData.driverVersion))
                CheckVersion(filterData.driverVersion, filterName, driverVersionStringValue);
            if (!string.IsNullOrEmpty(filterData.featureLevel))
                CheckVersion(filterData.featureLevel, filterName, featureLevelStringValue);
            if (!string.IsNullOrEmpty(filterData.graphicsMemory))
                CheckRegex(filterData.graphicsMemory, filterName, graphicsMemoryString);
            if (!string.IsNullOrEmpty(filterData.processorCount))
                CheckRegex(filterData.processorCount, filterName, processorCountString);
        }

        internal static void CheckAllFilterData(D3D12DeviceFilterData[] filterDataList, string filterName)
        {
            // The check will throw an exception if there's an issue with the data.
            // We need to check the data here, as an invalid regex on the native side, can crash the game.
            foreach (var filterData in filterDataList)
            {
                CheckFilterData(filterData, filterName);
            }
        }

        internal static void CheckAllGraphicsJobsFilterData(D3D12GraphicsJobsDeviceFilterData[] filterDataList, string filterName)
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
    [NativeHeader("Runtime/Graphics/D3D12/D3D12DeviceFilterLists.h")]
    [HelpURL("d3d12-device-filter-list-asset-reference")]
    public sealed class D3D12DeviceFilterLists : UnityEngine.Object
    {
        private static extern void Internal_CreateD3D12DeviceFilterLists([Writable] D3D12DeviceFilterLists obj, string name);

        // The D3D12 Filters
        public extern D3D12DeviceFilterData[] d3D12DeviceAllowFilters { get; set; }
        public extern D3D12DeviceFilterData[] d3D12DeviceDenyFilters { get; set; }

        // The Graphics Jobs Filters
        public extern D3D12GraphicsJobsDeviceFilterData[] d3D12GraphicsJobsDeviceFilters { get; set; }

        public D3D12DeviceFilterLists(string name = "D3D12DeviceFilterLists")
        {
            Internal_CreateD3D12DeviceFilterLists(this, name);
        }

        public void EnsureValidOrThrow()
        {
            D3D12DeviceFilterUtils.CheckAllFilterData(d3D12DeviceAllowFilters, "D3D12 Allow Device Filter List");
            D3D12DeviceFilterUtils.CheckAllFilterData(d3D12DeviceDenyFilters, "D3D12 Deny Device Filter List");

            foreach (var filterData in d3D12GraphicsJobsDeviceFilters)
            {
                D3D12DeviceFilterUtils.CheckFilterData(filterData.filter, "D3D12 Graphics Jobs Device Filter List");
            }
        }
    }
}
