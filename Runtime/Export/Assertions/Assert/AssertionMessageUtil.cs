// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEngine.Assertions
{
    internal class AssertionMessageUtil
    {
        const string k_Expected = "Expected:";
        const string k_AssertionFailed = "Assertion failure.";

        public static string GetMessage(string failureMessage)
        {
            return string.Format("{0} {1}", k_AssertionFailed, failureMessage);
        }

        public static string GetMessage(string failureMessage, string expected)
        {
            return GetMessage(string.Format("{0}{1}{2} {3}", failureMessage, Environment.NewLine, k_Expected, expected));
        }

        public static string GetEqualityMessage(object actual, object expected, bool expectEqual)
        {
            return GetMessage(string.Format("Values are {0}equal.", expectEqual ? "not " : ""),
                string.Format("{0} {2} {1}", actual, expected, expectEqual ? "==" : "!="));
        }

        public static string NullFailureMessage(object value, bool expectNull)
        {
            return GetMessage(string.Format("Value was {0}Null", expectNull ? "not " : ""),
                string.Format("Value was {0}Null", expectNull ? "" : "not "));
        }

        public static string BooleanFailureMessage(bool expected)
        {
            return GetMessage("Value was " + !expected, expected.ToString());
        }
    }
}
