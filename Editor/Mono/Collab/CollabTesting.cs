// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Collaboration
{
    internal class CollabTesting
    {
        private static IEnumerator<bool> _enumerator = null;
        private static Action _runAfter = null;

        public static Func<IEnumerable<bool>> Tick
        {
            set { _enumerator = value().GetEnumerator(); }
        }

        public static Action AfterRun
        {
            set { _runAfter = value; }
        }

        public static bool IsRunning
        {
            get { return _enumerator != null; }
        }

        public static void OnCompleteJob()
        {
            Execute();
        }

        public static void Execute()
        {
            if (_enumerator == null)
                return;

            if (Collab.instance.AnyJobRunning())
                return;

            try
            {
                if (!_enumerator.MoveNext())
                    End();
            }
            catch (Exception)
            {
                Debug.LogError("Something Went wrong with the test framework itself");
                throw;
            }
        }

        public static void End()
        {
            if (_enumerator != null)
            {
                _runAfter();
                _enumerator = null;
            }
        }
    }
}
