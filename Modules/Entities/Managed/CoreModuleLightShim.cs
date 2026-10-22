// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.IO.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace Unity.Entities
{
    /// <summary>
    /// What Entities needs from CoreModule. CoreModule depends on EntitiesModule, so Entities can't reference it
    /// directly; CoreModule installs the implementation at startup by calling <see cref="InstallHooks"/>.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.CoreModule")]
    static partial class CoreModuleLightShim
    {
        [VisibleToOtherModules("UnityEngine.CoreModule")]
        internal interface IHooks
        {
            // Types Entities tests against
            Type UnityObjectType { get; }
            Type ComponentType { get; }
            Type UnityObjectRefGenericType { get; }
            Type ExecuteInEditModeType { get; }
            Type ExecuteAlwaysType { get; }

            float FixedDeltaTime { get; }
            bool IsPlaying { get; }
            long ArchetypeAllocatorBudget { get; }
            long QueryAllocatorBudget { get; }

            IReadOnlyList<Assembly> GetLoadedAssemblies();
            string GetLoadedAssemblyPath(Assembly assembly);
            object[] EntityIdsToObjects(NativeArray<UnityEngine.EntityId> entityIds);
            object EntityIdToObject(UnityEngine.EntityId entityId);
            string GetObjectName(object unityObject);
            bool IsUnityComponentType(Type type);
            UnityEngine.EntityId GetEntityId(object unityObject);
            void MarkInstanceIDsAsRoot(IntPtr instanceIDs, int count, IntPtr state);

            // AsyncReadManager: reads the commands and waits for them; true when every command completed
            unsafe bool ReadFile(string path, ReadCommand* commands, uint commandCount, AssetLoadingSubsystem subsystem);

            // Component type ids, minted natively by TypeManagerV2
            int GetComponentTypeId(Type type);

            // Companion GameObjects
            void ClearCompanionComponentTypes();
            void RegisterCompanionComponentTypeIfWrapper(Type type, TypeIndex typeIndex);
        }

        [NoAutoStaticsCleanup] // CoreModule installs the hooks and outlives code reloads.
        static IHooks s_Hooks;

        [VisibleToOtherModules("UnityEngine.CoreModule")]
        internal static void InstallHooks(IHooks hooks) => s_Hooks = hooks;

        internal static IHooks Hooks => s_Hooks ?? throw new InvalidOperationException(
            "Entities needs CoreModule, but its hooks are not installed yet. CoreModule installs them when its code loads.");
    }
}
