// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Unity.DataModel;
using Unity.EntitiesLike;
using UnityEngine;
using Debug = UnityEngine.Debug;

internal sealed partial class SchemaReload
{
    public static void AfterTypeManagerIsInitialized()
    {

        // Set up the UdmInterop instance for the Editor here
        UdmInterop.Instance = new UnityUdmInterop();

        TypeTraitsRegistry.Clear();
        UdmManager.CreateBasicUdmTypeData();
        RegenerateSchemas();
    }

    public static void RegenerateSchemas()
    {
        try
        {
            // Note: ALC related code, using AppDomain for now
            // var assemblies = CurrentAssemblies.GetLoadedAssemblies();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assemblyCount = GetCount(assemblies);
            var assemblyFiles = new List<string>(assemblyCount);
            var references = new List<string>(assemblyCount);


            foreach (var assembly in assemblies)
            {
                if (assembly.IsDynamic)
                    continue;

                //var location = assembly.GetLoadedAssemblyPath();
                var location = assembly.Location;
                if (string.IsNullOrEmpty(location))
                    continue;

                var splitLocation = new HashSet<string>(location.Split(Path.DirectorySeparatorChar));
                if (splitLocation.Contains("MonoBleedingEdge"))
                {
                    references.Add(location);
                    continue;
                }

                assemblyFiles.Add(location);
            }

            RttiResolver.ReloadSchemas();
            foreach (var type in TypeManager.GetUdmRTTITypes())
            {
                RttiResolver.GetOrAddRTTI(type);
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Encountered exception: Message: {e.Message}; Stack trace: {e.StackTrace.Replace(Environment.NewLine, "--||--")}.");
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int GetCount<T>(IEnumerable<T> values)
    {
        if (values is IReadOnlyCollection<T> valueCollection)
            return valueCollection.Count;

        var list = 0;
        foreach (var _ in values)
            list++;
        return list;
    }
}
