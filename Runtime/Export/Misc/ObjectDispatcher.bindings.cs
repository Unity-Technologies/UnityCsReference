// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using System.Collections.Generic;

[assembly: InternalsVisibleTo("Unity.RenderPipelines.GPUDriven.Runtime")]
[assembly: InternalsVisibleTo("Unity.ObjectDispatcher.Tests")]

namespace UnityEngine
{
    using TypeDispatchAction = Action<Object[], IntPtr, IntPtr, int, int, Action<TypeDispatchData>>;
    using TransformDispatchAction = Action<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, Action<TransformDispatchData>>;

    internal struct TypeDispatchData : IDisposable
    {
        public Object[] changed;
        public NativeArray<int> changedID;
        public NativeArray<int> destroyedID;

        public void Dispose()
        {
            changed = null;
            changedID.Dispose();
            destroyedID.Dispose();
        }
    }

    internal struct TransformDispatchData : IDisposable
    {
        public NativeArray<int> transformedID;
        public NativeArray<int> parentID;
        public NativeArray<Matrix4x4> localToWorldMatrices;
        public NativeArray<Vector3> positions;
        public NativeArray<Quaternion> rotations;
        public NativeArray<Vector3> scales;

        public void Dispose()
        {
            transformedID.Dispose();
            parentID.Dispose();
            localToWorldMatrices.Dispose();
            positions.Dispose();
            rotations.Dispose();
            scales.Dispose();
        }
    }

    [RequiredByNativeCode]
    [NativeHeader("Runtime/Misc/ObjectDispatcher.h")]
    [StaticAccessor("GetObjectDispatcher()", StaticAccessorType.Dot)]
    internal sealed class ObjectDispatcher : IDisposable
    {
        public enum TransformTrackingType
        {
            GlobalTRS,
            LocalTRS,
            Hierarchy
        } 

        [Flags]
        public enum TypeTrackingFlags
        {
            // All the objects that are instantiated in the scene.
            // For example: GameObjects, Components or dynamically created Meshes or Materials. 
            SceneObjects = 1,
            // All the persistent objects that are either assets or resources.
            // For example: Mesh or Material assets references by MeshRenderer or MeshFilter components.
            // Or a resource object loaded through Resources.Load method.
            Assets = 2,
            // All the objects that are used by Editor internally.
            // For example: preview scene objects.
            EditorOnlyObjects = 4,

            Default = SceneObjects | Assets,
            All = SceneObjects | Assets | EditorOnlyObjects
        }

        private IntPtr m_Ptr = IntPtr.Zero;

        public bool valid { get { return m_Ptr != IntPtr.Zero; } }

        public int maxDispatchHistoryFramesCount
        {
            get
            {
                ValidateSystemHandleAndThrow();

                return GetMaxDispatchHistoryFramesCount(m_Ptr);
            }
            set
            {
                ValidateSystemHandleAndThrow();

                SetMaxDispatchHistoryFramesCount(m_Ptr, value);
            }
        }

        public ObjectDispatcher()
        {
            m_Ptr = CreateDispatchSystemHandle();
        }

