// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // Must match UnityEditor.WebGPUDeviceType in Runtime/Graphics/WebGPU/WebGPUDeviceFilterData.h
    public enum WebGPUDeviceType
    {
        DoNotCare = 0,
        Mobile = 1,
        Desktop = 2
    }

    // Must match UnityEditor.WebGPUComparator in Runtime/Graphics/WebGPU/WebGPUDeviceFilterData.h
    public enum WebGPUComparator
    {
        None = 0,
        EqualTo = 1,
        NotEqualTo = 2,
        LessThan = 3,
        LessThanOrEqualTo = 4,
        GreaterThan = 5,
        GreaterThanOrEqualTo = 6
    }

    // Must match UnityEditor.WebGPUDeviceFeature in Runtime/Graphics/WebGPU/WebGPUDeviceFilterData.h
    public enum WebGPUDeviceFeature
    {
        None,
        CoreFeaturesAndLimits = 1,
        DepthClipControl = 2,
        Depth32FloatStencil8 = 3,
        TextureCompressionBC = 4,
        TextureCompressionBCSliced3D = 5,
        TextureCompressionETC2 = 6,
        TextureCompressionASTC = 7,
        TextureCompressionASTCSliced3D = 8,
        TimestampQuery = 9,
        IndirectFirstInstance = 10,
        ShaderF16 = 11,
        RG11B10UfloatRenderable = 12,
        BGRA8UnormStorage = 13,
        Float32Filterable = 14,
        Float32Blendable = 15,
        ClipDistances = 16,
        DualSourceBlending = 17,
        Subgroups = 18,
        TextureFormatsTier1 = 19,
        TextureFormatsTier2 = 20,
        PrimitiveIndex = 21,
        TextureComponentSwizzle = 22,
    }

    // Must match UnityEditor.WebGPUDeviceLimit in Runtime/Graphics/WebGPU/WebGPUDeviceFilterData.h
    public enum WebGPUDeviceLimit
    {
        None = 0,
        MaxUniformBufferBindingSize = 1,
        MaxStorageBufferBindingSize = 2,
        MaxBufferSize = 3,
        MaxTextureDimension1D = 4,
        MaxTextureDimension2D = 5,
        MaxTextureDimension3D = 6,
        MaxTextureArrayLayers = 7,
        MaxBindGroups = 8,
        MaxBindGroupsPlusVertexBuffers = 9,
        MaxBindingsPerBindGroup = 10,
        MaxDynamicUniformBuffersPerPipelineLayout = 11,
        MaxDynamicStorageBuffersPerPipelineLayout = 12,
        MaxSampledTexturesPerShaderStage = 13,
        MaxSamplersPerShaderStage = 14,
        MaxStorageBuffersPerShaderStage = 15,
        MaxStorageBuffersInVertexStage = 16,
        MaxStorageBuffersInFragmentStage = 17,
        MaxStorageTexturesPerShaderStage = 18,
        MaxStorageTexturesInVertexStage = 19,
        MaxStorageTexturesInFragmentStage = 20,
        MaxUniformBuffersPerShaderStage = 21,
        MinUniformBufferOffsetAlignment = 22,
        MinStorageBufferOffsetAlignment = 23,
        MaxVertexBuffers = 24,
        MaxVertexAttributes = 25,
        MaxVertexBufferArrayStride = 26,
        MaxInterStageShaderVariables = 27,
        MaxColorAttachments = 28,
        MaxColorAttachmentBytesPerSample = 29,
        MaxComputeWorkgroupStorageSize = 30,
        MaxComputeInvocationsPerWorkgroup = 31,
        MaxComputeWorkgroupSizeX = 32,
        MaxComputeWorkgroupSizeY = 33,
        MaxComputeWorkgroupSizeZ = 34,
        MaxComputeWorkgroupsPerDimension = 35
    }

    // Must match UnityEditor.WebGPUDeviceFilterLimit in Runtime/Graphics/WebGPU/WebGPUDeviceFilterData.h
    public struct WebGPUDeviceFilterLimit
    {
        public WebGPUDeviceLimit limit;
        public WebGPUComparator comparator;
        public ulong value;
    }

    // Must match UnityEditor.WebGPUDeviceFilterData in Runtime/Graphics/WebGPU/WebGPUDeviceFilterData.h
    public struct WebGPUDeviceFilterData
    {
        public string browserName;
        public string browserVersion;
        public WebGPUComparator browserVersionComparator;
        public WebGPUDeviceType deviceType;
        public WebGPUDeviceFeature[] features;
        public WebGPUDeviceFilterLimit[] limits;
    }

    internal static class WebGPUDeviceFilterUtils
    {
        private static readonly string browserVersionString = "browserVersion";
        private static readonly string versionErrorMessage = "Version information should be formatted as:" +
            "\n1. 'MajorVersion.MinorVersion.PatchVersion.MinorPatvhVersion' where MinorVersion, PatchVersion and MinorPatchVersion " +
            "are optional and must only contain numbers";
        // Keep in sync with validVersionString in PlayerSettingsAndroid.binding.cs (TODO)
        private static readonly Regex validVersionString = new Regex(@"(^[0-9]+(\.[0-9]+){0,3}$)", RegexOptions.Compiled);

        internal static void CheckRegex(string value, string filterName, string fieldName)
        {
            try
            {
                // NOTE: C++ defaults to ECMAScript so we need to at least ensure the string is ECMAScript compliant.
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
                    Regex regex = new Regex(val, RegexOptions.ECMAScript);
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

        internal static void CheckFilterData(WebGPUDeviceFilterData filterData, string filterName)
        {
            // The check will throw an exception if there's an issue with the data.
            // We need to check the data here, as an invalid regex on the native side, can crash the game.
            if (!string.IsNullOrEmpty(filterData.browserVersion))
                CheckRegex(filterData.browserVersion, filterName, browserVersionString);
        }

        internal static void CheckAllFilterData(WebGPUDeviceFilterData[] filterDataList, string filterName)
        {
            // The check will throw an exception if there's an issue with the data.
            // We need to check the data here, as an invalid regex on the native side, can crash the game.
            foreach (var filterData in filterDataList)
            {
                CheckFilterData(filterData, filterName);
            }
        }
    }

    // Note: This asset is immutable at runtime but is needed to ensure that the asset can be read in native code.
    // We don't use the UnityEditor namespace for it but we do only allow it's entries to be used in the editor.
    [NativeHeader("Runtime/Graphics/WebGPU/WebGPUDeviceFilterLists.h")]
    public sealed class WebGPUDeviceFilterLists : UnityEngine.Object
    {
        private static extern void Internal_CreateWebGPUDeviceFilterLists([Writable] WebGPUDeviceFilterLists obj, string name);

        // The WebGPU Filters
        public extern WebGPUDeviceFilterData[] deviceAllowFilters { get; set; }
        public extern WebGPUDeviceFilterData[] deviceDenyFilters { get; set; }

        public WebGPUDeviceFilterLists(string name = "WebGPUDeviceFilterLists")
        {
            Internal_CreateWebGPUDeviceFilterLists(this, name);
        }

        public void EnsureValidOrThrow()
        {
            WebGPUDeviceFilterUtils.CheckAllFilterData(deviceAllowFilters, "WebGPU Allow Device Filter List");
            WebGPUDeviceFilterUtils.CheckAllFilterData(deviceDenyFilters, "WebGPU Deny Device Filter List");
        }
    }
}
