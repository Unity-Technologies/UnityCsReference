// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngineInternal;
using uei = UnityEngine.Internal;

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

        DontSave = 4 + 16 + 32,

        // A combination of not shown in the hierarchy and not saved to to scenes.
        HideAndDontSave = 1 + 4 + 8 + 16 + 32
    }

    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [GenerateManagedProxy]
    [NativeHeader("Runtime/Export/UnityEngineObject.bindings.h")]
    [NativeHeader("Runtime/GameCode/CloneObject.h")]
    public partial class Object
    {
        // This has to be public on .NET scripting backend
        // because it is set by generated IL from outside for performance reasons
        IntPtr   m_CachedPtr;

        private int m_InstanceID;
#pragma warning disable 169
        private string m_UnityRuntimeErrorString;

        internal static int OffsetOfInstanceIDInCPlusPlusObject = -1;

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
        public static implicit operator bool(Object exists)
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
            get { return GetName(this); }
            set { SetName(this, value); }
        }

        // Clones the object /original/ and returns the clone.
        [TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation)
        {
            CheckNullArgument(original, "The Object you want to instantiate is null.");

            if (original is ScriptableObject)
                throw new ArgumentException("Cannot instantiate a ScriptableObject with a position and rotation");

            return Internal_InstantiateSingle(original, position, rotation);
        }

        // Clones the object /original/ and returns the clone.
        [TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (parent == null)
                return Internal_InstantiateSingle(original, position, rotation);

            CheckNullArgument(original, "The Object you want to instantiate is null.");

            return Internal_InstantiateSingleWithParent(original, parent, position, rotation);
        }

        // Clones the object /original/ and returns the clone.
        [TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
        public static Object Instantiate(Object original)
        {
            CheckNullArgument(original, "The Object you want to instantiate is null.");
            return Internal_CloneSingle(original);
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
                return Internal_CloneSingle(original);

            CheckNullArgument(original, "The Object you want to instantiate is null.");

            return Internal_CloneSingleWithParent(original, parent, instantiateInWorldSpace);
        }

        public static T Instantiate<T>(T original) where T : UnityEngine.Object
        {
            CheckNullArgument(original, "The Object you want to instantiate is null.");
            return (T)Internal_CloneSingle(original);
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
        [FreeFunction("Scripting::DestroyObjectFromScripting")]
        public extern static void Destroy(Object obj, [uei.DefaultValue("0.0F")] float t);

        [uei.ExcludeFromDocs]
        public static void Destroy(Object obj)
        {
            float t = 0.0F;
            Destroy(obj, t);
        }

        // Destroys the object /obj/ immediately. It is strongly recommended to use Destroy instead.
        [FreeFunction("Scripting::DestroyObjectFromScriptingImmediate")]
        public extern static void DestroyImmediate(Object obj, [uei.DefaultValue("false")]  bool allowDestroyingAssets);

        [uei.ExcludeFromDocs]
        public static void DestroyImmediate(Object obj)
        {
            bool allowDestroyingAssets = false;
            DestroyImmediate(obj, allowDestroyingAssets);
        }

        // Returns a list of all active loaded objects of Type /type/.
        [TypeInferenceRule(TypeInferenceRules.ArrayOfTypeReferencedByFirstArgument)]
        [FreeFunction("UnityEngineObjectBindings::FindObjectsOfType")]
        public extern static Object[] FindObjectsOfType(Type type);

        // Makes the object /target/ not be destroyed automatically when loading a new scene.
        [FreeFunction("GetSceneManager().DontDestroyOnLoad")]
        public extern static void DontDestroyOnLoad(Object target);

        // // Should the object be hidden, saved with the scene or modifiable by the user?
        public extern HideFlags hideFlags { get; set; }

        //*undocumented* deprecated
        // We cannot properly deprecate this in C# right now, since the optional parameter creates
        // another method calling this, which creates compiler warnings when deprecated.
        [Obsolete("use Object.Destroy instead.")]
        [FreeFunction("Scripting::DestroyObjectFromScripting")]
        public extern static void DestroyObject(Object obj, [uei.DefaultValue("0.0F")]  float t);

        [Obsolete("use Object.Destroy instead.")]
        [uei.ExcludeFromDocs]
        public static void DestroyObject(Object obj)
        {
            float t = 0.0F;
            DestroyObject(obj, t);
        }

        //*undocumented* DEPRECATED
        [Obsolete("warning use Object.FindObjectsOfType instead.")]
        [FreeFunction("UnityEngineObjectBindings::FindObjectsOfType")]
        public extern static Object[] FindSceneObjectsOfType(Type type);

        //*undocumented*  DEPRECATED
        [Obsolete("use Resources.FindObjectsOfTypeAll instead.")]
        [FreeFunction("UnityEngineObjectBindings::FindObjectsOfTypeIncludingAssets")]
        public extern static Object[] FindObjectsOfTypeIncludingAssets(Type type);

        public static T[] FindObjectsOfType<T>() where T : Object
        {
            return Resources.ConvertObjects<T>(FindObjectsOfType(typeof(T)));
        }

        public static T FindObjectOfType<T>() where T : Object
        {
            return (T)FindObjectOfType(typeof(T));
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
        public static Object FindObjectOfType(System.Type type)
        {
            Object[] objects = FindObjectsOfType(type);
            if (objects.Length > 0)
                return objects[0];
            else
                return null;
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

        [NativeMethod(Name = "Thread::CurrentThreadIsMainThread", IsFreeFunction = true, IsThreadSafe = true)]
        extern static bool CurrentThreadIsMainThread();

        [FreeFunction("CloneObject")]
        extern static Object Internal_CloneSingle(Object data);

        [FreeFunction("CloneObject")]
        extern static Object Internal_CloneSingleWithParent(Object data, Transform parent, bool worldPositionStays);

        [FreeFunction("InstantiateObject")]
        extern static Object Internal_InstantiateSingle(Object data, Vector3 pos, Quaternion rot);

        [FreeFunction("InstantiateObject")]
        extern static Object Internal_InstantiateSingleWithParent(Object data, Transform parent, Vector3 pos, Quaternion rot);

        [FreeFunction("UnityEngineObjectBindings::ToString")]
        extern static string ToString(Object obj);

        [FreeFunction("UnityEngineObjectBindings::GetName")]
        extern static string GetName(Object obj);

        [FreeFunction("UnityEngineObjectBindings::SetName")]
        extern static void SetName(Object obj, string name);

        [NativeMethod(Name = "UnityEngineObjectBindings::DoesObjectWithInstanceIDExist", IsFreeFunction = true, IsThreadSafe = true)]
        internal extern static bool DoesObjectWithInstanceIDExist(int instanceID);

        [VisibleToOtherModules]
        [FreeFunction("UnityEngineObjectBindings::FindObjectFromInstanceID")]
        internal extern static Object FindObjectFromInstanceID(int instanceID);
    }
}