        ~ObjectDispatcher()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (m_Ptr != IntPtr.Zero)
            {
                DestroyDispatchSystemHandle(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        private void ValidateSystemHandleAndThrow()
        {
            if (!valid)
                throw new Exception("The ObjectDispatcher is invalid or has been disposed.");
        }

        private void ValidateTypeAndThrow(Type type)
        {
            if (!type.IsSubclassOf(typeof(Object)))
                throw new Exception("Only types inherited from UnityEngine.Object are supported.");
        }

        private void ValidateComponentTypeAndThrow(Type type)
        {
            if (!type.IsSubclassOf(typeof(Component)))
                throw new Exception("Only types inherited from UnityEngine.Component are supported.");
        }

        private static TypeDispatchAction s_TypeDispatch = (Object[] changed, IntPtr changedID, IntPtr destroyedID, int changedCount, int destroyedCount, Action<TypeDispatchData> callback) =>
        {
            unsafe
            {
                NativeArray<int> changedIDArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(changedID.ToPointer(), changedCount, Allocator.Invalid);
                NativeArray<int> destroyedIDArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(destroyedID.ToPointer(), destroyedCount, Allocator.Invalid);

                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref changedIDArray, AtomicSafetyHandle.Create());
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref destroyedIDArray, AtomicSafetyHandle.Create());
                var dispatchData = new TypeDispatchData()
                {
                    changed = changed,
                    changedID = changedIDArray,
                    destroyedID = destroyedIDArray
                };

                callback(dispatchData);

                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(changedIDArray));
                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(destroyedIDArray));
            }
        };

        private static TransformDispatchAction s_TransformDispatch = (
            IntPtr transformed,
            IntPtr parents,
            IntPtr localToWorldMatrices,
            IntPtr positions,
            IntPtr rotations,
            IntPtr scales,
            int count,
            Action<TransformDispatchData> callback) =>
        {
            unsafe
            {
                NativeArray<int> transformedArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(transformed.ToPointer(), count, Allocator.Invalid);
                NativeArray<int> parentArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(parents.ToPointer(), parents != IntPtr.Zero ? count : 0, Allocator.Invalid);
                NativeArray<Matrix4x4> localToWorldMatricesArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Matrix4x4>(localToWorldMatrices.ToPointer(), localToWorldMatrices != IntPtr.Zero ? count : 0, Allocator.Invalid);
                NativeArray<Vector3> positionsArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3>(positions.ToPointer(), positions != IntPtr.Zero ? count : 0, Allocator.Invalid);
                NativeArray<Quaternion> rotationsArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Quaternion>(rotations.ToPointer(), rotations != IntPtr.Zero ? count : 0, Allocator.Invalid);
                NativeArray<Vector3> scalesArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3>(scales.ToPointer(), scales != IntPtr.Zero ? count : 0, Allocator.Invalid);

                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref transformedArray, AtomicSafetyHandle.Create());
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref parentArray, AtomicSafetyHandle.Create());
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref localToWorldMatricesArray, AtomicSafetyHandle.Create());
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref positionsArray, AtomicSafetyHandle.Create());
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref rotationsArray, AtomicSafetyHandle.Create());
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref scalesArray, AtomicSafetyHandle.Create());
                var dispatchData = new TransformDispatchData()
                {
                    transformedID = transformedArray,
                    parentID = parentArray,
                    localToWorldMatrices = localToWorldMatricesArray,
                    positions = positionsArray,
                    rotations = rotationsArray,
                    scales = scalesArray,
                };

                callback(dispatchData);

                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(transformedArray));
                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(parentArray));
                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(localToWorldMatricesArray));
                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(positionsArray));
                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(rotationsArray));
                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(scalesArray));
            }
        };

        public void DispatchTypeChangesAndClear(Type type, Action<TypeDispatchData> callback, bool sortByInstanceID = false, bool noScriptingArray = false)
        {
            ValidateSystemHandleAndThrow();
            ValidateTypeAndThrow(type);
            DispatchTypeChangesAndClear(m_Ptr, type, s_TypeDispatch, sortByInstanceID, noScriptingArray, callback);
        }

        public void DispatchTransformChangesAndClear(Type type, TransformTrackingType trackingType, Action<Component[]> callback, bool sortByInstanceID = false)
        {
            ValidateSystemHandleAndThrow();
            ValidateComponentTypeAndThrow(type);
            DispatchTransformChangesAndClear(m_Ptr, type, trackingType, callback, sortByInstanceID);
        }

        public void DispatchTransformChangesAndClear(Type type, TransformTrackingType trackingType, Action<TransformDispatchData> callback)
        {
            ValidateSystemHandleAndThrow();
            ValidateComponentTypeAndThrow(type);
            DispatchTransformDataChangesAndClear(m_Ptr, type, trackingType, s_TransformDispatch, callback);
        }

        public void ClearTypeChanges(Type type)
        {
            ValidateSystemHandleAndThrow();
            ValidateTypeAndThrow(type);
            DispatchTypeChangesAndClear(m_Ptr, type, null, false, false, null);
        }

        public TypeDispatchData GetTypeChangesAndClear(Type type, Allocator allocator, bool sortByInstanceID = false, bool noScriptingArray = false)
        {
            var dispatchData = new TypeDispatchData();

            DispatchTypeChangesAndClear(type, (TypeDispatchData data) =>
            {
                dispatchData.changed = data.changed;
                dispatchData.changedID = new NativeArray<int>(data.changedID, allocator);
                dispatchData.destroyedID = new NativeArray<int>(data.destroyedID, allocator);
            }, sortByInstanceID, noScriptingArray);

            return dispatchData;
        }

        public void GetTypeChangesAndClear(Type type, List<Object> changed, out NativeArray<int> changedID, out NativeArray<int> destroyedID, Allocator allocator, bool sortByInstanceID = false)
        {
            var dispatchData = new TypeDispatchData();

            DispatchTypeChangesAndClear(type, (TypeDispatchData data) =>
            {
                dispatchData.changedID = new NativeArray<int>(data.changedID, allocator);
                dispatchData.destroyedID = new NativeArray<int>(data.destroyedID, allocator);
            }, sortByInstanceID, true);

            changedID = dispatchData.changedID;
            destroyedID = dispatchData.destroyedID;
            Resources.InstanceIDToObjectList(dispatchData.changedID, changed);
        }

        public Component[] GetTransformChangesAndClear(Type type, TransformTrackingType trackingType, bool sortByInstanceID = false)
        {
            Component[] dispatchData = null;

            DispatchTransformChangesAndClear(type, trackingType, (Component[] instances) =>
            {
                dispatchData = instances;
            }, sortByInstanceID);

            return dispatchData;
        }

        public TransformDispatchData GetTransformChangesAndClear(Type type, TransformTrackingType trackingType, Allocator allocator)
        {
            var dispatchData = new TransformDispatchData();

            DispatchTransformChangesAndClear(type, trackingType, (TransformDispatchData data) =>
            {
                dispatchData.transformedID = new NativeArray<int>(data.transformedID, allocator);
                dispatchData.parentID = new NativeArray<int>(data.parentID, allocator);
                dispatchData.localToWorldMatrices = new NativeArray<Matrix4x4>(data.localToWorldMatrices, allocator);
                dispatchData.positions = new NativeArray<Vector3>(data.positions, allocator);
                dispatchData.rotations = new NativeArray<Quaternion>(data.rotations, allocator);
                dispatchData.scales = new NativeArray<Vector3>(data.scales, allocator);
            });

            return dispatchData;
        }

        public void EnableTypeTracking(TypeTrackingFlags typeTrackingMask, params Type[] types)
        {
            ValidateSystemHandleAndThrow();

            foreach (Type type in types)
            {
                ValidateTypeAndThrow(type);
                EnableTypeTracking(m_Ptr, type, typeTrackingMask);
            }
        }

        public void EnableTypeTracking(params Type[] types)
        {
            EnableTypeTracking(TypeTrackingFlags.Default, types);
        }

        [Obsolete("EnableTypeTrackingIncludingAssets is deprecated, please use EnableTypeTracking and provide the flag that specifies whether you need assets or not.", false)]
        public void EnableTypeTrackingIncludingAssets(params Type[] types)
        {
            EnableTypeTracking(TypeTrackingFlags.SceneObjects | TypeTrackingFlags.Assets, types);
        }

        public void DisableTypeTracking(params Type[] types)
        {
            ValidateSystemHandleAndThrow();

            foreach (Type type in types)
            {
                ValidateTypeAndThrow(type);
                DisableTypeTracking(m_Ptr, type);
            }
        }

        public void EnableTransformTracking(TransformTrackingType trackingType, params Type[] types)
        {
            ValidateSystemHandleAndThrow();

            foreach (Type type in types)
            {
                ValidateComponentTypeAndThrow(type);
                EnableTransformTracking(m_Ptr, type, trackingType);
            }
        }

        public void DisableTransformTracking(TransformTrackingType trackingType, params Type[] types)
        {
            ValidateSystemHandleAndThrow();

            foreach (Type type in types)
            {
                ValidateComponentTypeAndThrow(type);
                DisableTransformTracking(m_Ptr, type, trackingType);
            }
        }

        public void DispatchTypeChangesAndClear<T>(Action<TypeDispatchData> callback, bool sortByInstanceID = false, bool noScriptingArray = false) where T : Object
        {
            DispatchTypeChangesAndClear(typeof(T), callback, sortByInstanceID, noScriptingArray);
        }

        public void DispatchTransformChangesAndClear<T>(TransformTrackingType trackingType, Action<Component[]> callback, bool sortByInstanceID = false) where T : Object
        {
            DispatchTransformChangesAndClear(typeof(T), trackingType, callback, sortByInstanceID);
        }

        public void DispatchTransformChangesAndClear<T>(TransformTrackingType trackingType, Action<TransformDispatchData> callback) where T : Object
        {
            DispatchTransformChangesAndClear(typeof(T), trackingType, callback);
        }

        public void ClearTypeChanges<T>() where T : Object
        {
            ClearTypeChanges(typeof(T));
        }

        public TypeDispatchData GetTypeChangesAndClear<T>(Allocator allocator, bool sortByInstanceID = false, bool noScriptingArray = false) where T : Object
        {
            return GetTypeChangesAndClear(typeof(T), allocator, sortByInstanceID, noScriptingArray);
        }

        public void GetTypeChangesAndClear<T>(List<Object> changed, out NativeArray<int> changedID, out NativeArray<int> destroyedID, Allocator allocator, bool sortByInstanceID = false) where T : Object
        {
            GetTypeChangesAndClear(typeof(T), changed, out changedID, out destroyedID, allocator, sortByInstanceID);
        }

        public Component[] GetTransformChangesAndClear<T>(TransformTrackingType trackingType, bool sortByInstanceID = false) where T : Object
        {
            return GetTransformChangesAndClear(typeof(T), trackingType, sortByInstanceID);
        }

        public TransformDispatchData GetTransformChangesAndClear<T>(TransformTrackingType trackingType, Allocator allocator) where T : Object
        {
            return GetTransformChangesAndClear(typeof(T), trackingType, allocator);
        }

        public void EnableTypeTracking<T>(TypeTrackingFlags typeTrackingMask = TypeTrackingFlags.Default) where T : Object
        {
            EnableTypeTracking(typeTrackingMask, typeof(T));
        }

        public void DisableTypeTracking<T>() where T : Object
        {
            DisableTypeTracking(typeof(T));
        }

        public void EnableTransformTracking<T>(TransformTrackingType trackingType) where T : Object
        {
            EnableTransformTracking(trackingType, typeof(T));
        }

        public void DisableTransformTracking<T>(TransformTrackingType trackingType) where T : Object
        {
            DisableTransformTracking(trackingType, typeof(T));
        }

        private static extern IntPtr CreateDispatchSystemHandle();

        [ThreadSafe]
        private static extern void DestroyDispatchSystemHandle(IntPtr ptr);

        private static extern int GetMaxDispatchHistoryFramesCount(IntPtr ptr);

        private static extern void SetMaxDispatchHistoryFramesCount(IntPtr ptr, int count);

        private extern static void EnableTypeTracking(IntPtr ptr, Type type, TypeTrackingFlags typeTrackingMask);

        private extern static void DisableTypeTracking(IntPtr ptr, Type type);

        private extern static void EnableTransformTracking(IntPtr ptr, Type type, TransformTrackingType trackingType);

        private extern static void DisableTransformTracking(IntPtr ptr, Type type, TransformTrackingType trackingType);

        private extern static void DispatchTypeChangesAndClear(IntPtr ptr, Type type, TypeDispatchAction callback, bool sortByInstanceID, bool noScriptingArray, Action<TypeDispatchData> param);

        private extern static void DispatchTransformDataChangesAndClear(IntPtr ptr, Type type, TransformTrackingType trackingType, TransformDispatchAction callback, Action<TransformDispatchData> param);

        private extern static void DispatchTransformChangesAndClear(IntPtr ptr, Type type, TransformTrackingType trackingType, Action<Component[]> callback, bool sortByInstanceID);
    }
}
