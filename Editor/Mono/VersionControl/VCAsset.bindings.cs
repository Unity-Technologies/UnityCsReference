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
            Local = 1 << 0,
            Synced = 1 << 1,
            OutOfSync = 1 << 2,
            Missing = 1 << 3,
            CheckedOutLocal = 1 << 4,
            CheckedOutRemote = 1 << 5,
            DeletedLocal = 1 << 6,
            DeletedRemote = 1 << 7,
            AddedLocal = 1 << 8,
            AddedRemote = 1 << 9,
            Conflicted = 1 << 10,
            LockedLocal = 1 << 11,
            LockedRemote = 1 << 12,
            Updating = 1 << 13,
            ReadOnly = 1 << 14,
            MetaFile = 1 << 15,
            MovedLocal = 1 << 16,
            MovedRemote = 1 << 17,
            Unversioned = 1 << 18,
            Exclusive = 1 << 19,
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
        public extern string metaPath { get; }

        [NativeMethod(IsThreadSafe = true)]
        public extern string assetPath { get; }

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
