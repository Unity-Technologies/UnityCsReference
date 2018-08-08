// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEditor.VersionControl
{
    [NativeType("Editor/Src/VersionControl/VCEnums.h")]
    public enum CompletionAction
    {
        UpdatePendingWindow = 1,
        OnChangeContentsPendingWindow = 2,
        OnIncomingPendingWindow = 3,
        OnChangeSetsPendingWindow = 4,
        OnGotLatestPendingWindow = 5,
        OnSubmittedChangeWindow = 6,
        OnAddedChangeWindow = 7,
        OnCheckoutCompleted = 8
    }

    [Flags]
    public enum SubmitResult
    {
        OK = 1,
        Error = 2,
        ConflictingFiles = 4,
        UnaddedFiles = 8
    }

    [NativeHeader("Editor/Src/VersionControl/VCTask.h")]
    [NativeHeader("Editor/Src/VersionControl/VC_bindings.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public partial class Task
    {
        // The bindings generator will set the instance pointer in this field
        IntPtr m_Self;

        public extern void Wait();

        public extern void SetCompletionAction(CompletionAction action);

        [NativeMethod("GetMonoAssetList")]
        extern Asset[] Internal_GetAssetList();
        [NativeMethod("GetMonoChangeSets")]
        extern ChangeSet[] Internal_GetChangeSets();

        [FreeFunction("VersionControlBindings::Task::Destroy", IsThreadSafe = true)]
        static extern void Destroy(IntPtr task);

        public void Dispose()
        {
            if (m_Self != IntPtr.Zero)
            {
                Destroy(m_Self);
                m_Self = IntPtr.Zero;
            }
        }

        internal Task() {}

        ~Task()
        {
            Dispose();
        }

        public extern int userIdentifier { get; set; }
        public extern string text { get; }
        public extern string description { get; }
        public extern bool success { get; }
        public extern int secondsSpent { get; }
        public extern int progressPct { get; }
        public extern string progressMessage { get; }
        public extern int resultCode { get; }
        public extern Message[] messages
        {
            [NativeName("GetMonoMessages")]
            get;
        }
    }
}
