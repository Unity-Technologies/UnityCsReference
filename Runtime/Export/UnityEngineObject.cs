// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngineInternal;
using System;
using UnityEngine.Scripting;

namespace UnityEngine
{
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

        public static T[] FindObjectsOfType<T>() where T : Object
        {
            return Resources.ConvertObjects<T>(FindObjectsOfType(typeof(T)));
        }

        public static T FindObjectOfType<T>() where T : Object
        {
            return (T)FindObjectOfType(typeof(T));
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

        public static bool operator==(Object x, Object y) { return CompareBaseObjects(x, y); }

        public static bool operator!=(Object x, Object y) { return !CompareBaseObjects(x, y); }
    }
}
