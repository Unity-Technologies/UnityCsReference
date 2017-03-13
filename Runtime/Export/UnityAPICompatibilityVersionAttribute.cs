// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class UnityAPICompatibilityVersionAttribute : Attribute
    {
        public UnityAPICompatibilityVersionAttribute(string version)
        {
            _version = version;
        }

        public string version { get { return _version; } }

        private string _version;
    }
}
