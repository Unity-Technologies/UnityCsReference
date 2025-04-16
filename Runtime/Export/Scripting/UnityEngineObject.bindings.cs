// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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
    }

    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode(GenerateProxy = true)]
    [NativeHeader("Runtime/Export/Scripting/UnityEngineObject.bindings.h")]
    [NativeHeader("Runtime/GameCode/CloneObject.h")]
    [NativeHeader("Runtime/SceneManager/SceneManager.h")]
    public partial class Object
    {
        private const int kInstanceID_None = 0;

#pragma warning disable 649
        IntPtr   m_CachedPtr;

        private int m_InstanceID;
#pragma warning disable 169
        private string m_UnityRuntimeErrorString;
#pragma warning restore 169

#pragma warning disable 414
#pragma warning restore 414
#pragma warning restore 649

        const string objectIsNullMessage = "The Object you want to instantiate is null.";
        const string cloneDestroyedMessage = "Instantiate failed because the clone was destroyed during creation. This can happen if DestroyImmediate is called in MonoBehaviour.Awake.";

        [System.Security.SecuritySafeCritical]
        public unsafe int GetInstanceID()
        {
            //Because in the player we dissalow calling GetInstanceID() on a non-mainthread, we're also
            //doing this in the editor, so people notice this problem early. even though technically in the editor,
            //it is a threadsafe operation.
            EnsureRunningOnMainThread();
            return m_InstanceID;
        }

        public override int GetHashCode()
        {
            //in the editor, we store the m_InstanceID in the c# objects. It's actually possible to have multiple c# objects
            //pointing to the same c++ object in some edge cases, and in those cases we'd like GetHashCode() and Equals() to treat
            //these objects as equals.
            return m_InstanceID;
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

            return lhs.m_InstanceID == rhs.m_InstanceID;
        }

        private void EnsureRunningOnMainThread()
        {
            if (!CurrentThreadIsMainThread())
                throw new System.InvalidOperationException("EnsureRunningOnMainThread can only be called from the main thread");
        }

        static bool IsNativeObjectAlive(UnityEngine.Object o)
        {
            if (o.GetCachedPtr() != IntPtr.Zero)
                return true;

            //Ressurection of assets is complicated.
            //For almost all cases, if you have a c# wrapper for an asset like a material,
            //if the material gets moved, or deleted, and later placed back, the persistentmanager
            //will ensure it will come back with the same instanceid.
            //in this case, we want the old c# wrapper to still "work".
            //we only support this behaviour in the editor, even though there
            //are some cases in the player where this could happen too. (when unloading things from assetbundles)
            //supporting this makes all operator== slow though, so we decided to not support it in the player.
            //
            //we have an exception for assets that "are" a c# object, like a MonoBehaviour in a prefab, and a ScriptableObject.
            //in this case, the asset "is" the c# object,  and you cannot actually pretend
            //the old wrapper points to the new c# object. this is why we make an exception in the operator==
            //for this case. If we had a c# wrapper to a persistent monobehaviour, and that one gets
            //destroyed, and placed back with the same instanceID,  we still will say that the old
            //c# object is null.
            if (o is MonoBehaviour || o is ScriptableObject)
                return false;

            return DoesObjectWithInstanceIDExist(o.GetInstanceID());
        }

        System.IntPtr GetCachedPtr()
        {
            return m_CachedPtr;
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
            var obj = (T)Internal_CloneSingleWithParams(original, parameters);

            if (obj == null)
                throw new UnityException(cloneDestroyedMessage);

            return obj;
        }

        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, InstantiateParameters parameters) where T : UnityEngine.Object
        {
            CheckNullArgument(original, objectIsNullMessage);
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
        public extern static Object[] FindObjectsOfType(Type type, bool includeInactive);

        // Returns a list of all active loaded objects of Type /type/.
        public static Object[] FindObjectsByType(Type type, FindObjectsSortMode sortMode)
        {
            return FindObjectsByType(type, FindObjectsInactive.Exclude, sortMode);
        }

        // Returns a list of all loaded objects of Type /type/.
        [TypeInferenceRule(TypeInferenceRules.ArrayOfTypeReferencedByFirstArgument)]
        [FreeFunction("UnityEngineObjectBindings::FindObjectsByType")]
        public extern static Object[] FindObjectsByType(Type type, FindObjectsInactive findObjectsInactive, FindObjectsSortMode sortMode);

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
        public extern static Object[] FindObjectsOfTypeIncludingAssets(Type type);

        // Returns a list of all loaded objects of Type /type/. Results are sorted by InstanceID
        [Obsolete("Object.FindObjectsOfType has been deprecated. Use Object.FindObjectsByType instead which lets you decide whether you need the results sorted or not.  FindObjectsOfType sorts the results by InstanceID but if you do not need this using FindObjectSortMode.None is considerably faster.", false)]
        public static T[] FindObjectsOfType<T>() where T : Object
        {
            return Resources.ConvertObjects<T>(FindObjectsOfType(typeof(T), false));
        }

        // Returns a list of all loaded objects of Type /type/
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
        public static T[] FindObjectsByType<T>(FindObjectsInactive findObjectsInactive, FindObjectsSortMode sortMode) where T : Object
        {
            return Resources.ConvertObjects<T>(FindObjectsByType(typeof(T), findObjectsInactive, sortMode));
        }


        [Obsolete("Object.FindObjectOfType has been deprecated. Use Object.FindFirstObjectByType instead or if finding any instance is acceptable the faster Object.FindAnyObjectByType", false)]
        public static T FindObjectOfType<T>() where T : Object
        {
            return (T)FindObjectOfType(typeof(T), false);
        }

        [Obsolete("Object.FindObjectOfType has been deprecated. Use Object.FindFirstObjectByType instead or if finding any instance is acceptable the faster Object.FindAnyObjectByType", false)]
        public static T FindObjectOfType<T>(bool includeInactive) where T : Object
        {
            return (T)FindObjectOfType(typeof(T), includeInactive);
        }

        public static T FindFirstObjectByType<T>() where T : Object
        {
            return (T)FindFirstObjectByType(typeof(T), FindObjectsInactive.Exclude);
        }

        public static T FindAnyObjectByType<T>() where T : Object
        {
            return (T)FindAnyObjectByType(typeof(T), FindObjectsInactive.Exclude);
        }

        public static T FindFirstObjectByType<T>(FindObjectsInactive findObjectsInactive) where T : Object
        {
            return (T)FindFirstObjectByType(typeof(T), findObjectsInactive);
        }

        public static T FindAnyObjectByType<T>(FindObjectsInactive findObjectsInactive) where T : Object
        {
            return (T)FindAnyObjectByType(typeof(T), findObjectsInactive);
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

        // Returns the first active loaded object of Type /type/.
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [Obsolete("Object.FindObjectOfType has been deprecated. Use Object.FindFirstObjectByType instead or if finding any instance is acceptable the faster Object.FindAnyObjectByType", false)]
        public static Object FindObjectOfType(System.Type type)
        {
            Object[] objects = FindObjectsOfType(type, false);
            if (objects.Length > 0)
                return objects[0];
            else
                return null;
        }

        public static Object FindFirstObjectByType(System.Type type)
        {
            Object[] objects = FindObjectsByType(type, FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            return (objects.Length > 0) ? objects[0] : null;
        }

        public static Object FindAnyObjectByType(System.Type type)
        {
            Object[] objects = FindObjectsByType(type, FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return (objects.Length > 0) ? objects[0] : null;
        }

        // Returns the first active loaded object of Type /type/.
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [Obsolete("Object.FindObjectOfType has been deprecated. Use Object.FindFirstObjectByType instead or if finding any instance is acceptable the faster Object.FindAnyObjectByType", false)]
        public static Object FindObjectOfType(System.Type type, bool includeInactive)
        {
            Object[] objects = FindObjectsOfType(type, includeInactive);
            if (objects.Length > 0)
                return objects[0];
            else
                return null;
        }

        public static Object FindFirstObjectByType(System.Type type, FindObjectsInactive findObjectsInactive)
        {
            Object[] objects = FindObjectsByType(type, findObjectsInactive, FindObjectsSortMode.InstanceID);
            return (objects.Length > 0) ? objects[0] : null;
        }

        public static Object FindAnyObjectByType(System.Type type, FindObjectsInactive findObjectsInactive)
        {
            Object[] objects = FindObjectsByType(type, findObjectsInactive, FindObjectsSortMode.None);
            return (objects.Length > 0) ? objects[0] : null;
        }

        // Returns the name of the game object.
        public override string ToString()
        {
            return ToString(this);
        }

        public static bool operator==(Object x, Object y) { return CompareBaseObjects(x, y); }

        public static bool operator!=(Object x, Object y) { return !CompareBaseObjects(x, y); }

        [NativeMethod(Name = "Object::GetOffsetOfInstanceIdMember", IsFreeFunction = true, IsThreadSafe = true)]
        extern static int GetOffsetOfInstanceIDInCPlusPlusObject();

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
        internal extern static bool DoesObjectWithInstanceIDExist(int instanceID);

        [VisibleToOtherModules]
        [FreeFunction("UnityEngineObjectBindings::FindObjectFromInstanceID")]
        internal extern static Object FindObjectFromInstanceID(int instanceID);

        [FreeFunction("UnityEngineObjectBindings::GetPtrFromInstanceID")]
        private extern static IntPtr GetPtrFromInstanceID(int instanceID, Type objectType, out bool isMonoBehaviour);

        [VisibleToOtherModules]
        [FreeFunction("UnityEngineObjectBindings::ForceLoadFromInstanceID")]
        internal extern static Object ForceLoadFromInstanceID(int instanceID);
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static Object CreateMissingReferenceObject(int instanceID)
        {
            return new Object { m_InstanceID = instanceID };
        }

        [FreeFunction("UnityEngineObjectBindings::MarkObjectDirty", HasExplicitThis = true)]
        internal extern void MarkDirty();

        [VisibleToOtherModules]
        internal static class MarshalledUnityObject
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static IntPtr Marshal<T>(T obj) where T: Object
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
            public static IntPtr MarshalNotNull<T>(T obj) where T : Object
            {
                // obj has already been checked and is guaranteed to not be null
                if (obj.m_CachedPtr != IntPtr.Zero)
                    return obj.m_CachedPtr;
                return MarshalFromInstanceId(obj);
            }

            private static IntPtr MarshalFromInstanceId<T>(T obj) where T:Object
            {
                if (obj.m_InstanceID == kInstanceID_None)
                    return IntPtr.Zero;

                var retPtr = GetPtrFromInstanceID(obj.m_InstanceID, typeof(T), out var isNativeInstanceMonoBehaviour);
                if (retPtr == IntPtr.Zero)
                    return IntPtr.Zero;

                if (!isNativeInstanceMonoBehaviour)
                    return retPtr;

                if(IsMonoBehaviourOrScriptableObjectOrParentClass(obj))
                    return retPtr;

                return IntPtr.Zero;
            }

            static bool IsMonoBehaviourOrScriptableObjectOrParentClass(Object obj)
            {
                // There might be multiple C# objects pointing to the same C++ object. This is not safe for
                // MonoBehaviour/ScriptableObject _derived_ classes that might have additional state in their C# objects,
                // as the C# objects would get out of sync.
                // However, it is safe for multiple MonoBehaviour/ScriptableObject (and parent classes) C# objects to point to
                // the same C++ object, because all the state reachable from a C# reference with such a type is stored in the C++ object
                // and they will therefore always be in sync.

                var objClass = obj.GetType();

                if (objClass == typeof(Object) || objClass == typeof(MonoBehaviour) || objClass == typeof(ScriptableObject))
                    return true;

                return Array.IndexOf(m_MonoBehaviorBaseClasses, objClass) >= 0;
            }

            private static readonly Type[] m_MonoBehaviorBaseClasses;

            static MarshalledUnityObject()
            {
                var baseClassList = new List<Type>();
                var baseClass = typeof(MonoBehaviour).BaseType;
                while (baseClass != typeof(Object))
                {
                    baseClassList.Add(baseClass);
                    baseClass = baseClass.BaseType;
                }
                baseClass = typeof(ScriptableObject).BaseType;
                while (baseClass != typeof(Object))
                {
                    baseClassList.Add(baseClass);
                    baseClass = baseClass.BaseType;
                }
                m_MonoBehaviorBaseClasses = baseClassList.ToArray();
            }

            public static void TryThrowEditorNullExceptionObject(Object unityObj, string parameterName)
            {
                string error = unityObj.m_UnityRuntimeErrorString ?? "";
                if (unityObj.m_InstanceID != kInstanceID_None && !error.StartsWith($"{nameof(MissingReferenceException)}:"))
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

        }
    }
}
