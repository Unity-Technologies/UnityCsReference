// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.HardwareProfiles;

namespace UnityEditor.HardwareProfiles
{
    [Serializable]
    public enum GraphicsAPI
    {
        Default,
        UseOpenGles,
        UseVulkan
    }

    [Serializable]
    public enum State
    {
        Default,
        Enabled,
        Disabled
    }

    [Serializable]
    public class DefaultWorkarounds
    {
        [SerializeField] protected List<string> disabled = new();
        [SerializeField] protected List<string> enabled = new();

        public static readonly string[] DefinedWorkarounds =
        {
            "HasBuggyPipelineCacheDataSize",
            "HasBuggyPipelineCacheHeaderVersion",
            "HasBuggyBackBufferCopyImage",
            "HasBuggyRenderingWithoutFragmentShader",
            "HasBuggyResetCommandBuffer",
            "HasBuggyCopyImageToBuffer",
            "HasBuggyAutoResolveStoreResolvedOnly",
            "HasBuggySubAllocatedColorAttachment",
            "HasBuggyRenderingWithColorMask0",
            "HasBuggyLoadStoreAttachmentOps",
            "HasBuggyBitfieldUExtract",
            "HasBuggyTransferExecutionDependencyChain",
            "HasBuggySRGBSwapChain",
            "HasBuggyDescriptorSetUpdateTemplate",
            "HasBuggyDebugUtilsLabels",
            "HasBuggyMSAAResolvePass",
            "HasBuggyPSOSerialization"
        };

        public virtual void DisableAll()
        {
            enabled.Clear();
            disabled.Clear();
            disabled.AddRange(DefinedWorkarounds);
        }

        public virtual void SetWorkaround(string workaroundName, State state)
        {
            enabled.Remove(workaroundName);
            disabled.Remove(workaroundName);

            switch (state)
            {
                case State.Default:
                    break;
                case State.Disabled:
                    disabled.Add(workaroundName);
                    break;
                case State.Enabled:
                    enabled.Add(workaroundName);
                    break;
                default:
                    throw new NotImplementedException(state.ToString());
            }
        }
    }

    [Serializable]
    public class ProfileWorkarounds : DefaultWorkarounds
    {
        [SerializeField] protected List<string> @default = new();

        public override void DisableAll()
        {
            @default.Clear();
            base.DisableAll();
        }

        public void ClearAll()
        {
            enabled.Clear();
            disabled.Clear();
            @default.Clear();
            @default.AddRange(DefinedWorkarounds);
        }

        public override void SetWorkaround(string workaroundName, State state)
        {
            enabled.Remove(workaroundName);
            disabled.Remove(workaroundName);
            @default.Remove(workaroundName);

            switch (state)
            {
                case State.Default:
                    @default.Add(workaroundName);
                    break;
                case State.Disabled:
                    disabled.Add(workaroundName);
                    break;
                case State.Enabled:
                    enabled.Add(workaroundName);
                    break;
                default:
                    throw new NotImplementedException(state.ToString());
            }
        }
    }

    [Serializable]
    public class GenericHardwareDescription
    {
        [SerializeField] protected string androidOsVersion;
        [SerializeField] protected string vulkanVersion;
        [SerializeField] protected string driverVersion;
        [SerializeField] protected string graphicsAPI =  nameof(GraphicsAPI.Default);
    }

    [Serializable]
    public class SpecificHardwareDescription : GenericHardwareDescription
    {
        [SerializeField] protected string vendor;
        [SerializeField] protected string device;
        [SerializeField] protected string brand;
        [SerializeField] protected string product;
        [SerializeField] protected string graphicsJobsFilterMode;
    }

    [Serializable]
    public class DefaultDeviceFilter : GenericHardwareDescription
    {
        [SerializeField] protected DefaultWorkarounds workarounds;

        public DefaultDeviceFilter()
        {
            androidOsVersion = "";
            vulkanVersion = "";
            driverVersion = "";
            graphicsAPI = nameof(GraphicsAPI.Default);
            workarounds = new();
        }

        public string MinAndroidOsVersion {get => androidOsVersion; set => androidOsVersion = value; }
        public string MinVulkanVersion {get => vulkanVersion; set => vulkanVersion = value; }
        public string MinDriverVersion {get => driverVersion; set => driverVersion = value; }
        public void SetGraphicsAPI(GraphicsAPI api) => graphicsAPI = api.ToString();

        public void SetWorkaround(string workaroundName, State state)
        {
            workarounds.SetWorkaround(workaroundName, state);
        }

        public void DisableAllWorkarounds()
        {
            workarounds.DisableAll();
        }
    }

    [Serializable]
    public class ProfileDeviceFilter : SpecificHardwareDescription
    {
        [SerializeField] protected ProfileWorkarounds workarounds;

        public ProfileDeviceFilter(string vendor, string device, string brand, string product)
        {
            androidOsVersion = "";
            vulkanVersion = "";
            driverVersion = "";
            this.vendor = vendor;
            this.device = device;
            this.brand = brand;
            this.product = product;
            graphicsAPI = nameof(GraphicsAPI.Default);
            graphicsJobsFilterMode = null;
            workarounds = new();
        }

        public string RequiredAndroidOsVersion {get => androidOsVersion; set => androidOsVersion = value; }
        public string RequiredVulkanVersion {get => vulkanVersion; set => vulkanVersion = value; }
        public string RequiredDriverVersion {get => driverVersion; set => driverVersion = value; }
        public void SetGraphicsAPI(GraphicsAPI api) => graphicsAPI = api.ToString();
        public void SetGraphicsJobsFilterMode(GraphicsJobsFilterMode? mode) => graphicsJobsFilterMode = mode.ToString();

        public void SetWorkaround(string workaroundName, State state)
        {
            workarounds.SetWorkaround(workaroundName, state);
        }

        public void DisableAllWorkarounds()
        {
            workarounds.DisableAll();
        }
    }

    [Serializable]
    public class ProfileDatabase
    {
        [SerializeField] protected DefaultDeviceFilter @default = new();
        [SerializeField] protected List<ProfileDeviceFilter> profiles = new();

        public DefaultDeviceFilter GetDefaultFilter() => @default;

        public ProfileDeviceFilter CreateFilter(string vendor, string device, string brand, string product)
        {
            ProfileDeviceFilter filter = new ProfileDeviceFilter(vendor, device, brand, product);
            profiles.Add(filter);
            return filter;
        }
    }
}
