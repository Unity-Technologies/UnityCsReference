// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngineInternal;
using uei = UnityEngine.Internal;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Threading;
using NotNullWhenAttribute = System.Diagnostics.CodeAnalysis.NotNullWhenAttribute;
using MaybeNullWhenAttribute = System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{
    // Bit mask that controls object destruction and visibility in inspectors
    [Flags]
    public enum HideFlags
    {
        // A normal, visible object. This is the default.
        None = 0,

        // The object will not appear in the hierarchy and will not show up in the project view if it is stored in an asset.
        HideInHierarchy = 1,

        // It is not possible to view it in the inspector
        HideInInspector = 2,

        // The object will not be saved to the scene.
        DontSaveInEditor = 4,

        // The object is not be editable in the inspector
        NotEditable = 8,

        // The object will not be saved when building a player
        DontSaveInBuild = 16,

        // The object will not be unloaded by UnloadUnusedAssets
        DontUnloadUnusedAsset = 32,

        DontSave = DontSaveInEditor | DontSaveInBuild | DontUnloadUnusedAsset,

        // A combination of not shown in the hierarchy and not saved to to scenes.
        HideAndDontSave = HideInHierarchy | DontSaveInEditor | NotEditable | DontSaveInBuild | DontUnloadUnusedAsset
    }

    // Must match Scripting::FindObjectsSortMode
    [Obsolete("FindObjectsSortMode has been deprecated. Use the FindObjectsByType overloads that do not take a FindObjectsSortMode parameter.", false)]
    public enum FindObjectsSortMode
    {
        None = 0,
        InstanceID = 1
    }

    // Must match Scripting::FindObjectsInactive
    public enum FindObjectsInactive
    {
        Exclude = 0,
        Include = 1
    }

    public struct InstantiateParameters
    {
        public Transform parent;
        public Scene scene;
        public bool worldSpace;
        public bool originalImmutable;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    [Serializable]
    [Obsolete("Obsolete - Please use EntityId instead.", true)]
    public struct InstanceID : IEquatable<InstanceID>, IComparable<InstanceID>, IFormattable
    {
        [SerializeField]
        int m_index;

        [SerializeField]
        int m_version;

        public static InstanceID None => default;
        public override bool Equals(object obj) => obj is InstanceID other && Equals(other);
        public bool Equals(InstanceID other) => m_index == other.m_index;
        public int CompareTo(InstanceID other) => m_index.CompareTo(other.m_index);
        public static bool operator ==(InstanceID left, InstanceID right) => left.Equals(right);
        public static bool operator !=(InstanceID left, InstanceID right) => !left.Equals(right);

        public static bool operator <(InstanceID left, InstanceID right)  => left.m_index < right.m_index;
        public static bool operator >(InstanceID left, InstanceID right)  => left.m_index > right.m_index;
        public static bool operator <=(InstanceID left, InstanceID right) => left.m_index <= right.m_index;
        public static bool operator >=(InstanceID left, InstanceID right)  => left.m_index >= right.m_index;

        public override int GetHashCode()
        {
            // We only want the lower bits, which is the Index
            uint a = (uint)m_index;

            // Same Int hash as in the engine
            a = (a + 0x7ed55d16) + (a << 12);
            a = (a ^ 0xc761c23c) ^ (a >> 19);
            a = (a + 0x165667b1) + (a << 5);
            a = (a + 0xd3a2646c) ^ (a << 9);
            a = (a + 0xfd7046c5) + (a << 3);
            a = (a ^ 0xb55a4f09) ^ (a >> 16);

            return (int)a;
        }

        public bool IsValid()
        {
            return this != InstanceID.None;
        }

        public bool Equals(int other) => m_index == (int)other;

        public static implicit operator int(InstanceID entityId) => entityId.m_index;
        public static implicit operator InstanceID(int intValue) => new InstanceID {m_index = intValue};

        public static implicit operator EntityId(InstanceID entityId) => (int)entityId;
        public static implicit operator InstanceID(EntityId entityId) => new InstanceID {m_index = (int)entityId};

        public override string ToString() => m_index.ToString();
        public string ToString(string format) => m_index.ToString(format);
        public string ToString(string format, IFormatProvider formatProvider) => m_index.ToString(format, formatProvider);
    }

    class AssetGCFilterTypeAttribute : Attribute
    {
        public AssetGCFilterTypeAttribute() {}
    }

    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode(GenerateProxy = false)]
    [NativeHeader("Runtime/Export/Scripting/UnityEngineObject.bindings.h")]
    [NativeHeader("Runtime/GameCode/CloneObject.h")]
    [NativeHeader("Runtime/SceneManager/SceneManager.h")]
    [AssetGCFilterType]
    public partial class Object
    {

#pragma warning disable 649
        // The only link from a managed UnityEngine.Object to its native object. The native pointer is
        // resolved on demand through the EntityIdStore (lock-free, validates the id version), so a
        // destroyed or recycled id resolves to null without native ever writing back into this object.
        private EntityId m_EntityId;

#pragma warning disable 169
        // Set by native on "fake null" wrappers: managed objects created for a reference that could not be
        // bound to a native object (unassigned, missing, or type-mismatched). Never set on a bound wrapper.
        private string m_UnityRuntimeErrorString;
#pragma warning restore 169
#pragma warning restore 649

        const string objectIsNullMessage = "The Object you want to instantiate is null.";
        const string cloneDestroyedMessage = "Instantiate failed because the clone was destroyed during creation. This can happen if DestroyImmediate is called in MonoBehaviour.Awake.";

        // Explicit parameterless ctor: it preserves the (previously implicit) parameterless ctor that
        // every Object subclass chains to via `: base()`. Declaring the EntityId ctor below would
        // otherwise suppress the compiler-generated one.
        public Object() {}

        // Wrapper-construction ctor used by the TypeManagerV2 wrapper factories: binds this managed
        // wrapper to the already-existing native object identified by `id`. The EntityId is the whole
        // link; the native pointer is resolved through the EntityIdStore on every use. Each [NativeClass]
        // Object subclass declares `internal T(global::UnityEngine.EntityId id) : base(id) {}` (enforced by the generator).
        protected internal Object(EntityId id)
        {
            m_EntityId = id;
        }

        [System.Security.SecuritySafeCritical]
        public EntityId GetEntityId()
        {
            return UntagReleasedWrapperId(m_EntityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EntityId GetEntityIdForSerializationUnchecked()
        {
            return UntagReleasedWrapperId(m_EntityId);
        }

        // Released wrappers store their id with the lowest version bit cleared; keep in sync with Scripting::TagReleasedWrapperEntityId
        const ulong k_ReleasedWrapperEntityIdBit = 1UL << 40;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsReleasedWrapperId(EntityId storedId)
        {
            return storedId != EntityId.None && (EntityId.ToULong(storedId) & k_ReleasedWrapperEntityIdBit) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static EntityId TagReleasedWrapperId(EntityId entityId)
        {
            return EntityId.FromULong(EntityId.ToULong(entityId) & ~k_ReleasedWrapperEntityIdBit);
        }

        // Setting the bit is a no-op on a live id, so only None needs excluding
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static EntityId UntagReleasedWrapperId(EntityId storedId)
        {
            return storedId == EntityId.None ? storedId : EntityId.FromULong(EntityId.ToULong(storedId) | k_ReleasedWrapperEntityIdBit);
        }

        // A released wrapper only reaches a published object; a tagged id never resolves through the raw lookup
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe void* GetNativeObjectForWrapper(EntityId storedId)
        {
            void* nativeObject = EntityIdStore.GetRawNativeObject(storedId);
            if (nativeObject == null && IsReleasedWrapperId(storedId))
                nativeObject = EntityIdStore.GetNativeObject(UntagReleasedWrapperId(storedId));
            return nativeObject;
        }

        // Called by constructors of UnityEngine.Object subclasses with the EntityId returned by their native
        // constructor binding. Links the two objects in both directions: this object stores the id, and a strong
        // GCHandle to this object is written into the native object's GCHandle slot, so native code and later
        // managed lookups resolve the native object to this instance.
        // EntityId.None means native did not create an object (it raised an exception, or the caller reports the
        // failure); this object then stays null.
        [VisibleToOtherModules]
        internal unsafe void SetEntityIdFromConstructor(EntityId entityId)
        {
            m_EntityId = entityId;
            if (entityId == EntityId.None)
                return;

            void* nativeObject = EntityIdStore.GetNativeObject(entityId);
            if (nativeObject == null)
                throw new InvalidOperationException($"{GetType().FullName}: the native constructor returned EntityId {entityId} but no object is registered for it.");

            if (!MarshalledUnityObject.TryPublishWrapper((IntPtr)nativeObject, this))
                throw new InvalidOperationException($"{GetType().FullName}: the native constructor created a managed wrapper for the new object before returning. A constructor binding must not hand the object to managed code.");
        }

        // True when a native object exists for this wrapper (published, loading or in MainThreadCleanup). A lock-free
        // store read: no icall, no main-thread requirement, so it is usable from worker threads (serialization).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe bool HasLiveNativeObject()
        {
            return GetNativeObjectForWrapper(m_EntityId) != null;
        }

        [Obsolete("Calling MemberwiseClone on a UnityEngine.Object will result in a corrupt object, use Instantiate or InstantiateAsync instead.", true)]
        new protected object MemberwiseClone()
        {
            throw new NotImplementedException();
        }

        [Obsolete("Use GetEntityId instead.", true)]
        [System.Security.SecuritySafeCritical]
        public unsafe int GetInstanceID() => (int)(GetEntityId().GetRawData() & 0x00000000FFFFFFFF);

        public override int GetHashCode()
        {
            //in the editor, we store the m_EntityId in the c# objects. It's actually possible to have multiple c# objects
            //pointing to the same c++ object in some edge cases, and in those cases we'd like GetHashCode() and Equals() to treat
            //these objects as equals.
            return GetEntityId().GetHashCode();
        }

        public override bool Equals(object other)
        {
            Object otherAsObject = other as Object;
            // A UnityEngine.Object can only be equal to another UnityEngine.Object - or null if it has been destroyed.
            // Make sure other is a UnityEngine.Object if "as Object" fails. The explicit "is" check is required since the == operator
            // in this class treats destroyed objects as equal to null
            if (otherAsObject == null && other != null && !(other is Object))
                return false;

            return CompareBaseObjects(this, otherAsObject);
        }

        // Does the object exist?
        public static implicit operator bool([NotNullWhen(true)] [MaybeNullWhen(false)] Object exists)
        {
            return !CompareBaseObjects(exists, null);
        }

        static bool CompareBaseObjects(UnityEngine.Object lhs, UnityEngine.Object rhs)
        {
            bool lhsNull = ((object)lhs) == null;
            bool rhsNull = ((object)rhs) == null;

            if (rhsNull && lhsNull) return true;

            if (rhsNull) return !IsNativeObjectAlive(lhs);
            if (lhsNull) return !IsNativeObjectAlive(rhs);

            return lhs.GetEntityId() == rhs.GetEntityId();
        }

        static unsafe bool IsNativeObjectAlive(UnityEngine.Object o)
        {
            void* nativeObject = GetNativeObjectForWrapper(o.m_EntityId);
            if (nativeObject != null)
                return MarshalledUnityObject.IsInstanceOf(nativeObject, o);

            // A MonoBehaviour/ScriptableObject reloaded from the PersistentManager gets a new C# instance, so this
            // one would not be it.
            if (o is MonoBehaviour || o is ScriptableObject)
                return false;

            //Ressurection of assets: if you have a c# wrapper for an asset like a material, and the material
            //gets moved, or deleted, and later placed back, the persistentmanager will ensure it comes back
            //with the same EntityId, and the old c# wrapper works again. If the object is loaded again, the
            //EntityIdStore lookup above already resolves it; here we additionally treat an object that is not
            //loaded but still available in the persistent manager as alive (accessing it loads it). Only the
            //editor does this last step: it is an icall, and only the editor moves assets around.
            return DoesObjectWithInstanceIDExist(o.GetEntityId());
        }

        [RequiredByNativeCode]
        static void GetEntityIdFromNative(System.Object obj, out EntityId v)
        {
            // TODO: change parameter to UnityEngine.Object once additional proxy work is done to allow passing GC handles directly
            var self = UnsafeUtility.As<System.Object, UnityEngine.Object>(ref obj);
            v = self.m_EntityId;
        }

        [RequiredByNativeCode]
        static void SetEntityIdFromNative(System.Object obj, EntityId v)
        {
            var self = UnsafeUtility.As<System.Object, UnityEngine.Object>(ref obj);
            self.m_EntityId = v;
        }

        // Called by native when a native object gets this managed object as its wrapper
        // (Scripting::ConnectScriptingWrapperToObject).
        [RequiredByNativeCode]
        static void BindNativeObject(System.Object obj, EntityId v)
        {
            var unityObj = UnsafeUtility.As<System.Object, UnityEngine.Object>(ref obj);
            unityObj.m_EntityId = v;
            unityObj.m_UnityRuntimeErrorString = null;
        }

        [RequiredByNativeCode]
        static void SetUnityRuntimeErrorStringFromNative(System.Object obj, string errorString)
        {
            var self = UnsafeUtility.As<System.Object, UnityEngine.Object>(ref obj);
            self.m_UnityRuntimeErrorString = errorString;
        }

        // UUM-143556: lets the native game-release writer read back the marker the reference
        // deserializer stamps on a type-mismatched reference, so it can drop it (write fileID 0).
        internal string GetUnityRuntimeErrorString() { return m_UnityRuntimeErrorString; }

        [RequiredByNativeCode]
        static string GetUnityRuntimeErrorStringFromNative(System.Object obj)
        {
            var self = UnsafeUtility.As<System.Object, UnityEngine.Object>(ref obj);
            return self.GetUnityRuntimeErrorString();
        }

        // The name of the object.
        public string name
        {
            get { return GetName(); }
            set { SetName(value); }
        }

        // Clones the object /original/ and returns the clone.
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, new InstantiateParameters{ worldSpace = true });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, Transform parent) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, new InstantiateParameters{ worldSpace = true, parent = parent });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, Vector3 position, Quaternion rotation) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, position, rotation, new InstantiateParameters{ worldSpace = true });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, Transform parent, Vector3 position, Quaternion rotation) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, position, rotation, new InstantiateParameters{ worldSpace = true, parent = parent });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, new InstantiateParameters{ worldSpace = true });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Transform parent) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, new InstantiateParameters{ worldSpace = true, parent = parent });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Vector3 position, Quaternion rotation) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, position, rotation, new InstantiateParameters{ worldSpace = true });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, ReadOnlySpan<Vector3> positions, ReadOnlySpan<Quaternion> rotations) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, positions, rotations, new InstantiateParameters{ worldSpace = true });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Transform parent, Vector3 position, Quaternion rotation) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, position, rotation, new InstantiateParameters{ worldSpace = true, parent = parent });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Transform parent, Vector3 position, Quaternion rotation, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, position, rotation, new InstantiateParameters{ worldSpace = true, parent = parent }, cancellationToken);
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Transform parent, ReadOnlySpan<Vector3> positions, ReadOnlySpan<Quaternion> rotations) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, positions, rotations, new InstantiateParameters{ worldSpace = true, parent = parent });
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Transform parent, ReadOnlySpan<Vector3> positions, ReadOnlySpan<Quaternion> rotations, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, positions, rotations, new InstantiateParameters{ worldSpace = true, parent = parent }, cancellationToken);
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, InstantiateParameters parameters, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, 1, parameters, cancellationToken);
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, InstantiateParameters parameters, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, count, ReadOnlySpan<Vector3>.Empty,  ReadOnlySpan<Quaternion>.Empty, parameters, cancellationToken);
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, Vector3 position, Quaternion rotation, InstantiateParameters parameters, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            return InstantiateAsync(original, 1, position, rotation, parameters, cancellationToken);
        }

        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Vector3 position, Quaternion rotation, InstantiateParameters parameters, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            unsafe
            {
                return InstantiateAsync(original, count, new ReadOnlySpan<Vector3>(&position, 1),  new ReadOnlySpan<Quaternion>(&rotation, 1), parameters, cancellationToken);
            }
        }

        // Use the value directly to support netstandard
        // MethodImplOptions.AggressiveInlining = 256
        // MethodImplOptions.AggressiveOptimization = 512
        [MethodImpl(256 | 512)]
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, ReadOnlySpan<Vector3> positions, ReadOnlySpan<Quaternion> rotations, InstantiateParameters parameters, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            CheckNullArgument(original, objectIsNullMessage);

            if (count <= 0)
            {
                throw new ArgumentException("Cannot call instantiate multiple with count less or equal to zero");
            }

                if (original is ScriptableObject)
                    throw new ArgumentException("Cannot call instantiate multiple for a ScriptableObject");

            unsafe
            {
                fixed(Vector3* positionsPtr = positions)
                fixed(Quaternion* rotationsPtr = rotations)
                {
                    return new AsyncInstantiateOperation<T>(Internal_InstantiateAsyncWithParams(original, count, parameters, (IntPtr)positionsPtr, positions.Length, (IntPtr)rotationsPtr, rotations.Length), cancellationToken);
                }
            }
        }

        // Clones the object /original/ and returns the clone.
        [TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation)
        {
            CheckNullArgument(original, objectIsNullMessage);

            if (original is ScriptableObject)
                throw new ArgumentException("Cannot instantiate a ScriptableObject with a position and rotation");

            var obj = Internal_InstantiateSingle(original, position, rotation);

            if (obj == null)
                throw new UnityException(cloneDestroyedMessage);

            return obj;
        }

        // Clones the object /original/ and returns the clone.
        [TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (parent == null)
                return Instantiate(original, position, rotation);

            CheckNullArgument(original, objectIsNullMessage);
            if (parent.gameObject.IsDestroying())
                ThrowArgumentExceptionForParentBeingDestroyed(original.name, parent.name, nameof(parent));

            var obj = Internal_InstantiateSingleWithParent(original, parent, position, rotation);

            if (obj == null)
                throw new UnityException(cloneDestroyedMessage);

            return obj;
        }

        // Clones the object /original/ and returns the clone.
        [TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
        public static Object Instantiate(Object original)
        {
            CheckNullArgument(original, objectIsNullMessage);
            var obj = Internal_CloneSingle(original);

            if (obj == null)
                throw new UnityException(cloneDestroyedMessage);

            return obj;
        }

        // Clones the object /original/ and returns the clone.
        [TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
        public static Object Instantiate(Object original, Scene scene)
        {
            CheckNullArgument(original, objectIsNullMessage);
            var obj = Internal_CloneSingleWithScene(original, scene);

            if (obj == null)
                throw new UnityException(cloneDestroyedMessage);

            return obj;
        }

        public static T Instantiate<T>(T original, InstantiateParameters parameters) where T : UnityEngine.Object
        {
            CheckNullArgument(original, objectIsNullMessage);

            if (parameters.parent != null && parameters.parent.gameObject.IsDestroying())
                ThrowArgumentExceptionForParentBeingDestroyed(original.name, parameters.parent.name, nameof(parameters.parent));

            var obj = (T)Internal_CloneSingleWithParams(original, parameters);

            if (obj == null)
                throw new UnityException(cloneDestroyedMessage);

            return obj;
        }

        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, InstantiateParameters parameters) where T : UnityEngine.Object
        {
            CheckNullArgument(original, objectIsNullMessage);

            if (parameters.parent != null && parameters.parent.gameObject.IsDestroying())
                ThrowArgumentExceptionForParentBeingDestroyed(original.name, parameters.parent.name, nameof(parameters.parent));

            var obj = (T)Internal_InstantiateSingleWithParams(original, position, rotation, parameters);

            if (obj == null)
                throw new UnityException(cloneDestroyedMessage);

            return obj;
        }

        // Clones the object /original/ and returns the clone.
        [TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
        public static Object Instantiate(Object original, Transform parent)
        {
            return Instantiate(original, parent, false);
        }

        [TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
        public static Object Instantiate(Object original, Transform parent, bool instantiateInWorldSpace)
        {
            if (parent == null)
                return Instantiate(original);

            CheckNullArgument(original, objectIsNullMessage);
            if (parent.gameObject.IsDestroying())
                ThrowArgumentExceptionForParentBeingDestroyed(original.name, parent.name, nameof(parent));

            var obj = Internal_CloneSingleWithParent(original, parent, instantiateInWorldSpace);

            if (obj == null)
                throw new UnityException(cloneDestroyedMessage);

            return obj;
        }

        public static T Instantiate<T>(T original) where T : UnityEngine.Object
        {
            CheckNullArgument(original, objectIsNullMessage);
            var obj = (T)Internal_CloneSingle(original);

            if (obj == null)
                throw new UnityException(cloneDestroyedMessage);

            return obj;
        }

        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : UnityEngine.Object
        {
            return (T)Instantiate((Object)original, position, rotation);
        }

        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : UnityEngine.Object
        {
            return (T)Instantiate((Object)original, position, rotation, parent);
        }

        public static T Instantiate<T>(T original, Transform parent) where T : UnityEngine.Object
        {
            return Instantiate<T>(original, parent, false);
        }

        public static T Instantiate<T>(T original, Transform parent, bool worldPositionStays) where T : UnityEngine.Object
        {
            return (T)Instantiate((Object)original, parent, worldPositionStays);
        }

        // Removes a gameobject, component or asset.
        [NativeMethod(Name = "Scripting::DestroyObjectFromScripting", IsFreeFunction = true, ThrowsException = true)]
        public extern static void Destroy(Object obj, [uei.DefaultValue("0.0F")] float t);

        [uei.ExcludeFromDocs]
        public static void Destroy(Object obj)
        {
            float t = 0.0F;
            Destroy(obj, t);
        }

        // Destroys the object /obj/ immediately. It is strongly recommended to use Destroy instead.
        [NativeMethod(Name = "Scripting::DestroyObjectFromScriptingImmediate", IsFreeFunction = true, ThrowsException = true)]
        public extern static void DestroyImmediate(Object obj, [uei.DefaultValue("false")]  bool allowDestroyingAssets);

        [uei.ExcludeFromDocs]
        public static void DestroyImmediate(Object obj)
        {
            bool allowDestroyingAssets = false;
            DestroyImmediate(obj, allowDestroyingAssets);
        }

        /*
         * Profiling enter/exit playmode with the Volvo Test Track project and Gigaya has shown that ~95% of the time spent in FindObjectsOfType() is spent sorting the array by InstanceID even though in almost all cases this is not thought to be necessary.
         * In the Volvo project(2022.1) during a single enter/exit playmode cycle 203ms was spent in Object::FindObjectsOfType() of which 190ms was in the sorting(93.6%)
         * In Gigaya(2021.3) during a single enter/exit playmode cycle 496ms was spent in Object::FindObjectsOfType() of which 461ms was in the sorting(92.9%)
         * There has been a lengthy discussion in #devs-scripting about possible solutions to this (https://unity.slack.com/archives/C06TPSM32/p1651840563109579), the consensus is to deprecate FindObjectsOfType() and replace it with FindObjectsByType()
         * which lets the user choose whether to perform the sort or not
         * Note it is considered undesirable to have the API updater automatically convert FindObjectsOfType() to FindObjectsByType(FindObjectsSortMode.InstanceID) as we really want users to assess their usage on a case by case basis and only choose
         * sorting when necessary to maximise the performance gain
         * The plan is:
         *   2023.1 :
         *     FindObjectsOfType() Obsolete(warning), direct users to FindObjectsByType
         *     FindObjectOfType() Obsolete(warning), direct users to FindFirstObjectByType and FindAnyObjectByType
         *   2023.2
         *     FindObjectsOfType() Obsolete(error), direct users to FindObjectsByType
         *     FindObjectOfType() Obsolete(error), direct users to FindFirstObjectByType and FindAnyObjectByType
         *   2024.2
         *     FindObjectsOfType() deleted
         *     FindObjectOfType() deleted
         * This work is captured in https://jira.unity3d.com/browse/COPT-854
         *
         * As an update to this (12/17/2025), we are deprecated FindObject(s) methods that depend on sort order.
         * This is due to the move from InstanceID to EntityId - where we cannot depend on maintaining a meaningful sort order.
         * Additionally new FindObjectsByType methods have been introduced that do not take in a sort order parameter,
         * and we are steering users to these new methods.
         * */

        // Returns a list of all active loaded objects of Type /type/. Results are sorted by InstanceID
        [Obsolete("Object.FindObjectsOfType has been deprecated. Use Object.FindObjectsByType instead which lets you decide whether you need the results sorted or not.  FindObjectsOfType sorts the results by InstanceID, but if you do not need this using FindObjectSortMode.None is considerably faster.", false)]
        public static Object[] FindObjectsOfType(Type type)
        {
            return FindObjectsOfType(type, false);
        }

        // Returns a list of all loaded objects of Type /type/. Results are sorted by InstanceID
        [TypeInferenceRule(TypeInferenceRules.ArrayOfTypeReferencedByFirstArgument)]
        [FreeFunction("UnityEngineObjectBindings::FindObjectsOfType")]
        [Obsolete("Object.FindObjectsOfType has been deprecated. Use Object.FindObjectsByType instead which lets you decide whether you need the results sorted or not.  FindObjectsOfType sorts the results by InstanceID but if you do not need this using FindObjectSortMode.None is considerably faster.", false)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public extern static Object[] FindObjectsOfType(Type type, bool includeInactive);

        // Returns a list of all active loaded objects of Type /type/.
        [Obsolete("FindObjectsByType with FindObjectsSortMode parameter has been deprecated. Use FindObjectsByType(Type) or FindObjectsByType(Type, FindObjectsInactive) instead. InstanceID will be replaced in the future with EntityId and previous sort order cannot be maintained.", false)]
        public static Object[] FindObjectsByType(Type type, FindObjectsSortMode sortMode)
        {
            return FindObjectsByType(type, FindObjectsInactive.Exclude, sortMode);
        }

        // Returns a list of all loaded objects of Type /type/.
        [TypeInferenceRule(TypeInferenceRules.ArrayOfTypeReferencedByFirstArgument)]
        [FreeFunction("UnityEngineObjectBindings::FindObjectsByType")]
        [Obsolete("FindObjectsByType with FindObjectsSortMode parameter has been deprecated. Use FindObjectsByType(Type) or FindObjectsByType(Type, FindObjectsInactive) instead. InstanceID will be replaced in the future with EntityId and previous sort order cannot be maintained.", false)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public extern static Object[] FindObjectsByType(Type type, FindObjectsInactive findObjectsInactive, FindObjectsSortMode sortMode);

        // Returns a list of all active loaded objects of Type /type/.
        public static Object[] FindObjectsByType(Type type)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return FindObjectsByType(type, FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // Returns a list of all loaded objects of Type /type/.
        public static Object[] FindObjectsByType(Type type, FindObjectsInactive findObjectsInactive)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return FindObjectsByType(type, findObjectsInactive, FindObjectsSortMode.None);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // Allocates a batch of EntityIds
        [FreeFunction("AllocateEntityIds")]
        internal extern static EntityId[] AllocateEntityIds(int count);

        // Byte offset of the GCHandle member inside a C++ Object; the native store only
        // hands out an Object*, and the GCHandle that links it to a managed wrapper is
        // part of Object's layout, so the resolution below lives here rather than in the
        // low-level EntityIdStore. GetOffsetOfGCHandleInCPlusPlusObject is declared with
        // the other native layout offsets further down.
        //
        // -1 until first resolved; the offset is a constant of the C++ Object layout, so a
        // benign race just recomputes the same value. Survives domain reloads (the native
        // layout is invariant); a runtime that resets statics anyway just re-fetches.
        [NoAutoStaticsCleanup]
        static int s_OffsetOfGCHandleInObject = -1;

        // Resolves an EntityId to its managed wrapper via the native object's GCHandle.
        // null when the entity is not resident, or is resident natively but not yet wrapped
        // (e.g. a baked / deserialized ref on first access), so the caller can fall back to
        // native resolution which materializes the wrapper.
        internal static unsafe T GetManagedObject<T>(EntityId entity) where T : Object
        {
            void* objectPtr = EntityIdStore.GetNativeObject(entity);
            if (objectPtr == null)
                return null;

            int offset = s_OffsetOfGCHandleInObject;
            if (offset < 0)
                s_OffsetOfGCHandleInObject = offset = GetOffsetOfGCHandleInCPlusPlusObject();

            GCHandle handle = *(GCHandle*)((byte*)objectPtr + offset);
            // Reading Target on an empty handle would throw.
            if (!handle.IsAllocated)
                return null;
            return UnsafeUtility.As<T>(handle.Target);
        }

        // Makes the object /target/ not be destroyed automatically when loading a new scene.
        [FreeFunction("GetSceneManager().DontDestroyOnLoad", ThrowsException = true)]
        public extern static void DontDestroyOnLoad([NotNull] Object target);

        // // Should the object be hidden, saved with the scene or modifiable by the user?
        public extern HideFlags hideFlags { get; set; }

        //*undocumented* deprecated
        // We cannot properly deprecate this in C# right now, since the optional parameter creates
        // another method calling this, which creates compiler warnings when deprecated.
        [Obsolete("use Object.Destroy instead.")]
        public static void DestroyObject(Object obj, [uei.DefaultValue("0.0F")]  float t)
        {
            Destroy(obj, t);
        }

        [Obsolete("use Object.Destroy instead.")]
        [uei.ExcludeFromDocs]
        public static void DestroyObject(Object obj)
        {
            float t = 0.0F;
            Destroy(obj, t);
        }

        //*undocumented* DEPRECATED
        [Obsolete("Object.FindSceneObjectsOfType has been deprecated, Use Object.FindObjectsByType instead which lets you decide whether you need the results sorted or not.  FindSceneObjectsOfType sorts the results by InstanceID but if you do not need this using FindObjectSortMode.None is considerably faster.", false)]
        public static Object[] FindSceneObjectsOfType(Type type)
        {
            return FindObjectsOfType(type);
        }

        //*undocumented*  DEPRECATED
        [Obsolete("use Resources.FindObjectsOfTypeAll instead.")]
        [FreeFunction("UnityEngineObjectBindings::FindObjectsOfTypeIncludingAssets")]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public extern static Object[] FindObjectsOfTypeIncludingAssets(Type type);

        // Returns a list of all loaded objects of Type /type/. Results are sorted by InstanceID
        [Obsolete("Object.FindObjectsOfType has been deprecated. Use Object.FindObjectsByType instead which lets you decide whether you need the results sorted or not.  FindObjectsOfType sorts the results by InstanceID but if you do not need this using FindObjectSortMode.None is considerably faster.", false)]
        public static T[] FindObjectsOfType<T>() where T : Object
        {
            return Resources.ConvertObjects<T>(FindObjectsOfType(typeof(T), false));
        }

        // Returns a list of all loaded objects of Type /type/
        [Obsolete("FindObjectsByType with FindObjectsSortMode parameter has been deprecated. Use FindObjectsByType<T>() or FindObjectsByType<T>(FindObjectsInactive) instead. InstanceID will be replaced in the future with EntityId and previous sort order cannot be maintained.", false)]
        public static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode) where T : Object
        {
            return Resources.ConvertObjects<T>(FindObjectsByType(typeof(T), FindObjectsInactive.Exclude, sortMode));
        }

        // Returns a list of all loaded objects of Type /type/. Results are sorted by InstanceID
        [Obsolete("Object.FindObjectsOfType has been deprecated. Use Object.FindObjectsByType instead which lets you decide whether you need the results sorted or not.  FindObjectsOfType sorts the results by InstanceID but if you do not need this using FindObjectSortMode.None is considerably faster.", false)]
        public static T[] FindObjectsOfType<T>(bool includeInactive) where T : Object
        {
            return Resources.ConvertObjects<T>(FindObjectsOfType(typeof(T), includeInactive));
        }

        // Returns a list of all loaded objects of Type /type/. Order of results is not guaranteed to be consistent between calls
        [Obsolete("FindObjectsByType with FindObjectsSortMode parameter has been deprecated. Use FindObjectsByType<T>() or FindObjectsByType<T>(FindObjectsInactive) instead. InstanceID will be replaced in the future with EntityId and previous sort order cannot be maintained.", false)]
        public static T[] FindObjectsByType<T>(FindObjectsInactive findObjectsInactive, FindObjectsSortMode sortMode) where T : Object
        {
            return Resources.ConvertObjects<T>(FindObjectsByType(typeof(T), findObjectsInactive, sortMode));
        }


        [Obsolete("Object.FindObjectOfType has been deprecated. Use Object.FindAnyObjectByType instead.", false)]
        public static T FindObjectOfType<T>() where T : Object
        {
            return (T)FindObjectOfType(typeof(T), false);
        }

        [Obsolete("Object.FindObjectOfType has been deprecated. Use Object.FindAnyObjectByType instead.", false)]
        public static T FindObjectOfType<T>(bool includeInactive) where T : Object
        {
            return (T)FindObjectOfType(typeof(T), includeInactive);
        }

        [Obsolete("FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.", false)]
        public static T FindFirstObjectByType<T>() where T : Object
        {
            return (T)FindFirstObjectByType(typeof(T), FindObjectsInactive.Exclude);
        }

        public static T FindAnyObjectByType<T>() where T : Object
        {
            return (T)FindAnyObjectByType(typeof(T), FindObjectsInactive.Exclude);
        }

        [Obsolete("FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.", false)]
        public static T FindFirstObjectByType<T>(FindObjectsInactive findObjectsInactive) where T : Object
        {
            return (T)FindFirstObjectByType(typeof(T), findObjectsInactive);
        }

        public static T FindAnyObjectByType<T>(FindObjectsInactive findObjectsInactive) where T : Object
        {
            return (T)FindAnyObjectByType(typeof(T), findObjectsInactive);
        }

        public static T[] FindObjectsByType<T>() where T : Object
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return Resources.ConvertObjects<T>(FindObjectsByType(typeof(T), FindObjectsInactive.Exclude, FindObjectsSortMode.None));
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public static T[] FindObjectsByType<T>(FindObjectsInactive findObjectsInactive) where T : Object
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return Resources.ConvertObjects<T>(FindObjectsByType(typeof(T), findObjectsInactive, FindObjectsSortMode.None));
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [System.Obsolete("Please use Resources.FindObjectsOfTypeAll instead")]
        public static Object[] FindObjectsOfTypeAll(Type type)
        {
            return Resources.FindObjectsOfTypeAll(type);
        }

        static private void CheckNullArgument(object arg, string message)
        {
            if (arg == null)
                throw new System.ArgumentException(message);
        }

        static private void ThrowArgumentExceptionForParentBeingDestroyed(string nameOfObjectToInstantiate, string parentName, string parameterName)
        {
            throw new ArgumentException($"Trying to instantiate '{nameOfObjectToInstantiate}' as child of '{parentName}', but that parent is currently being destroyed. Check IsDestroying() on the parent GameObject before using it.", parameterName);
        }

        // Returns the first active loaded object of Type /type/.
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [Obsolete("Object.FindObjectOfType has been deprecated. Use Object.FindAnyObjectByType instead.", false)]
        public static Object FindObjectOfType(System.Type type)
        {
            Object[] objects = FindObjectsOfType(type, false);
            if (objects.Length > 0)
                return objects[0];
            else
                return null;
        }

        [Obsolete("FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.", false)]
        public static Object FindFirstObjectByType(System.Type type)
        {
            Object[] objects = FindObjectsByType(type, FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            return (objects.Length > 0) ? objects[0] : null;
        }

        public static Object FindAnyObjectByType(System.Type type)
        {
            Object[] objects = FindObjectsByType(type, FindObjectsInactive.Exclude);
            return (objects.Length > 0) ? objects[0] : null;
        }

        // Returns the first active loaded object of Type /type/.
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [Obsolete("Object.FindObjectOfType has been deprecated. Use Object.FindAnyObjectByType instead.", false)]
        public static Object FindObjectOfType(System.Type type, bool includeInactive)
        {
            Object[] objects = FindObjectsOfType(type, includeInactive);
            if (objects.Length > 0)
                return objects[0];
            else
                return null;
        }

        [Obsolete("FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.", false)]
        public static Object FindFirstObjectByType(System.Type type, FindObjectsInactive findObjectsInactive)
        {
            Object[] objects = FindObjectsByType(type, findObjectsInactive, FindObjectsSortMode.InstanceID);
            return (objects.Length > 0) ? objects[0] : null;
        }

        public static Object FindAnyObjectByType(System.Type type, FindObjectsInactive findObjectsInactive)
        {
            Object[] objects = FindObjectsByType(type, findObjectsInactive);
            return (objects.Length > 0) ? objects[0] : null;
        }

        // Returns the name of the game object.
        public override string ToString()
        {
            return ToString(this);
        }

        public static bool operator==(Object x, Object y) { return CompareBaseObjects(x, y); }

        public static bool operator!=(Object x, Object y) { return !CompareBaseObjects(x, y); }

        [NativeMethod(Name = "Object::GetOffsetOfGCHandleMember", IsFreeFunction = true, IsThreadSafe = true)]
        extern static int GetOffsetOfGCHandleInCPlusPlusObject();

        [NativeMethod(Name = "Object::GetOffsetOfTypeIndexWord", IsFreeFunction = true, IsThreadSafe = true)]
        extern static int GetOffsetOfTypeIndexWordInCPlusPlusObject();

        [NativeMethod(Name = "Object::RegisterCreatedCallback", IsFreeFunction = true, IsThreadSafe = true)]
        internal extern static void RegisterEntityCreatedCallback(IntPtr callback);

        [NativeMethod(Name = "Object::RegisterEntityDestroyedCallback", IsFreeFunction = true, IsThreadSafe = true)]
        internal extern static void RegisterEntityDestroyedCallback(IntPtr callback);

        // Editor fake-null (MonoObjectNULL) side channel; see Scripting::SetPendingFakeNullWrapper.
        [NativeMethod(Name = "Scripting::TakePendingFakeNullWrapper", IsFreeFunction = true, IsThreadSafe = true)]
        extern static IntPtr TakePendingFakeNullWrapper();

        [NativeMethod(Name = "CurrentThreadIsMainThread", IsFreeFunction = true, IsThreadSafe = true)]
        extern static bool CurrentThreadIsMainThread();

        [NativeMethod(Name = "CloneObject", IsFreeFunction = true, ThrowsException = true)]
        extern static Object Internal_CloneSingle([NotNull] Object data);

        [FreeFunction("CloneObjectToScene")]
        extern static Object Internal_CloneSingleWithScene([NotNull] Object data, Scene scene);

        [FreeFunction("CloneObjectWithParams")]
        extern static Object Internal_CloneSingleWithParams([NotNull] Object data, InstantiateParameters parameters);
        [FreeFunction("InstantiateObjectWithParams")]
        extern static Object Internal_InstantiateSingleWithParams([NotNull] Object data, Vector3 position, Quaternion rotation, InstantiateParameters parameters);

        [FreeFunction("CloneObject")]
        extern static Object Internal_CloneSingleWithParent([NotNull] Object data, [NotNull] Transform parent, bool worldPositionStays);

        [FreeFunction("InstantiateAsyncObjects")]
        extern static IntPtr Internal_InstantiateAsyncWithParams([NotNull] Object original, int count, InstantiateParameters parameters, IntPtr positions, int positionsCount, IntPtr rotations, int rotationsCount);

        [FreeFunction("InstantiateObject")]
        extern static Object Internal_InstantiateSingle([NotNull] Object data, Vector3 pos, Quaternion rot);

        [FreeFunction("InstantiateObject")]
        extern static Object Internal_InstantiateSingleWithParent([NotNull] Object data, [NotNull] Transform parent, Vector3 pos, Quaternion rot);

        [FreeFunction("UnityEngineObjectBindings::ToString")]
        extern static string ToString(Object obj);

        [FreeFunction("UnityEngineObjectBindings::GetName", HasExplicitThis = true)]
        extern string GetName();

        [FreeFunction("UnityEngineObjectBindings::IsPersistent")]
        internal extern static bool IsPersistent([NotNull] Object obj);

        [FreeFunction("UnityEngineObjectBindings::SetName", HasExplicitThis = true)]
        extern void SetName(string name);

        [NativeMethod(Name = "UnityEngineObjectBindings::DoesObjectWithInstanceIDExist", IsFreeFunction = true, IsThreadSafe = true)]
        internal extern static bool DoesObjectWithInstanceIDExist(EntityId instanceID);

        [VisibleToOtherModules]
        [FreeFunction("UnityEngineObjectBindings::FindObjectFromInstanceID")]
        internal extern static Object FindObjectFromInstanceID(EntityId instanceID);

        [VisibleToOtherModules]
        [FreeFunction("UnityEngineObjectBindings::FindObjectFromInstanceIDThreadSafe", IsThreadSafe = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        internal extern static Object FindObjectFromInstanceIDThreadSafe(EntityId instanceID);

        [FreeFunction("UnityEngineObjectBindings::GetPtrFromInstanceID")]
        private extern static IntPtr GetPtrFromInstanceID(EntityId instanceID, Type objectType);

        [VisibleToOtherModules]
        [FreeFunction("UnityEngineObjectBindings::ForceLoadFromInstanceID")]
        internal extern static Object ForceLoadFromInstanceID(EntityId instanceID);
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static Object CreateMissingReferenceObject(EntityId instanceID)
        {
            // Never bound, so it carries the id tagged as released
            return new Object { m_EntityId = TagReleasedWrapperId(instanceID) };
        }

        [FreeFunction("UnityEngineObjectBindings::MarkObjectDirty", HasExplicitThis = true)]
        internal extern void MarkDirty();

        [VisibleToOtherModules]
        internal static class MarshalledUnityObject
        {
            // Lazy scripting-wrapper creation, owned by managed code.
            //
            // A native Object carries an 8-byte GCHandle slot (see BaseObject.h). The wrapper is created
            // here in C# on first use and its strong GCHandle is written back into that slot, so every
            // later lookup (native or managed) resolves to the same instance. The runtime type is read
            // from native memory: the type index is in a bitfield word at s_TypeIndexWordOffset, isolated
            // with the shift/mask below (matching the bit layout in BaseObject.h).
            //
            // Offsets are read once by Initialize(); the per-object path is then plain memory access
            // plus a dictionary lookup for the factory. Not set via field initializers or a static ctor:
            // a .cctor would add a class-init check before every offset read on the resolve path.
            // Initialize() must run before the first resolve; CoreInitializer calls it from [OnAssemblyLoaded].
            // Native layout offsets, valid for the process lifetime.
            [NoAutoStaticsCleanup] static int s_GCHandleOffset;
            [NoAutoStaticsCleanup] static int s_TypeIndexWordOffset;

            // Called by CoreInitializer before any wrapper is resolved. See the offset fields above for
            // why this is a method, not a static ctor.
            internal static void Initialize()
            {
                s_GCHandleOffset = GetOffsetOfGCHandleInCPlusPlusObject();
                s_TypeIndexWordOffset = GetOffsetOfTypeIndexWordInCPlusPlusObject();
            }

            // kMemLabelBits(11) + kTemporaryFlagsBits(1) + kHideFlagsBits(7) + kIsPersistentBits(1)
            const int k_TypeIndexShift = 20;
            // kCachedTypeIndexBits(12)
            const uint k_TypeIndexMask = 0xFFF;


            // Returns the managed wrapper for the native object at objPtr, whose EntityId is id, creating the
            // wrapper on first use. The cached case is inlined; the create-on-miss path is outlined below.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe UnityEngine.Object EnsureScriptingWrapperFor(IntPtr objPtr, EntityId id)
            {
                // Fast path: wrapper already created.
                IntPtr existing = *(IntPtr*)((byte*)objPtr + s_GCHandleOffset);
                if (existing != IntPtr.Zero)
                    return ResolveWrapperHandle(existing);

                return CreateScriptingWrapperForSlow(objPtr, id);
            }

            // Creates the wrapper and publishes its handle into the slot with a CAS. If another thread
            // published first, frees ours and uses theirs. Building a wrapper twice on a race is harmless:
            // it only stores the EntityId, and the extra managed object becomes garbage once its handle is freed.
            [MethodImpl(MethodImplOptions.NoInlining)]
            static unsafe UnityEngine.Object CreateScriptingWrapperForSlow(IntPtr objPtr, EntityId id)
            {
                uint word = *(uint*)((byte*)objPtr + s_TypeIndexWordOffset);
                int runtimeTypeIndex = (int)((word >> k_TypeIndexShift) & k_TypeIndexMask);

                // Walks the native RTTI base chain to the nearest registered wrapper factory; native-only
                // types resolve to their nearest managed ancestor. The ctor stores the EntityId.
                var wrapper = TypeManagerV2.CreateProxyObject(runtimeTypeIndex, id) as UnityEngine.Object;
                if (wrapper == null)
                {
                    // Scripted object (MonoBehaviour/ScriptableObject subclass) not yet created by the
                    // MonoScript machinery. Don't create one here: it would be the wrong type. Mirrors
                    // native ScriptingWrapperFor's IsAScriptedObject() early-out.
                    return null;
                }

                if (!TryPublishWrapper(objPtr, wrapper, out IntPtr installed))
                {
                    // Another thread published first: use theirs.
                    return ResolveWrapperHandle(installed);
                }
                return wrapper;
            }

            // Publishes `wrapper` as the managed wrapper of the native object at objPtr: allocates a strong
            // GCHandle (kept for the native object's lifetime; freed and the slot zeroed natively when the object
            // is destroyed or the domain is unloaded) and CASes it into the object's GCHandle slot. Returns false,
            // with the handle freed and `installed` set to the existing handle, if the slot was already taken.
            internal static unsafe bool TryPublishWrapper(IntPtr objPtr, UnityEngine.Object wrapper, out IntPtr installed)
            {
                GCHandle handle = GCHandle.Alloc(wrapper);
                IntPtr* slot = (IntPtr*)((byte*)objPtr + s_GCHandleOffset);
                installed = Interlocked.CompareExchange(ref *slot, GCHandle.ToIntPtr(handle), IntPtr.Zero);
                if (installed != IntPtr.Zero)
                {
                    handle.Free();
                    return false;
                }
                return true;
            }

            internal static bool TryPublishWrapper(IntPtr objPtr, UnityEngine.Object wrapper)
            {
                return TryPublishWrapper(objPtr, wrapper, out _);
            }

            // A MonoBehaviour/ScriptableObject is its C# instance: only the one in the native GC-handle slot is the object,
            // not a placeholder or stale instance with the same id. Other types carry no managed state, so any instance is.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static unsafe bool IsInstanceOf(void* objPtr, UnityEngine.Object o)
            {
                IntPtr handle = *(IntPtr*)((byte*)objPtr + s_GCHandleOffset);
                if (handle != IntPtr.Zero && ReferenceEquals(ResolveWrapperHandle(handle), o))
                    return true;
                return !(o is MonoBehaviour || o is ScriptableObject);
            }

            // Marshalling for standards-compliant backends (CoreCLR). A UnityEngine.Object parameter, return value,
            // struct field or array element crosses the boundary as its EntityId; native resolves the id when it
            // needs the pointer (Marshalling::ReadOnlyUnityObjectMarshaller). Marshalling in is a field read, plus one
            // EntityIdStore lookup for [NotNull] parameters so the exception is raised before the call.

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe EntityId MarshalId<T>(T obj) where T : Object
            {
                // Do not do an == null or .Equals(null) check here or anything that would make an icall; see Marshal<T>.
                if (ReferenceEquals(obj, null))
                    return EntityId.None;

                var storedId = obj.m_EntityId;
                if (EntityIdStore.GetRawNativeObject(storedId) != null)
                    return storedId;
                if (IsReleasedWrapperId(storedId))
                    return MarshalReleasedWrapperId(obj, storedId);
                // Native loads an unloaded asset when it resolves the id; a script-derived instance that is not the
                // object's own must not cause that.
                if (IsScriptDerivedClass(obj))
                    return EntityId.None;
                return storedId;
            }

            // Native resolves ids with a lookup that accepts unpublished objects, so a released wrapper must only pass a published or unloaded one
            static unsafe EntityId MarshalReleasedWrapperId<T>(T obj, EntityId storedId) where T : Object
            {
                var entityId = UntagReleasedWrapperId(storedId);
                if (EntityIdStore.GetNativeObject(entityId) != null)
                    return entityId;
                if (EntityIdStore.GetRawNativeObject(entityId) == null)
                    return IsScriptDerivedClass(obj) ? EntityId.None : entityId;

                // Still loading or being destroyed: the PersistentManager waits for a load in flight
                return MarshalFromInstanceId(obj) != IntPtr.Zero ? entityId : EntityId.None;
            }

            // [NotNull] parameter: obj itself has already been null-checked by the generated code. Throws
            // ArgumentNullException (with the "destroyed but still trying to access it" diagnostics) unless obj
            // resolves to a live native object.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static EntityId MarshalIdNotNull<T>(T obj, string parameterName) where T : Object
            {
                if (!ResolvesToLiveNativeObject(obj))
                    ThrowArgumentNullException(obj, parameterName);
                return obj.GetEntityId();
            }

            // [NotNull] 'this' or struct field: same, throwing NullReferenceException; see MarshalNotNullThis.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static EntityId MarshalIdNotNullThis<T>(T obj) where T : Object
            {
                if (MarshalNotNullThis(obj) == IntPtr.Zero)
                    ThrowNullReferenceException(obj);
                return obj.GetEntityId();
            }

            // Whether native will be able to resolve obj's EntityId to an object of type T. Fast path: the id is in
            // the EntityIdStore and obj is not an editor fake-null wrapper. Editor slow path: the object may be an
            // unloaded asset (loaded through the PersistentManager) or a fake-null wrapper whose id now names a live
            // object (type-checked), both handled by MarshalFromInstanceId; native applies the same rules again when
            // it resolves the id (Scripting::ResolveNativeObject).
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static unsafe bool ResolvesToLiveNativeObject<T>(T obj) where T : Object
            {
                if (GetNativeObjectForWrapper(obj.m_EntityId) != null && CanResolveDirectly(obj))
                    return true;
                return MarshalFromInstanceId(obj) != IntPtr.Zero;
            }

            // Marshalling for Mono and IL2CPP. A UnityEngine.Object parameter crosses as the native Object*, resolved here
            // through the EntityIdStore.

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static IntPtr Marshal<T>(T obj) where T : Object
            {
                // Do not to an == null or .Equals(null) check in here or anything that would make an icall
                // This may be called during AppDomain shutdown and there is code called during shutdown
                // that relies on the SCRIPTINGAPI_THREAD_AND_SERIALIZATION_CHECK throwing on shutdown
                // So this code can't call any icalls marked as ThreadSafe (e.g. DoesObjectWithInstanceIDExist)
                if (ReferenceEquals(obj, null))
                    return IntPtr.Zero;
                return MarshalNotNull(obj);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe IntPtr MarshalNotNull<T>(T obj) where T : Object
            {
                // obj has already been checked and is guaranteed to not be null
                void* nativeObject = GetNativeObjectForWrapper(obj.m_EntityId);
                if (nativeObject != null && CanResolveDirectly(obj))
                    return (IntPtr)nativeObject;
                return MarshalFromInstanceId(obj);
            }

            // 'this' of an instance icall: an instance of a script-derived class must be the one the native object holds,
            // or the call's arguments would come from another instance's fields. Base-class instances carry no such state.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe IntPtr MarshalNotNullThis<T>(T obj) where T : Object
            {
                void* nativeObject = GetNativeObjectForWrapper(obj.m_EntityId);
                if (nativeObject != null && (IsInstanceOf(nativeObject, obj) || !IsScriptDerivedClass(obj)) && CanResolveDirectly(obj))
                    return (IntPtr)nativeObject;
                return MarshalFromInstanceId(obj);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool IsScriptDerivedClass(Object obj)
            {
                var type = obj.GetType();
                return (obj is MonoBehaviour && type != typeof(MonoBehaviour)) || (obj is ScriptableObject && type != typeof(ScriptableObject));
            }

            // A fake-null wrapper (m_UnityRuntimeErrorString set, see TransferPPtrToMonoObject.cpp) can carry the id of a
            // live object of another type (UUM-143556); it must resolve through native, which type-checks.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static bool CanResolveDirectly(Object obj)
            {
                return obj.m_UnityRuntimeErrorString == null;
            }

            // Editor slow path: the object is not loaded, or obj is a fake-null wrapper. Resolves through
            // native, which loads the object from the PersistentManager if needed and checks that it is
            // assignable to the declared parameter type T.
            private static IntPtr MarshalFromInstanceId<T>(T obj) where T : Object
            {
                if (obj.m_EntityId == EntityId.None)
                    return IntPtr.Zero;

                // A script-derived instance that is not the object's own must not load the object.
                if (IsScriptDerivedClass(obj))
                    return IntPtr.Zero;

                return GetPtrFromInstanceID(obj.GetEntityId(), typeof(T));
            }

            public static void TryThrowEditorNullExceptionObject(Object unityObj) => TryThrowEditorNullExceptionObject(unityObj, null);

            public static void TryThrowEditorNullExceptionObject(Object unityObj, string parameterName)
            {
                string error = unityObj.m_UnityRuntimeErrorString ?? "";
                if (unityObj.m_EntityId != EntityId.None && !error.StartsWith($"{nameof(MissingReferenceException)}:"))
                {
                    error = $"The object of type '{unityObj.GetType().FullName}' has been destroyed but you are still trying to access it.\n" +
                        "Your script should either check if it is null or you should not destroy the object.";

                    if (!string.IsNullOrEmpty(parameterName))
                        error += $" Parameter name: {parameterName}";

                    throw new MissingReferenceException(error);
                }

                var splitIndex = error.IndexOf(':');
                if (splitIndex > 0)
                {
                    var exceptionTypeString = error.Substring(0, splitIndex);
                    error = error.Substring(splitIndex + 1);
                    if (!string.IsNullOrEmpty(parameterName))
                        error += $" Parameter name: {parameterName}";

                    var exceptionType = Type.GetType($"UnityEngine.{exceptionTypeString}", false);
                    if (exceptionType != null)
                        throw (Exception)Activator.CreateInstance(exceptionType, error);
                }
            }


            // Scalar return values: native returns the object's EntityId (see Marshalling::UnityObjectReturnValue).
            // Resolve it through the EntityIdStore and hand back the object's wrapper, creating it on first use.
            // EntityId.None (or an id that no longer resolves) is null; the editor fake-null wrapper, if any,
            // comes through the side channel.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe T Unmarshal<T>(EntityId id) where T : UnityEngine.Object
            {
                if (id == EntityId.None)
                    return UnmarshalNullOrFakeNull<T>();

                void* objPtr = EntityIdStore.GetRawNativeObject(id);
                if (objPtr == null)
                    return UnmarshalNullOrFakeNull<T>();

                // The native return type is T by construction, so reinterpret rather than castclass.
                return UnsafeUtility.As<T>(EnsureScriptingWrapperFor((IntPtr)objPtr, id));
            }

            // Outlined null / editor fake-null handling; NoInlining keeps Unmarshal<T>'s hot path small.
            [MethodImpl(MethodImplOptions.NoInlining)]
            static T UnmarshalNullOrFakeNull<T>() where T : UnityEngine.Object
            {
                // MonoObjectNULL fake-null: no native object, delivered through the side channel.
                // Consume the temp handle and free it here (it was created just for this return).
                IntPtr pending = TakePendingFakeNullWrapper();
                if (pending != IntPtr.Zero)
                {
                    var tempHandle = FromIntPtrUnsafe(pending);
                    var fakeNull = (T)tempHandle.Target;
                    tempHandle.Free();
                    return fakeNull;
                }
                return null;
            }

            // Resolves a strong GCHandle from an object's slot to its wrapper. On CoreCLR the handle
            // points at the table entry holding the object reference, so read it directly (like native
            // scripting_gchandle_get_target_unsafe); the low pin bit is masked off. The slot only ever
            // holds a UnityEngine.Object, so reinterpret instead of castclass.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static unsafe UnityEngine.Object ResolveWrapperHandle(IntPtr handle)
            {
                IntPtr* entry = (IntPtr*)(void*)((nint)handle & ~(nint)1);
                return UnsafeUtility.As<UnityEngine.Object>(UnsafeUtility.As<IntPtr, object>(ref *entry));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static GCHandle FromIntPtrUnsafe(IntPtr gcHandle)
            {
                return GCHandle.FromIntPtr(gcHandle);
            }

            // UnityEngine.Object collections cross as one EntityId per element, in both directions (see
            // Marshalling::UnityObjectArrayElement / ContainerFromArray). An element that is an unloaded asset keeps
            // its id; native loads it if it needs the pointer.
            public unsafe static void Marshal<T, TCollectionAccessor>(in TCollectionAccessor collectionAccessor, ref MarshalledArray marshalledArray)
                where T : UnityEngine.Object
                where TCollectionAccessor : struct, ICollectionMarshallingAccessor<T>
            {
                MarshalledArray.Allocate<T, EntityId, TCollectionAccessor>(collectionAccessor, ref marshalledArray, elementCleanupRequired: false);
                var span = marshalledArray.AsSpan<EntityId>();

                for (int i = 0; i < span.Length; i++)
                    span[i] = MarshalId(collectionAccessor[i]);
            }

            public static void Unmarshal<T, TCollectionAccessor>(in MarshalledArray marshalledArray, ref TCollectionAccessor collectionAccessor)
                where T : UnityEngine.Object
                where TCollectionAccessor : struct, ICollectionMarshallingAccessor<T>
            {
                var span = marshalledArray.GetDataForUnmarshal<T, EntityId, TCollectionAccessor>(ref collectionAccessor);

                for (int i = 0; i < span.Length; i++)
                    collectionAccessor[i] = Unmarshal<T>(span[i]);
            }

            [System.Diagnostics.CodeAnalysis.DoesNotReturn]
            public static void ThrowArgumentNullException(object obj, string parameterName)
            {
                if (obj is UnityEngine.Object unityObj)
                    TryThrowEditorNullExceptionObject(unityObj, parameterName);
                throw new ArgumentNullException(parameterName);
            }

            [System.Diagnostics.CodeAnalysis.DoesNotReturn]
            public static void ThrowNullReferenceException(object obj)
            {
                if (obj is UnityEngine.Object unityObj)
                    TryThrowEditorNullExceptionObject(unityObj, null);
                throw new NullReferenceException();
            }
        }
    }
}
