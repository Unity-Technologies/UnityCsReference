// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Profiling.Editor
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public partial class ProfilerModuleMetadataAttribute : Attribute
    {
        public ProfilerModuleMetadataAttribute(string displayName)
        {
            DisplayName = displayName;
        }

        public string DisplayName { get; }

        public string Tooltip { get; set; }

        public string IconPath { get; set; } = "Profiler.Custom";
    }

    public partial class ProfilerModuleMetadataAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilerModuleMetadataAttribute"/> class.
        /// </summary>
        /// <param name="displayNameKey">The key for the localized display name.</param>
        /// <param name="localizationResourceType">The type of the localization resource.</param>
        internal ProfilerModuleMetadataAttribute(string displayNameKey, Type localizationResourceType)
            : this(displayNameKey, null, localizationResourceType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilerModuleMetadataAttribute"/> class.
        /// </summary>
        /// <param name="displayNameKey">The key for the localized display name.</param>
        /// <param name="tooltipKey">The key for the localized tooltip.</param>
        /// <param name="localizationResourceType">The type of the localization resource.</param>
        internal ProfilerModuleMetadataAttribute(string displayNameKey, string tooltipKey, Type localizationResourceType)
        {
            var resource = Activator.CreateInstance(localizationResourceType) as IResource;
            if (resource == null)
                throw new ArgumentException($"Resource type must implement {nameof(IResource)}.", "resourceType");

            DisplayName = resource.GetLocalizedString(displayNameKey);
            if (!string.IsNullOrEmpty(tooltipKey))
                Tooltip = resource.GetLocalizedString(tooltipKey);
        }

        internal interface IResource
        {
            string GetLocalizedString(string key);
        }
    }
}
