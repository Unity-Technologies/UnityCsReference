// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/UnityLogWriter.bindings.h")]
    internal class UnityLogWriter : System.IO.TextWriter
    {
        [ThreadAndSerializationSafe]
        public static void WriteStringToUnityLog(string s)
        {
            if (s == null) return;
            WriteStringToUnityLogImpl(s);
        }

        [FreeFunction(IsThreadSafe = true)]
        private static extern void WriteStringToUnityLogImpl(string s);

        public static void Init()
        {
            System.Console.SetOut(new UnityLogWriter());
        }

        public override System.Text.Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }
        public override void Write(char value)
        {
            WriteStringToUnityLog(value.ToString());
        }

        public override void Write(string s)
        {
            WriteStringToUnityLog(s);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            WriteStringToUnityLogImpl(new string(buffer, index, count));
        }
    }
}
