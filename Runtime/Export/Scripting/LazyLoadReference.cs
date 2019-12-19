// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngineInternal;
using uei = UnityEngine.Internal;

namespace UnityEngine
{
    //----------------------------------------------------------------------------------------------------------------------
    // What is this : Serializable lazy reference to a UnityEngine.Object contained in an asset file, where referenced object
    //                is loaded only when accessed and not at deserialization of this struct.
    // Motivation(s):
    //  - Importer settings need to be able to reference assets but reading the settings from disk Must Not load the referenced
    //    assets: they may not have been imported yet.
    //  - Scenes with large number of assets can be slow to open because all referenced assets get loaded at the same time
    //    as the scene, even if they are not needed right away. Having lazy loading can make loading scene faster for the user.
    //    For example: MonoBehaviours that have references to assets but that do not execute in editor mode, do not need to have
    //         all the referenced assets loaded when editor opens a scene. Later when user enters play mode, those assets get loaded.
    //
    // Notes:
    //  - *** The memory layout of this struct must be identical to the native type: AssetReferenceMemoryLayout. ***
    //  - Not using bindings file as we don't want a wrapper class in this situation. but it must mirror it's native counter part to a 'T'.
    //----------------------------------------------------------------------------------------------------------------------
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct LazyLoadReference<T> where T : UnityEngine.Object
    {
        private const int kInstanceID_None = 0;

        [SerializeField]
        private int m_InstanceID;

        public int instanceID
        {
            get { return m_InstanceID; }
            set { m_InstanceID = value; }
        }

        // Determines if the reference is linked to an object, loaded or not, valid or not.
        // Calling this never triggers a load.
        public bool isSet => m_InstanceID == kInstanceID_None;

        // Convenience property that checks if the reference is broken: is set to something, but that something is not available/loadable at the moment for whatever reason.
        // Note that this will may trigger loading the referenced object into memory.
        public bool isBroken => m_InstanceID != kInstanceID_None && !UnityEngine.Object.DoesObjectWithInstanceIDExist(m_InstanceID);

        // Accessor to the referenenced object/asset.
        // Note that this will may trigger loading the referenced object into memory.
        public T asset
        {
            get
            {
                if (m_InstanceID == kInstanceID_None)
                    return null;
                else
                {
                    return (T)Object.ForceLoadFromInstanceID(m_InstanceID);
                }
            }

            set
            {
                if (value != null)
                {
                    if (!Object.IsPersistent(value))
                    {
                        throw new ArgumentException("Object that does not belong to a persisted asset cannot be set as the target of a LazyLoadReference.");
                    }

                    m_InstanceID = value.GetInstanceID();
                }
                else
                    m_InstanceID = kInstanceID_None;
            }
        }
    }
}
