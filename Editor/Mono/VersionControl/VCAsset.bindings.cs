// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEditor.VersionControl
{
    [NativeHeader("Editor/Src/VersionControl/VCAsset.h")]
    [NativeHeader("Editor/Src/VersionControl/VC_bindings.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public partial class Asset
    {
        // The bindings generator will set the instance pointer in this field
        IntPtr m_Self;

        [Flags]
        public enum States
        {
            None = 0,
            Local = 1,
            Synced = 2,
            OutOfSync = 4,
            Missing = 8,
            CheckedOutLocal = 16,
            CheckedOutRemote = 32,
            DeletedLocal = 64,
            DeletedRemote = 128,
            AddedLocal = 256,
            AddedRemote = 512,
            Conflicted = 1024,
            LockedLocal = 2048,
            LockedRemote = 4096,
            Updating = 8192,
            ReadOnly = 16384,
            MetaFile = 32768,
            MovedLocal = 65536,
            MovedRemote = 131072
        }

        public Asset(string clientPath)
        {
            m_Self = Create(clientPath);
        }

        ~Asset()
        {
            Dispose();
        }

        public void Dispose()
        {
            Destroy(m_Self);
            m_Self = IntPtr.Zero;
        }

        [FreeFunction("VersionControlBindings::Asset::Create", IsThreadSafe = true)]
        static extern IntPtr Create(string clientPath);

        [FreeFunction("VersionControlBindings::Asset::Destroy", IsThreadSafe = true)]
        static extern void Destroy(IntPtr asset);

        [FreeFunction("VersionControlBindings::Asset::GetState", IsThreadSafe = true)]
        static extern States GetState(IntPtr asset);

        [NativeName("MonoIsChildOf")]
        [NativeMethod(IsThreadSafe = true)]
        public extern bool IsChildOf(Asset other);

        public States state
        {
            get
            {
                return GetState(m_Self);
            }
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern string path { get; }

        [NativeMethod(IsThreadSafe = true)]
        public extern bool isFolder
        {
            [NativeName("IsFolder")]
            get;
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern bool readOnly
        {
            [NativeName("IsReadOnly")]
            get;
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern bool isMeta
        {
            [NativeName("IsMeta")]
            get;
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern bool locked
        {
            [NativeName("IsLocked")]
            get;
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern string name { get; }

        [NativeMethod(IsThreadSafe = true)]
        public extern string fullName { get; }

        [NativeMethod(IsThreadSafe = true)]
        public extern bool isInCurrentProject
        {
            [NativeName("IsInCurrentProject")]
            get;
        }
    }
}
