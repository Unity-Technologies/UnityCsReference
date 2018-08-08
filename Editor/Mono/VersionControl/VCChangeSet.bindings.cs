// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEditor.VersionControl
{
    //*undocumented*
    [NativeHeader("Editor/Src/VersionControl/VCChangeSet.h")]
    [NativeHeader("Editor/Src/VersionControl/VC_bindings.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    partial class ChangeSet
    {
        // The bindings generator will set the instance pointer in this field
        IntPtr m_Self;

        [FreeFunction("VersionControlBindings::ChangeSet::Create", IsThreadSafe = true)]
        static extern IntPtr Create();

        [FreeFunction("VersionControlBindings::ChangeSet::CreateFromCopy", IsThreadSafe = true)]
        static extern IntPtr CreateFromCopy(ChangeSet other);

        [FreeFunction("VersionControlBindings::ChangeSet::CreateFromString", IsThreadSafe = true)]
        static extern IntPtr CreateFromString(string description);

        [FreeFunction("VersionControlBindings::ChangeSet::CreateFromStringString", IsThreadSafe = true)]
        static extern IntPtr CreateFromStringString(string description, string changeSetID);

        [FreeFunction("VersionControlBindings::ChangeSet::Destroy", IsThreadSafe = true)]
        static extern void Destroy(IntPtr changeSet);

        void InternalCreate()
        {
            m_Self = Create();
        }

        void InternalCopyConstruct(ChangeSet other)
        {
            m_Self = CreateFromCopy(other);
        }

        void InternalCreateFromString(string description)
        {
            m_Self = CreateFromString(description);
        }

        void InternalCreateFromStringString(string description, string changeSetID)
        {
            m_Self = CreateFromStringString(description, changeSetID);
        }

        //*undocumented
        public void Dispose()
        {
            Destroy(m_Self);
            m_Self = IntPtr.Zero;
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern string description { get; }

        [NativeMethod(IsThreadSafe = true)]
        public extern string id
        {
            [NativeName("GetID")]
            get;
        }
    }
}
