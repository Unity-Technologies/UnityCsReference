// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.IO.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Assemblies;

namespace Unity.Entities
{
    /// <summary>
    /// Installs what Entities needs from this module into Entities, which sits below it, and registers Entities'
    /// callbacks with the parts of this module that call back into it.
    /// </summary>
    static partial class CoreModuleLightShimHooks
    {
        [OnCodeLoaded]
        static void Install()
        {
            EntitiesCoreCallbacksBindings.Install();
            CoreModuleLightShim.InstallHooks(new Hooks());
            ResourcesAPIInternal.EntitiesAssetGC.RegisterAdditionalRootsHandler(UnityObjectRefUtility.RootsHandlerDelegate);
        }

        sealed class Hooks : CoreModuleLightShim.IHooks
        {
            public Type UnityObjectType => typeof(UnityEngine.Object);
            public Type ComponentType => typeof(Component);
            public Type UnityObjectRefGenericType => typeof(UnityObjectRef<>);
            public Type ExecuteInEditModeType => typeof(ExecuteInEditMode);
            public Type ExecuteAlwaysType => typeof(ExecuteAlways);

            public float FixedDeltaTime => Time.fixedDeltaTime;

            public unsafe bool ReadFile(string path, ReadCommand* commands, uint commandCount, AssetLoadingSubsystem subsystem)
            {
                var readHandle = AsyncReadManager.Read(path, commands, commandCount, subsystem: subsystem);
                readHandle.JobHandle.Complete();
                return readHandle.Status == ReadStatus.Complete;
            }

            public IReadOnlyList<Assembly> GetLoadedAssemblies() => CurrentAssemblies.GetLoadedAssemblies();

            public string GetLoadedAssemblyPath(Assembly assembly) => assembly.GetLoadedAssemblyPath();

            public object[] EntityIdsToObjects(NativeArray<EntityId> entityIds)
            {
                // Callers cast the result to UnityEngine.Object[], including when it is empty
                var objects = new List<UnityEngine.Object>(entityIds.Length);
                if (entityIds.Length > 0)
                    Resources.EntityIdsToObjectList(entityIds, objects);
                return objects.ToArray();
            }

            public object EntityIdToObject(EntityId entityId) => Resources.EntityIdToObject(entityId);

            public string GetObjectName(object unityObject) => ((UnityEngine.Object)unityObject).name;

            public bool IsUnityComponentType(Type type) => type.IsClass &&
                (type.IsSubclassOf(typeof(Component)) ||
                 type.IsSubclassOf(typeof(MonoBehaviour)) ||
                 type == typeof(Transform) ||
                 type == typeof(GameObject));

            public EntityId GetEntityId(object unityObject) => ((UnityEngine.Object)unityObject).GetEntityId();

            public void MarkInstanceIDsAsRoot(IntPtr instanceIDs, int count, IntPtr state) =>
                ResourcesAPIInternal.EntitiesAssetGC.MarkInstanceIDsAsRoot(instanceIDs, count, state);

            public int GetComponentTypeId(Type type) => TypeManagerV2.GetComponentTypeId(type.TypeHandle);

            public void ClearCompanionComponentTypes() => CompanionComponentTypeIndices.Clear();

            public void RegisterCompanionComponentTypeIfWrapper(Type type, TypeIndex typeIndex) =>
                CompanionComponentTypeIndices.RegisterIfWrapper(type, typeIndex);
        }
    }
}
