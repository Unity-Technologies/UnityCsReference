// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;


namespace UnityEditor.Analytics
{
    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class CollabOperationAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public CollabOperationAnalytic() : base("collabOperation", 1) { }

        [UsedByNativeCode]
        internal static CollabOperationAnalytic CreateCollabOperationAnalytic() { return new CollabOperationAnalytic(); }

        public string category;
        public string operation;
        public string result;

        public Int64 start_ts;
        public Int64 duration;
    }
}
