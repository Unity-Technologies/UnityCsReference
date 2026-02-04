// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.SceneManagement
{
    [NativeHeader("Runtime/Export/SceneManager/Scene.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Scene
    {
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static bool IsValidInternal(SceneHandle sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static string GetPathInternal(SceneHandle sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static void SetPathAndGUIDInternal(SceneHandle sceneHandle, string path, string guid);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static string GetNameInternal(SceneHandle sceneHandle);

        [NativeThrows]
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static void SetNameInternal(SceneHandle sceneHandle, string name);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static string GetGUIDInternal(SceneHandle sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static bool IsSubScene(SceneHandle sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static void SetIsSubScene(SceneHandle sceneHandle, bool value);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static bool GetIsLoadedInternal(SceneHandle sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static Scene.LoadingState GetLoadingStateInternal(SceneHandle sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static bool GetIsDirtyInternal(SceneHandle sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static int GetDirtyID(SceneHandle sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static int GetBuildIndexInternal(SceneHandle sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static int GetRootCountInternal(SceneHandle sceneHandle);

        [NativeMethod("GetRootGameObjectsInternal")]
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static void GetRootGameObjectsInternalList(SceneHandle sceneHandle, [Out] List<GameObject> resultRootList);

        [NativeMethod("GetRootGameObjectsInternal")]
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static void GetRootGameObjectsInternalArray(SceneHandle sceneHandle, [Out] GameObject[] resultRootArray);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static EntityId GetDefaultParent(SceneHandle sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static void SetDefaultParent(SceneHandle sceneHandle, EntityId value);
    }

    /// <summary>
    /// Handle to a scene. This is a wrapper around an EntityId.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [Serializable]
    [NativeHeader("Runtime/SceneManager/UnitySceneHandle.h")]
    [NativeClass("UnitySceneHandle")]
    public struct SceneHandle : IEquatable<SceneHandle>, IFormattable
    {
        internal EntityId m_Value;
        public static SceneHandle None => default;
        internal static SceneHandle From(EntityId entityId) => new() { m_Value = entityId };
        public override bool Equals(object obj) => obj is SceneHandle other && Equals(other);
        public bool Equals(SceneHandle other) => m_Value == other.m_Value;

        /// <summary>
        /// Test for equality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>True if the two SceneHandles are the same</returns>
        public static bool operator ==(SceneHandle left, SceneHandle right) => left.Equals(right);

        /// <summary>
        /// Test for inequality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>True if the two SceneHandles are different</returns>
        public static bool operator !=(SceneHandle left, SceneHandle right) => !left.Equals(right);

        /// <summary>
        /// Implicit conversion from <see cref="SceneHandle"/> to <see langword="int"/>.
        /// </summary>
        /// <param name="handle">The SceneHandle</param>
        [Obsolete("Implicit conversion from SceneHandle to int is deprecated. Use SceneHandle.GetRawData() instead")]
        public static implicit operator int(SceneHandle handle) => handle.m_Value;

        /// <summary>
        /// Implicit conversion from <see langword="int"/> to <see cref="SceneHandle"/>.
        /// </summary>
        /// <param name="handle"></param>
        [Obsolete("Implicit conversion from int to SceneHandle is deprecated. Use SceneHandle.FromRawData(ulong) instead")]
        public static implicit operator SceneHandle(int handle) => FromRawData((ulong)handle);

        /// <summary>
        /// Implicit conversion from <see cref="SceneHandle"/> to <see langword="uint"/>.
        /// </summary>
        /// <param name="handle">The SceneHandle</param>
        [Obsolete("Implicit conversion from SceneHandle to uint is deprecated. Use SceneHandle.GetRawData() instead")]
        public static implicit operator uint(SceneHandle handle) => (uint)(int)handle.m_Value;

        /// <summary>
        /// Implicit conversion from <see langword="uint"/> to <see cref="SceneHandle"/>.
        /// </summary>
        /// <param name="handle"></param>
        [Obsolete("Implicit conversion from uint to SceneHandle is deprecated. Use SceneHandle.FromRawData(ulong) instead")]
        public static implicit operator SceneHandle(uint handle) => FromRawData(handle);

        public override int GetHashCode() => m_Value.GetHashCode();

        public override string ToString() => m_Value.ToString();
        public string ToString(string format) => m_Value.ToString(format);

        public string ToString(string format, IFormatProvider formatProvider) => m_Value.ToString(format, formatProvider);

        internal EntityId ToEntityId() => m_Value;

        public ulong GetRawData() => m_Value.GetRawData();
        public static SceneHandle FromRawData(ulong rawdata) => new() { m_Value = EntityId.From(rawdata) };
    }

    internal static class SceneHandleExtensions
    {
        /// <summary>
        /// Convert an array of <see langword="int"/> to an array of <see cref="SceneHandle"/>.
        /// </summary>
        public static SceneHandle[] ToSceneHandleArray(this int[] integers) => (SceneHandleToIntArray)integers;

        /// <summary>
        /// Convert an array of <see cref="SceneHandle"/> to an array of <see langword="int"/>.
        /// </summary>
        public static int[] ToIntArray(this SceneHandle[] sceneHandles) => (SceneHandleToIntArray)sceneHandles;

        /// <summary>
        /// Convert an array of <see cref="EntityId"/> to an array of <see cref="SceneHandle"/>.
        /// </summary>
        public static SceneHandle[] ToSceneHandleArray(this EntityId[] entityIds) => (SceneHandleToEntityIdArray)entityIds;

        /// <summary>
        /// Convert an array of <see cref="SceneHandle"/> to an array of <see cref="EntityId"/>.
        /// </summary>
        public static EntityId[] ToEntityIdArray(this SceneHandle[] sceneHandles) => (SceneHandleToEntityIdArray)sceneHandles;

        /// <summary>
        /// Convert a list of <see langword="int"/> to a list of <see cref="SceneHandle"/>.
        /// </summary>
        public static List<SceneHandle> ToSceneHandleList(this List<int> integers) => (SceneHandleToIntList)integers;

        /// <summary>
        /// Convert a list of <see cref="SceneHandle"/> to a list of <see langword="int"/>.
        /// </summary>
        public static List<int> ToIntList(this List<SceneHandle> sceneHandles) => (SceneHandleToIntList)sceneHandles;

        /// <summary>
        /// Convert a list of <see cref="EntityId"/> to a list of <see cref="SceneHandle"/>.
        /// </summary>
        public static List<SceneHandle> ToSceneHandleList(this List<EntityId> entityIds) => (SceneHandleToEntityIdList)entityIds;

        /// <summary>
        /// Convert a list of <see cref="SceneHandle"/> to a list of <see cref="EntityId"/>.
        /// </summary>
        public static List<EntityId> ToEntityIdList(this List<SceneHandle> sceneHandles) => (SceneHandleToEntityIdList)sceneHandles;

        [StructLayout(LayoutKind.Explicit)]
        struct SceneHandleToIntArray // change casting mechanism to long when we have a 64 bit EntityId
        {
            [FieldOffset(0)] int[] _integers;
            [FieldOffset(0)] SceneHandle[] _sceneHandles;

            public static implicit operator SceneHandleToIntArray(int[] integers) => new() { _integers = integers };
            public static implicit operator SceneHandleToIntArray(SceneHandle[] sceneHandles) => new() { _sceneHandles = sceneHandles };
            public static implicit operator int[](SceneHandleToIntArray value) => value._integers;
            public static implicit operator SceneHandle[](SceneHandleToIntArray value) => value._sceneHandles;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct SceneHandleToEntityIdArray
        {
            [FieldOffset(0)] EntityId[] _entityIds;
            [FieldOffset(0)] SceneHandle[] _sceneHandles;

            public static implicit operator SceneHandleToEntityIdArray(EntityId[] entityIds) => new() { _entityIds = entityIds };
            public static implicit operator SceneHandleToEntityIdArray(SceneHandle[] sceneHandles) => new() { _sceneHandles = sceneHandles };
            public static implicit operator EntityId[](SceneHandleToEntityIdArray value) => value._entityIds;
            public static implicit operator SceneHandle[](SceneHandleToEntityIdArray value) => value._sceneHandles;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct SceneHandleToIntList // change casting mechanism to long when we have a 64 bit EntityId
        {
            [FieldOffset(0)] List<int> _integers;
            [FieldOffset(0)] List<SceneHandle> _sceneHandles;

            public static implicit operator SceneHandleToIntList(List<int> integers) => new() { _integers = integers };
            public static implicit operator SceneHandleToIntList(List<SceneHandle> sceneHandles) => new() { _sceneHandles = sceneHandles };
            public static implicit operator List<int>(SceneHandleToIntList value) => value._integers;
            public static implicit operator List<SceneHandle>(SceneHandleToIntList value) => value._sceneHandles;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct SceneHandleToEntityIdList
        {
            [FieldOffset(0)] List<EntityId> _entityIds;
            [FieldOffset(0)] List<SceneHandle> _sceneHandles;

            public static implicit operator SceneHandleToEntityIdList(List<EntityId> entityIds) => new() { _entityIds = entityIds };
            public static implicit operator SceneHandleToEntityIdList(List<SceneHandle> sceneHandles) => new() { _sceneHandles = sceneHandles };
            public static implicit operator List<EntityId>(SceneHandleToEntityIdList value) => value._entityIds;
            public static implicit operator List<SceneHandle>(SceneHandleToEntityIdList value) => value._sceneHandles;
        }
    }
}
