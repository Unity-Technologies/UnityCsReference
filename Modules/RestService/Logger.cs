// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.RestService
{
    internal class Logger
    {
        static public void Log(Exception an_exception)
        {
            Debug.Log(an_exception.ToString());
        }

        static public void Log(string a_message)
        {
            Debug.Log(a_message);
        }
    }
}
