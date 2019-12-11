// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;


namespace UnityEngine.XR.WSA.Persistence
{
    [NativeHeader("Modules/VR/HoloLens/WorldAnchor/WorldAnchorStore.h")]
    [NativeHeader("VRScriptingClasses.h")]
    [MovedFrom("UnityEngine.VR.WSA.Persistence")]
    [StructLayout(LayoutKind.Sequential)]   // needed for IntPtr binding classes
    public class WorldAnchorStore : IDisposable
    {
        internal IntPtr m_NativePtr;

        private static WorldAnchorStore s_Instance = null;

        public delegate void GetAsyncDelegate(WorldAnchorStore store);

        private WorldAnchorStore(IntPtr nativePtr)
        {
            m_NativePtr = nativePtr;
        }

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public static void GetAsync(GetAsyncDelegate onCompleted)
        {
        }


        [RequiredByNativeCode]
        private static void InvokeGetAsyncDelegate(GetAsyncDelegate handler, IntPtr nativePtr)
        {
            s_Instance = new WorldAnchorStore(nativePtr);
            handler(s_Instance);
        }

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public bool Save(string id, WorldAnchor anchor)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("id must not be null or empty", "id");
            }

            if (anchor == null)
            {
                throw new ArgumentNullException("anchor");
            }
            return Save_Interal(id, anchor);
        }

        [NativeName("Save")]
        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        private extern bool Save_Interal(string id, WorldAnchor anchor);

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public WorldAnchor Load(string id, GameObject go)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("id must not be null or empty", "id");
            }

            if (go == null)
            {
                throw new ArgumentNullException("anchor");
            }
            return null;
        }

        [NativeName("Load")]
        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        private extern bool Load_Internal(string id, WorldAnchor anchor);

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public bool Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("id must not be null or empty", "id");
            }
            return false;
        }


        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public extern void Clear();

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public extern int anchorCount { get; }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public extern int GetAllIds([NotNull] string[] ids);

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public string[] GetAllIds()
        {
            return new string[0];
        }

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public void Dispose()
        {
            if (m_NativePtr != IntPtr.Zero)
                Internal_Destroy();

            m_NativePtr = IntPtr.Zero;

            GC.SuppressFinalize(this);
        }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        private extern void Internal_Destroy();
    }
}

