// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/Logging/UnityLogWriter.bindings.h")]
    internal class UnityLogWriter : System.IO.TextWriter
    {
        public static void WriteStringToUnityLog(string s)
        {
            if (s == null) return;
            WriteStringToUnityLogImpl(s);
        }

        [FreeFunction(IsThreadSafe = true)]
        private static extern void WriteStringToUnityLogImpl(string s);

        [NoAutoStaticsCleanup] // set once per process, holds no user objects
        static TextWriter s_LogWriter;
        [NoAutoStaticsCleanup] // set once per process, holds no user objects
        static TextReader s_StdIn;

        public static void Init()
        {
            if (s_LogWriter != null)
                return;

            s_LogWriter = TextWriter.Synchronized(new UnityLogWriter());

            // We want to tie both stdout and stderr to the same log writer.
            System.Console.SetOut(s_LogWriter);
            System.Console.SetError(s_LogWriter);

            // Opening the stream instead of reading Console.In avoids initializing the terminal.
            // SetIn wraps the reader, so cache the wrapper Console actually holds.
            System.Console.SetIn(new StreamReader(System.Console.OpenStandardInput(), System.Console.InputEncoding, detectEncodingFromByteOrderMarks: false));
            s_StdIn = System.Console.In;
        }

        // Drops any user writer or reader so System.Console does not keep the User ALC alive.
        public static void ResetConsoleStreams()
        {
            if (s_LogWriter == null)
                return;

            if (System.Console.Out != s_LogWriter)
                System.Console.SetOut(s_LogWriter);
            if (System.Console.Error != s_LogWriter)
                System.Console.SetError(s_LogWriter);
            if (System.Console.In != s_StdIn)
                System.Console.SetIn(s_StdIn);
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
