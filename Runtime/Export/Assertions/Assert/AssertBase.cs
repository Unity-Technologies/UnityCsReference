// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;
using System.ComponentModel;

namespace UnityEngine.Assertions
{
    [DebuggerStepThrough]
    public static partial class Assert
    {
        internal const string UNITY_ASSERTIONS = "UNITY_ASSERTIONS";

        public static bool raiseExceptions;

        static void Fail(string message, string userMessage)
        {
            if (Debugger.IsAttached)
                //Just thrown an exception for now.
                //One day maybe we can implement it with a proper opcode
                throw new AssertionException(message, userMessage);
            if (raiseExceptions)
                throw new AssertionException(message, userMessage);
            if (message == null)
                message = "Assertion has failed\n";
            if (userMessage != null)
                message = userMessage + '\n' + message;
            Debug.LogAssertion(message);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Assert.Equals should not be used for Assertions", true)]
        public new static bool Equals(object obj1, object obj2)
        {
            throw new InvalidOperationException("Assert.Equals should not be used for Assertions");
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Assert.ReferenceEquals should not be used for Assertions", true)]
        public new static bool ReferenceEquals(object obj1, object obj2)
        {
            throw new InvalidOperationException("Assert.ReferenceEquals should not be used for Assertions");
        }

    }
}
