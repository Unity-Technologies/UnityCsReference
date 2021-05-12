// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Profiling;

namespace Unity.Profiling.Editor
{
    static class ProfilerModuleTypeValidator
    {
        public static bool IsValidModuleTypeDefinition(Type moduleType, out ProfilerModuleMetadataAttribute moduleMetadata, out string errorDescription)
        {
            moduleMetadata = null;
            errorDescription = null;

            if (moduleType == null)
                return false;

            // Fail abstract types. Not an error.
            if (moduleType.IsAbstract)
                return false;

            // Fail DynamicProfilerModule types as they are defined via the Module Editor, i.e. they are not type-defined. Not an error.
            if (moduleType == typeof(DynamicProfilerModule))
                return false;

            // Fail non-abstract types that aren't attributed correctly. User error.
            moduleMetadata = Attribute.GetCustomAttribute(moduleType, typeof(ProfilerModuleMetadataAttribute)) as ProfilerModuleMetadataAttribute;
            if (moduleMetadata == null)
            {
                errorDescription = $"The module '{moduleType}' does not have a [{nameof(ProfilerModuleMetadataAttribute)}]. This is required in order for it to be displayed in the Profiler Window.";
                return false;
            }

            // Fail non-abstract, attributed types that haven't defined a valid name. User error.
            var moduleDisplayName = moduleMetadata.DisplayName;
            if (string.IsNullOrEmpty(moduleDisplayName))
            {
                errorDescription = $"The module '{moduleType}' does not provide a valid display name. This is required for it to be displayed in the Profiler Window.";
                return false;
            }

            return true;
        }
    }
}
