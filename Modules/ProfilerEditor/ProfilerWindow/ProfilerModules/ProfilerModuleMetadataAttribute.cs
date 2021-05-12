// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Profiling.Editor
{
    // TODO Make public as part of Extensibility API.
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal partial class ProfilerModuleMetadataAttribute : Attribute
    {
        public ProfilerModuleMetadataAttribute(string displayName)
        {
            DisplayName = displayName;
        }

        public string DisplayName { get; }

        public string IconPath { get; set; } = "Profiler.Custom";
    }

    internal partial class ProfilerModuleMetadataAttribute : Attribute
    {
        // Internally we localize module names using this constructor.
        internal ProfilerModuleMetadataAttribute(string displayNameKey, Type localizationResourceType)
        {
            var resource = Activator.CreateInstance(localizationResourceType) as IResource;
            if (resource == null)
                throw new ArgumentException($"Resource type must implement {nameof(IResource)}.", "resourceType");

            DisplayName = resource.GetLocalizedString(displayNameKey);
        }

        internal interface IResource
        {
            string GetLocalizedString(string key);
        }
    }
}
