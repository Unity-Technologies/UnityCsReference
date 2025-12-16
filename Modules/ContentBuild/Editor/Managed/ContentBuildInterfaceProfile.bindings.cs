// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Content
{
    ///<summary>Options for labelling captured profile events using the <see cref="ContentBuildInterface.BeginProfileCapture" /> and <see cref="ContentBuildInterface.EndProfileCapture" /> APIs.</summary>
    public enum ProfileEventType
    {
        ///<summary>Use to indicate that a task has begun.</summary>
        Begin = 0,
        ///<summary>Use to indicate that a task has ended.</summary>
        End = 1,
        ///<summary>Use to indicate that general information is being reported.</summary>
        Info = 2
    }

    ///<summary>Options for filtering captured profile events using the <see cref="ContentBuildInterface.BeginProfileCapture" /> and <see cref="ContentBuildInterface.EndProfileCapture" /> APIs.</summary>
    public enum ProfileCaptureOptions
    {
        ///<summary>Use to exclude none of the captured events.</summary>
        None = 0,
        ///<summary>Use to exclude all captured events that are less than 10 microseconds in duration.</summary>
        IgnoreShortEvents = 1
    }

    ///<summary>Details about a profile event captured using the <see cref="ContentBuildInterface.BeginProfileCapture" /> and <see cref="ContentBuildInterface.EndProfileCapture" /> APIs.</summary>
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct ContentBuildProfileEvent
    {
        ///<summary>Time in microseconds that the event has occurred relative to when the profile capture began.</summary>
        public UInt64 TimeMicroseconds;
        ///<summary>Name of the event.</summary>
        public string Name;
        ///<summary>Additional metadata associated with the event.</summary>
        public string Metadata;
        ///<summary>Enum used to label the event's type.</summary>
        public ProfileEventType Type;
    };
}
