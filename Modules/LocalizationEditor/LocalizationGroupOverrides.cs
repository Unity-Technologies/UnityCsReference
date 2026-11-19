// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    // A .po file is registered under the group of the assembly that owns it, and native derives that
    // group from the asmdef name alone. Only managed can read LocalizationAttribute, so the assemblies
    // that name their own group are pushed down here on every code load.
    static partial class LocalizationGroupOverrides
    {
        [OnCodeLoaded]
        internal static void PushGroupNameOverrides()
        {
            var assemblyNames = new List<string>();
            var groupNames = new List<string>();

            foreach (var assembly in EditorAssemblies.loadedAssemblies)
            {
                var attribute = assembly.GetCustomAttribute<LocalizationAttribute>();
                if (attribute?.locGroupName == null)
                    continue;

                assemblyNames.Add(assembly.GetName().Name);
                groupNames.Add(attribute.locGroupName);
            }

            LocalizationDatabase.SetGroupNameOverrides(assemblyNames.ToArray(), groupNames.ToArray());
        }
    }
}
