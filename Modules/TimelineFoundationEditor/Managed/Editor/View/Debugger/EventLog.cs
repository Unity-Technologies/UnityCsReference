// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Timeline.Foundation.View.Debugger
{
    readonly struct EventLog
    {
        public readonly string name;
        public readonly string stack;
        public readonly int frameNumber;

        public EventLog(string name)
        {
            this.name = name;
            frameNumber = UnityEngine.Time.frameCount;
            stack = GetStack();
        }

        public EventLog(string name, string stack)
        {
            this.name = name;
            frameNumber = UnityEngine.Time.frameCount;
            this.stack = stack;
        }

        static string GetStack()
        {
            return new System.Diagnostics.StackTrace().ToString();
        }

        public override string ToString()
        {
            return $"{name} (frame {frameNumber})";
        }
    }
}
